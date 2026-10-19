using System.Text.Json;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

public class KpiPlanningService : IKpiPlanningService
{
    private readonly KpiDbContext _context;
    private readonly ILogger<KpiPlanningService> _logger;

    public KpiPlanningService(KpiDbContext context, ILogger<KpiPlanningService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region 1. Truy vấn Danh sách & Chi tiết
    public async Task<List<KpiAssignmentSummaryDto>> GetAssignmentsAsync(
        long? periodId, long? departmentId, string? status, string? search, long currentUserId, string role)
    {
        var query = _context.KpiAssignments
            .Include(a => a.Period)
            .Include(a => a.Employee).ThenInclude(e => e.Department)
            .Include(a => a.Employee).ThenInclude(e => e.Position)
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Include(a => a.AssignmentDetails)
            .AsQueryable();

        // 1. Phân quyền dữ liệu
        if (role.Equals("Officer", StringComparison.OrdinalIgnoreCase) || role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => a.Employee.UserId == currentUserId);
        }
        else if (role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            var mgrProfile = await _context.EmployeeProfiles.FirstOrDefaultAsync(e => e.UserId == currentUserId && !e.IsDeleted);
            if (mgrProfile != null)
            {
                query = query.Where(a => a.Employee.DepartmentId == mgrProfile.DepartmentId || a.ApproverId == currentUserId);
            }
        }

        // 2. Lọc theo Kỳ đánh giá
        if (periodId.HasValue && periodId.Value > 0)
        {
            query = query.Where(a => a.PeriodId == periodId.Value);
        }

        // 3. Lọc theo Phòng ban
        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(a => a.Employee.DepartmentId == departmentId.Value);
        }

        // 4. Lọc theo Trạng thái
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AssignmentStatus>(status, true, out var st))
        {
            query = query.Where(a => a.Status == st);
        }

        // 5. Tìm kiếm từ khóa
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.Employee.FullName.ToLower().Contains(s) ||
                                     a.Employee.StaffCode.ToLower().Contains(s) ||
                                     a.Title.ToLower().Contains(s));
        }

        var list = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

        return list.Select(a => new KpiAssignmentSummaryDto
        {
            Id = a.Id,
            PeriodName = a.Period.Name,
            EmployeeName = a.Employee.FullName,
            StaffCode = a.Employee.StaffCode,
            DepartmentName = a.Employee.Department.Name,
            PositionName = a.Employee.Position.Name,
            Title = a.Title,
            TotalWeight = a.TotalWeight,
            ItemCount = a.AssignmentDetails.Count,
            Status = a.Status,
            CommittedAt = a.CommittedAt,
            ApprovedAt = a.ApprovedAt,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    public async Task<KpiAssignmentDto?> GetAssignmentByIdAsync(long id)
    {
        var a = await _context.KpiAssignments
            .Include(x => x.Period)
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Include(x => x.Employee).ThenInclude(e => e.Position)
            .Include(x => x.Employee).ThenInclude(e => e.User)
            .Include(x => x.Approver)
            .Include(x => x.AssignmentDetails).ThenInclude(d => d.KpiItem).ThenInclude(k => k.Category)
            .Include(x => x.AssignmentDetails).ThenInclude(d => d.Evidences)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return null;

        return MapToDto(a);
    }

    public async Task<KpiAssignmentDto?> GetMyKpiAsync(long employeeUserId, long? periodId = null)
    {
        var emp = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(e => e.UserId == employeeUserId && !e.IsDeleted);

        if (emp == null) return null;

        var query = _context.KpiAssignments
            .Include(x => x.Period)
            .Include(x => x.Employee).ThenInclude(e => e.Department)
            .Include(x => x.Employee).ThenInclude(e => e.Position)
            .Include(x => x.Employee).ThenInclude(e => e.User)
            .Include(x => x.Approver)
            .Include(x => x.AssignmentDetails).ThenInclude(d => d.KpiItem).ThenInclude(k => k.Category)
            .Include(x => x.AssignmentDetails).ThenInclude(d => d.Evidences)
            .Where(x => x.EmployeeId == emp.Id);

        if (periodId.HasValue && periodId.Value > 0)
        {
            query = query.Where(x => x.PeriodId == periodId.Value);
        }

        var assignment = await query.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
        return assignment != null ? MapToDto(assignment) : null;
    }
    #endregion

    #region 2. Danh mục Phục vụ Giao KPI & Gợi ý Template
    public async Task<List<EmployeeSelectDto>> GetEmployeesForAssignmentAsync(long managerUserId)
    {
        var mgr = await _context.EmployeeProfiles
            .Include(e => e.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(e => e.UserId == managerUserId && !e.IsDeleted);

        var query = _context.EmployeeProfiles
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => !e.IsDeleted);

        bool isHighLevel = mgr?.User.UserRoles.Any(r => r.Role.Code == "ADMIN" || r.Role.Code == "DIRECTOR") ?? false;
        if (!isHighLevel && mgr != null)
        {
            // Quản lý phòng ban: Chỉ lấy nhân viên cùng phòng ban
            query = query.Where(e => e.DepartmentId == mgr.DepartmentId);
        }

        var employees = await query.OrderBy(e => e.FullName).ToListAsync();

        return employees.Select(e => new EmployeeSelectDto
        {
            EmployeeId = e.Id,
            UserId = e.UserId,
            StaffCode = e.StaffCode,
            FullName = e.FullName,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department.Name,
            PositionId = e.PositionId,
            PositionName = e.Position.Name,
            HasActiveAssignment = false
        }).ToList();
    }

    public async Task<List<PeriodSelectDto>> GetEvaluationPeriodsForPlanningAsync()
    {
        var periods = await _context.EvaluationPeriods
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.StartDate)
            .ToListAsync();

        return periods.Select(p => new PeriodSelectDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Year = p.Year,
            Status = p.Status.ToString(),
            IsActiveForPlanning = p.Status == PeriodStatus.Planning || p.Status == PeriodStatus.Open
        }).ToList();
    }

    public async Task<TemplateSuggestDto?> GetTemplateSuggestionsAsync(long employeeId)
    {
        var emp = await _context.EmployeeProfiles
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (emp == null) return null;

        var template = await _context.KpiTemplates
            .Include(t => t.Position)
            .Include(t => t.TemplateDetails).ThenInclude(td => td.KpiItem).ThenInclude(k => k.Category)
            .Include(t => t.TemplateDetails).ThenInclude(td => td.KpiItem).ThenInclude(k => k.Unit)
            .FirstOrDefaultAsync(t => t.PositionId == emp.PositionId && !t.IsDeleted);

        if (template == null) return null;

        return new TemplateSuggestDto
        {
            TemplateId = template.Id,
            TemplateCode = template.Code,
            TemplateName = template.Name,
            PositionName = emp.Position.Name,
            Items = template.TemplateDetails.Select(td => new TemplateSuggestItemDto
            {
                KpiItemId = td.KpiItemId,
                KpiCode = td.KpiItem.Code,
                KpiName = td.KpiItem.Name,
                Perspective = td.KpiItem.Category.Perspective.ToString(),
                Unit = td.KpiItem.Unit.Symbol,
                TargetValue = td.TargetValue,
                MinThreshold = td.MinThreshold,
                Weight = td.Weight,
                ScoringFormula = td.KpiItem.ScoringFormula.ToString(),
                GuidanceNotes = td.GuidanceNotes
            }).ToList()
        };
    }
    #endregion

    #region 3. Tạo mới, Cập nhật, Cam kết & Duyệt Bản giao (BR-01, BR-02, BR-03)
    public async Task<long> SaveOrSubmitAssignmentAsync(CreateAssignmentRequest request, long currentUserId)
    {
        if (request.Items == null || !request.Items.Any())
        {
            throw new BusinessRuleException("BR-VALIDATION", "Bản giao KPI phải chứa ít nhất 01 chỉ tiêu.");
        }

        // Tính tổng trọng số
        decimal totalWeight = request.Items.Sum(i => i.Weight);

        // Kiểm tra nghiệp vụ nếu là Gửi cam kết (!IsDraft)
        if (!request.IsDraft)
        {
            // BR-01: Tổng trọng số bắt buộc đúng 100.00%
            if (totalWeight != 100.00m)
            {
                throw new BusinessRuleException("BR-01",
                    $"[BR-01] Tổng trọng số các chỉ tiêu phải bằng chính xác 100.00%. Hiện tại đang là: {totalWeight:0.##}%.");
            }

            // BR-02: Trọng số của từng chỉ tiêu phải từ 5% đến 50%
            foreach (var item in request.Items)
            {
                if (item.Weight < 5.00m || item.Weight > 50.00m)
                {
                    throw new BusinessRuleException("BR-02",
                        $"[BR-02] Chỉ tiêu '{item.KpiName}' có trọng số {item.Weight}% vi phạm quy định (Trọng số cho phép từ 5% đến 50%).");
                }
            }
        }

        KpiAssignment? assignment = null;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            assignment = await _context.KpiAssignments
                .Include(a => a.AssignmentDetails)
                .FirstOrDefaultAsync(a => a.Id == request.Id.Value);

            if (assignment == null)
            {
                throw new NotFoundException("KpiAssignment", request.Id.Value);
            }

            // BR-03: Nếu đã Approved_Locked -> Tuyệt đối không cho sửa!
            if (assignment.Status == AssignmentStatus.Approved_Locked)
            {
                throw new BusinessRuleException("BR-03",
                    "[BR-03] Bản giao KPI đã được Phê duyệt & Khóa (Approved_Locked). Không được phép chỉnh sửa trực tiếp. Vui lòng tạo yêu cầu Điều chỉnh giữa kỳ.");
            }
        }
        else
        {
            // Kiểm tra xem đã có bản giao cho nhân viên trong kỳ này chưa
            var existing = await _context.KpiAssignments
                .Include(a => a.AssignmentDetails)
                .FirstOrDefaultAsync(a => a.PeriodId == request.PeriodId && a.EmployeeId == request.EmployeeId);

            if (existing != null)
            {
                assignment = existing;
                if (assignment.Status == AssignmentStatus.Approved_Locked)
                {
                    throw new BusinessRuleException("BR-03",
                        "[BR-03] Nhân viên đã có bản giao KPI được phê duyệt trong kỳ này. Không thể tạo mới đè lên.");
                }
            }
            else
            {
                assignment = new KpiAssignment
                {
                    PeriodId = request.PeriodId,
                    EmployeeId = request.EmployeeId,
                    ApproverId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.KpiAssignments.Add(assignment);
            }
        }

        assignment.Title = string.IsNullOrWhiteSpace(request.Title)
            ? $"Kế hoạch mục tiêu KPI - {DateTime.UtcNow.Year}"
            : request.Title.Trim();

        assignment.TotalWeight = totalWeight;
        assignment.ApproverId = currentUserId;
        assignment.Status = request.IsDraft ? AssignmentStatus.Draft : AssignmentStatus.Pending_Staff_Confirm;
        assignment.UpdatedAt = DateTime.UtcNow;

        // Xóa các chi tiết cũ và tạo lại danh sách chi tiết mới
        if (assignment.AssignmentDetails.Any())
        {
            _context.KpiAssignmentDetails.RemoveRange(assignment.AssignmentDetails);
        }

        foreach (var item in request.Items)
        {
            assignment.AssignmentDetails.Add(new KpiAssignmentDetail
            {
                KpiItemId = item.KpiItemId,
                KpiName = item.KpiName.Trim(),
                Unit = item.Unit.Trim(),
                TargetValue = item.TargetValue,
                MinThreshold = item.MinThreshold ?? 0m,
                MaxCap = item.MaxCap > 0 ? item.MaxCap : 150m,
                Weight = item.Weight,
                ScoringFormula = string.IsNullOrWhiteSpace(item.ScoringFormula) ? "HigherIsBetter" : item.ScoringFormula
            });
        }

        await _context.SaveChangesAsync();

        await LogAuditAsync(currentUserId,
            request.IsDraft ? "SAVE_DRAFT_ASSIGNMENT" : "SUBMIT_KPI_ASSIGNMENT",
            "KpiAssignment",
            assignment.Id,
            $"Giao KPI ID: {assignment.Id} cho nhân viên ID: {assignment.EmployeeId}. Tổng trọng số: {totalWeight}%. Trạng thái: {assignment.Status}");

        return assignment.Id;
    }

    public async Task<bool> StaffConfirmAssignmentAsync(StaffConfirmRequest request, long employeeUserId)
    {
        var assignment = await _context.KpiAssignments
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId);

        if (assignment == null) return false;

        // Xác nhận quyền: Phải đúng là nhân viên sở hữu bản giao
        if (assignment.Employee.UserId != employeeUserId)
        {
            _logger.LogWarning("Người dùng {UserId} không có quyền cam kết bản giao của nhân viên {EmpId}", employeeUserId, assignment.EmployeeId);
            return false;
        }

        if (request.Agree)
        {
            assignment.CommittedAt = DateTime.UtcNow;
            assignment.Status = AssignmentStatus.Pending_Approval;
            await _context.SaveChangesAsync();

            await LogAuditAsync(employeeUserId, "STAFF_COMMIT_KPI", "KpiAssignment", assignment.Id,
                $"Nhân viên {assignment.Employee.FullName} đã ký cam kết điện tử thành công: {request.ElectronicSignatureNote ?? "Đồng ý các chỉ tiêu mục tiêu"}");

            return true;
        }
        else
        {
            // Yêu cầu điều chỉnh
            return await StaffRequestAdjustmentAsync(request.AssignmentId, request.AdjustmentReason ?? "Yêu cầu xem xét lại mục tiêu/trọng số", employeeUserId);
        }
    }

    public async Task<bool> StaffRequestAdjustmentAsync(long assignmentId, string reason, long employeeUserId)
    {
        var assignment = await _context.KpiAssignments
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null) return false;

        assignment.Status = AssignmentStatus.Draft;
        await _context.SaveChangesAsync();

        await LogAuditAsync(employeeUserId, "STAFF_ADJUST_REQUEST", "KpiAssignment", assignment.Id,
            $"Nhân viên {assignment.Employee.FullName} từ chối cam kết và yêu cầu điều chỉnh. Lý do: {reason}");

        return true;
    }

    public async Task<bool> DirectorApproveAsync(ApproveAssignmentRequest request, long directorUserId)
    {
        var assignment = await _context.KpiAssignments
            .Include(a => a.AssignmentDetails)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId);

        if (assignment == null) return false;

        // Kiểm tra tính toàn vẹn BR-01 trước khi duyệt
        decimal currentTotal = assignment.AssignmentDetails.Sum(d => d.Weight);
        if (currentTotal != 100.00m)
        {
            throw new BusinessRuleException("BR-01", $"Không thể phê duyệt vì tổng trọng số không bằng 100.00% (Hiện tại: {currentTotal}%).");
        }

        // BR-03: Chuyển trạng thái sang Approved_Locked để KHÓA toàn bộ
        assignment.Status = AssignmentStatus.Approved_Locked;
        assignment.ApprovedAt = DateTime.UtcNow;
        assignment.ApproverId = directorUserId;
        await _context.SaveChangesAsync();

        await LogAuditAsync(directorUserId, "DIRECTOR_APPROVE_KPI", "KpiAssignment", assignment.Id,
            $"Lãnh đạo phê duyệt và KHÓA bản giao KPI (BR-03). Nhận xét: {request.Comments ?? "Đã duyệt"}");

        return true;
    }

    public async Task<bool> DirectorRejectAsync(RejectAssignmentRequest request, long directorUserId)
    {
        var assignment = await _context.KpiAssignments.FirstOrDefaultAsync(a => a.Id == request.AssignmentId);
        if (assignment == null) return false;

        assignment.Status = AssignmentStatus.Draft;
        await _context.SaveChangesAsync();

        await LogAuditAsync(directorUserId, "DIRECTOR_REJECT_KPI", "KpiAssignment", assignment.Id,
            $"Lãnh đạo trả lại bản giao KPI. Lý do: {request.Reason}");

        return true;
    }
    #endregion

    #region 4. Điều chỉnh KPI Giữa kỳ (Revision & Diff)
    public async Task<bool> CreateRevisionAsync(RevisionRequest request, long currentUserId)
    {
        var assignment = await _context.KpiAssignments
            .Include(a => a.AssignmentDetails)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId);

        if (assignment == null) return false;

        // Kiểm tra BR-01 và BR-02 cho danh sách mới
        decimal revisedTotal = request.RevisedItems.Sum(i => i.Weight);
        if (revisedTotal != 100.00m)
        {
            throw new BusinessRuleException("BR-01", $"[BR-01] Tổng trọng số sau điều chỉnh phải bằng 100.00%. Hiện tại: {revisedTotal}%.");
        }

        foreach (var item in request.RevisedItems)
        {
            if (item.Weight < 5.00m || item.Weight > 50.00m)
            {
                throw new BusinessRuleException("BR-02", $"[BR-02] Chỉ tiêu '{item.KpiName}' có trọng số {item.Weight}% không hợp lệ.");
            }
        }

        // Tạo bản chụp (Snapshot) cũ để lưu vết lịch sử phiên bản
        var oldSnapshot = assignment.AssignmentDetails.Select(d => new AssignmentItemRequest
        {
            DetailId = d.Id,
            KpiItemId = d.KpiItemId,
            KpiName = d.KpiName,
            Unit = d.Unit,
            TargetValue = d.TargetValue,
            MinThreshold = d.MinThreshold,
            MaxCap = d.MaxCap,
            Weight = d.Weight,
            ScoringFormula = d.ScoringFormula
        }).ToList();

        var oldJson = JsonSerializer.Serialize(oldSnapshot);
        var newJson = JsonSerializer.Serialize(request.RevisedItems);

        // Lưu bản ghi lịch sử vào AuditLogs
        var audit = new AuditLog
        {
            UserId = currentUserId,
            Action = "ASSIGNMENT_REVISION",
            EntityName = "KpiAssignment",
            EntityId = assignment.Id,
            OldValues = oldJson,
            NewValues = newJson,
            UserAgent = request.Reason,
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(audit);

        // Cập nhật chi tiết mới
        _context.KpiAssignmentDetails.RemoveRange(assignment.AssignmentDetails);
        foreach (var item in request.RevisedItems)
        {
            assignment.AssignmentDetails.Add(new KpiAssignmentDetail
            {
                KpiItemId = item.KpiItemId,
                KpiName = item.KpiName.Trim(),
                Unit = item.Unit.Trim(),
                TargetValue = item.TargetValue,
                MinThreshold = item.MinThreshold ?? 0m,
                MaxCap = item.MaxCap > 0 ? item.MaxCap : 150m,
                Weight = item.Weight,
                ScoringFormula = string.IsNullOrWhiteSpace(item.ScoringFormula) ? "HigherIsBetter" : item.ScoringFormula
            });
        }

        // Chuyển sang chờ duyệt lại cho phiên bản mới
        assignment.Status = AssignmentStatus.Pending_Approval;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DiffComparisonDto> GetRevisionDiffAsync(long assignmentId)
    {
        var assignment = await _context.KpiAssignments
            .Include(a => a.Employee)
            .Include(a => a.Period)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        var result = new DiffComparisonDto
        {
            AssignmentId = assignmentId,
            EmployeeName = assignment?.Employee?.FullName ?? string.Empty,
            PeriodName = assignment?.Period?.Name ?? string.Empty,
            VersionFrom = 1,
            VersionTo = 2,
            RevisionTime = DateTime.UtcNow
        };

        var lastRevision = await _context.AuditLogs
            .Where(a => a.EntityName == "KpiAssignment" && a.EntityId == assignmentId && a.Action == "ASSIGNMENT_REVISION")
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastRevision == null || string.IsNullOrWhiteSpace(lastRevision.OldValues) || string.IsNullOrWhiteSpace(lastRevision.NewValues))
        {
            return result;
        }

        result.RevisionReason = lastRevision.UserAgent ?? "Điều chỉnh mục tiêu giữa kỳ";
        result.RevisionTime = lastRevision.CreatedAt;

        try
        {
            var oldItems = JsonSerializer.Deserialize<List<AssignmentItemRequest>>(lastRevision.OldValues) ?? new();
            var newItems = JsonSerializer.Deserialize<List<AssignmentItemRequest>>(lastRevision.NewValues) ?? new();

            var allKpiIds = oldItems.Select(x => x.KpiItemId).Union(newItems.Select(x => x.KpiItemId)).Distinct();

            foreach (var kpiId in allKpiIds)
            {
                var oldItem = oldItems.FirstOrDefault(x => x.KpiItemId == kpiId);
                var newItem = newItems.FirstOrDefault(x => x.KpiItemId == kpiId);

                if (oldItem == null && newItem != null)
                {
                    result.Differences.Add(new DiffItemDto
                    {
                        KpiItemId = kpiId,
                        KpiName = newItem.KpiName,
                        ChangeType = "ADDED",
                        OldWeight = 0,
                        NewWeight = newItem.Weight,
                        OldTarget = 0,
                        NewTarget = newItem.TargetValue,
                        Unit = newItem.Unit,
                        Description = "Chỉ tiêu bổ sung mới"
                    });
                }
                else if (oldItem != null && newItem == null)
                {
                    result.Differences.Add(new DiffItemDto
                    {
                        KpiItemId = kpiId,
                        KpiName = oldItem.KpiName,
                        ChangeType = "REMOVED",
                        OldWeight = oldItem.Weight,
                        NewWeight = 0,
                        OldTarget = oldItem.TargetValue,
                        NewTarget = 0,
                        Unit = oldItem.Unit,
                        Description = "Chỉ tiêu bị loại bỏ"
                    });
                }
                else if (oldItem != null && newItem != null)
                {
                    bool isChanged = oldItem.Weight != newItem.Weight || oldItem.TargetValue != newItem.TargetValue;
                    result.Differences.Add(new DiffItemDto
                    {
                        KpiItemId = kpiId,
                        KpiName = newItem.KpiName,
                        ChangeType = isChanged ? "MODIFIED" : "UNCHANGED",
                        OldWeight = oldItem.Weight,
                        NewWeight = newItem.Weight,
                        OldTarget = oldItem.TargetValue,
                        NewTarget = newItem.TargetValue,
                        Unit = newItem.Unit,
                        Description = isChanged ? "Điều chỉnh trọng số hoặc mục tiêu" : "Giữ nguyên"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phân tích Diff revision");
        }

        return result;
    }

    public async Task<bool> DeleteDraftAssignmentAsync(long assignmentId, long currentUserId)
    {
        var assignment = await _context.KpiAssignments
            .Include(a => a.AssignmentDetails)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null) return false;

        if (assignment.Status != AssignmentStatus.Draft)
        {
            throw new BusinessRuleException("BR-DELETE", "Chỉ được phép xóa bản giao khi đang ở trạng thái Bản nháp (Draft).");
        }

        _context.KpiAssignmentDetails.RemoveRange(assignment.AssignmentDetails);
        _context.KpiAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        await LogAuditAsync(currentUserId, "DELETE_DRAFT_ASSIGNMENT", "KpiAssignment", assignmentId, "Đã xóa bản giao nháp");
        return true;
    }
    #endregion

    #region Helper Methods
    private static KpiAssignmentDto MapToDto(KpiAssignment a)
    {
        return new KpiAssignmentDto
        {
            Id = a.Id,
            PeriodId = a.PeriodId,
            PeriodName = a.Period?.Name ?? string.Empty,
            PeriodYear = a.Period?.Year ?? DateTime.UtcNow.Year,
            EmployeeId = a.EmployeeId,
            StaffCode = a.Employee?.StaffCode ?? string.Empty,
            EmployeeName = a.Employee?.FullName ?? string.Empty,
            DepartmentName = a.Employee?.Department?.Name ?? string.Empty,
            PositionName = a.Employee?.Position?.Name ?? string.Empty,
            ApproverId = a.ApproverId,
            ApproverName = a.Approver?.Username ?? string.Empty,
            Title = a.Title,
            TotalWeight = a.TotalWeight,
            Status = a.Status,
            CommittedAt = a.CommittedAt,
            ApprovedAt = a.ApprovedAt,
            CreatedAt = a.CreatedAt,
            Details = a.AssignmentDetails.Select(d => new KpiAssignmentDetailDto
            {
                Id = d.Id,
                AssignmentId = d.AssignmentId,
                KpiItemId = d.KpiItemId,
                KpiCode = d.KpiItem?.Code ?? string.Empty,
                KpiName = d.KpiName,
                Perspective = d.KpiItem?.Category?.Perspective.ToString() ?? "Quy trình nội bộ",
                Unit = d.Unit,
                TargetValue = d.TargetValue,
                MinThreshold = d.MinThreshold,
                MaxCap = d.MaxCap,
                Weight = d.Weight,
                ScoringFormula = d.ScoringFormula,
                Description = d.KpiItem?.Description,
                EvidenceCount = d.Evidences?.Count ?? 0
            }).ToList()
        };
    }

    private async Task LogAuditAsync(long userId, string action, string entityName, long entityId, string details)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId > 0 ? userId : null,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                NewValues = details,
                IpAddress = "::1",
                UserAgent = "KPI.Web Planning Module",
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Không thể ghi AuditLog: {Message}", ex.Message);
        }
    }
    #endregion
}
