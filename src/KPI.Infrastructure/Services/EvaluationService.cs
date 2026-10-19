using System.IO;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Evaluation;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Thực thi nghiệp vụ phân hệ Đánh giá hiệu suất & Workflow Engine
/// </summary>
public class EvaluationService : IEvaluationService
{
    private readonly KpiDbContext _context;
    private readonly IKpiCalculationService _kpiCalculationService;
    private readonly IDecree90Service _decree90Service;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".png", ".jpg", ".jpeg", ".zip", ".rar"
    };

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB

    public EvaluationService(
        KpiDbContext context,
        IKpiCalculationService kpiCalculationService,
        IDecree90Service decree90Service,
        IWebHostEnvironment env)
    {
        _context = context;
        _kpiCalculationService = kpiCalculationService;
        _decree90Service = decree90Service;
        _env = env;
    }

    /// <summary>
    /// Lấy hoặc tự động khởi tạo phiếu đánh giá cho một bản giao KPI
    /// </summary>
    public async Task<EvaluationDto?> GetOrCreateEvaluationForAssignmentAsync(long assignmentId, long currentUserId)
    {
        var assignment = await _context.KpiAssignments
            .Include(a => a.Employee)
                .ThenInclude(e => e.Position)
            .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
            .Include(a => a.Period)
            .Include(a => a.AssignmentDetails)
                .ThenInclude(d => d.Evidences)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return null;
        }

        var evaluation = await _context.Evaluations
            .Include(e => e.EvaluationDetails)
                .ThenInclude(d => d.AssignmentDetail)
                    .ThenInclude(ad => ad.Evidences)
            .Include(e => e.Workflow)
            .Include(e => e.WorkflowHistories)
                .ThenInclude(h => h.Actor)
            .Include(e => e.PeerReviews)
                .ThenInclude(p => p.Reviewer)
            .Include(e => e.Disputes)
            .FirstOrDefaultAsync(e => e.AssignmentId == assignmentId);

        if (evaluation == null)
        {
            // Tự động khởi tạo Evaluation và EvaluationDetails từ KpiAssignment
            evaluation = new Evaluation
            {
                AssignmentId = assignment.Id,
                EmployeeId = assignment.EmployeeId,
                PeriodId = assignment.PeriodId,
                SelfTotalScore = 0.0m,
                ManagerTotalScore = 0.0m,
                FinalScore = 0.0m,
                Status = EvaluationStatus.Not_Started,
                IsDisputed = false,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var detail in assignment.AssignmentDetails)
            {
                evaluation.EvaluationDetails.Add(new EvaluationDetail
                {
                    Evaluation = evaluation,
                    AssignmentDetailId = detail.Id,
                    ActualValue = 0.0m,
                    CompletionRate = 0.0m,
                    SelfScore = 0.0m,
                    ManagerScore = 0.0m,
                    ScoreGap = 0.0m
                });
            }

            var workflow = new EvaluationWorkflow
            {
                Evaluation = evaluation,
                CurrentStep = "Self_Assessment",
                Status = "Running"
            };
            evaluation.Workflow = workflow;

            _context.Evaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            // Tải lại đầy đủ navigation properties
            return await GetEvaluationByIdAsync(evaluation.Id, currentUserId);
        }

        return MapToDto(evaluation, assignment);
    }

    /// <summary>
    /// Lấy chi tiết phiếu đánh giá theo Id
    /// </summary>
    public async Task<EvaluationDto?> GetEvaluationByIdAsync(long evaluationId, long currentUserId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
                    .ThenInclude(emp => emp.Position)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
                    .ThenInclude(emp => emp.Department)
            .Include(e => e.Period)
            .Include(e => e.EvaluationDetails)
                .ThenInclude(d => d.AssignmentDetail)
                    .ThenInclude(ad => ad.Evidences)
            .Include(e => e.WorkflowHistories)
                .ThenInclude(h => h.Actor)
            .Include(e => e.PeerReviews)
                .ThenInclude(p => p.Reviewer)
            .Include(e => e.Disputes)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        if (evaluation == null) return null;

        return MapToDto(evaluation, evaluation.Assignment);
    }

    /// <summary>
    /// Lấy phiếu đánh giá kỳ hiện tại của cán bộ đang đăng nhập
    /// </summary>
    public async Task<EvaluationDto?> GetMyActiveEvaluationAsync(long currentUserId)
    {
        var employee = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(e => e.UserId == currentUserId);

        if (employee == null) return null;

        var period = await _context.EvaluationPeriods
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        if (period == null) return null;

        var assignment = await _context.KpiAssignments
            .Where(a => a.EmployeeId == employee.Id && a.PeriodId == period.Id)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (assignment == null) return null;

        return await GetOrCreateEvaluationForAssignmentAsync(assignment.Id, currentUserId);
    }

    /// <summary>
    /// Lấy danh sách hồ sơ cần Quản lý thẩm định
    /// </summary>
    public async Task<List<EvaluationDto>> GetPendingManagerReviewsAsync(long managerUserId, long? periodId = null, long? departmentId = null)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
                    .ThenInclude(emp => emp.Position)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
                    .ThenInclude(emp => emp.Department)
            .Include(e => e.Period)
            .Include(e => e.EvaluationDetails)
                .ThenInclude(d => d.AssignmentDetail)
                    .ThenInclude(ad => ad.Evidences)
            .AsQueryable();

        if (periodId.HasValue)
        {
            query = query.Where(e => e.PeriodId == periodId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.Assignment.Employee.DepartmentId == departmentId.Value);
        }

        var list = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return list.Select(e => MapToDto(e, e.Assignment)).ToList();
    }

    /// <summary>
    /// Lấy danh sách hồ sơ trình Lãnh đạo viện phê chuẩn
    /// </summary>
    public async Task<List<EvaluationDto>> GetPendingDirectorApprovalsAsync(long? periodId = null, long? departmentId = null)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
                    .ThenInclude(emp => emp.Position)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
                    .ThenInclude(emp => emp.Department)
            .Include(e => e.Period)
            .Include(e => e.EvaluationDetails)
                .ThenInclude(d => d.AssignmentDetail)
                    .ThenInclude(ad => ad.Evidences)
            .AsQueryable();

        if (periodId.HasValue)
        {
            query = query.Where(e => e.PeriodId == periodId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.Assignment.Employee.DepartmentId == departmentId.Value);
        }

        var list = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return list.Select(e => MapToDto(e, e.Assignment)).ToList();
    }

    /// <summary>
    /// Lưu nháp hoặc nộp chính thức bản tự đánh giá của nhân viên (BR-09, BR-10, BR-12)
    /// </summary>
    public async Task<EvaluationDto> SaveOrSubmitSelfAssessmentAsync(SubmitSelfAssessmentRequest request, long currentUserId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
            .Include(e => e.Period)
            .Include(e => e.EvaluationDetails)
                .ThenInclude(d => d.AssignmentDetail)
                    .ThenInclude(ad => ad.Evidences)
            .Include(e => e.Workflow)
            .FirstOrDefaultAsync(e => e.Id == request.EvaluationId);

        if (evaluation == null)
        {
            throw new BusinessRuleException("BR-404", "Không tìm thấy hồ sơ đánh giá.");
        }

        // Kiểm tra quyền: Phải là chính chủ cán bộ hoặc Admin
        var user = await _context.Users.FindAsync(currentUserId);
        if (evaluation.Assignment.Employee.UserId != currentUserId && user?.Username != "admin" && user?.Username != "director")
        {
            throw new BusinessRuleException("BR-AUTH", "Bạn không có quyền thực hiện tự đánh giá trên hồ sơ của cán bộ khác.");
        }

        // Kiểm tra trạng thái hồ sơ
        if (evaluation.IsLocked || evaluation.Status == EvaluationStatus.Final_Approved || evaluation.Status == EvaluationStatus.Locked)
        {
            throw new BusinessRuleException("BR-15", "Hồ sơ đánh giá đã được phê duyệt và đóng băng (Locked). Không thể chỉnh sửa.");
        }

        if (!request.IsDraft && evaluation.Status != EvaluationStatus.Not_Started && evaluation.Status != EvaluationStatus.Self_Assessing)
        {
            throw new BusinessRuleException("BR-04", $"Hồ sơ đang ở trạng thái '{EvaluationDto.GetStatusDisplayName(evaluation.Status)}'. Không thể nộp lại.");
        }

        // BR-12: Kiểm tra thời hạn nộp tự đánh giá
        if (!request.IsDraft && DateTime.UtcNow > evaluation.Period.SelfDeadline)
        {
            throw new BusinessRuleException("BR-12", 
                $"Đã hết thời hạn nộp tự đánh giá cho kỳ '{evaluation.Period.Name}' (Hạn chót: {evaluation.Period.SelfDeadline:dd/MM/yyyy HH:mm}). Hồ sơ bị khóa nộp trễ.");
        }

        // Cập nhật từng chi tiết chỉ tiêu
        foreach (var item in request.Items)
        {
            var detail = evaluation.EvaluationDetails.FirstOrDefault(d => d.AssignmentDetailId == item.AssignmentDetailId);
            if (detail == null) continue;

            var ad = detail.AssignmentDetail;
            detail.ActualValue = item.ActualValue;
            detail.SelfRemarks = item.SelfRemarks;

            // BR-10: Tính completion rate có áp trần MaxCap
            var rate = _kpiCalculationService.CalculateCompletionRate(
                item.ActualValue, 
                ad.TargetValue, 
                ad.MaxCap, 
                ad.ScoringFormula);

            detail.CompletionRate = rate;
            detail.SelfScore = Math.Round(rate, 2); // Điểm tự chấm theo thang 100/150 của chỉ tiêu

            // BR-09: Khi kết quả đạt >= 120% và nộp chính thức -> Bắt buộc phải có minh chứng
            if (!request.IsDraft && rate >= 120.0m)
            {
                var evidenceCount = await _context.KpiEvidences
                    .CountAsync(ev => ev.AssignmentDetailId == ad.Id);

                if (evidenceCount == 0)
                {
                    throw new BusinessRuleException("BR-09", 
                        $"Chỉ tiêu '{ad.KpiName}' đạt {rate}% (>= 120%). Quy định BR-09 bắt buộc phải tải lên tệp minh chứng số hóa chứng minh kết quả vượt trần trước khi nộp chính thức.");
                }
            }
        }

        // Tính tổng điểm tự chấm: Sum(SelfScore * Weight / 100)
        var totalSelf = evaluation.EvaluationDetails.Sum(d => (d.SelfScore * d.AssignmentDetail.Weight) / 100.0m);
        evaluation.SelfTotalScore = Math.Round(totalSelf, 2);
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (request.IsDraft)
        {
            evaluation.Status = EvaluationStatus.Self_Assessing;
            await RecordAuditAsync(currentUserId, "SELF_ASSESS_DRAFT", evaluation.Id, 
                $"Cán bộ lưu nháp phiếu tự đánh giá ({evaluation.SelfTotalScore} điểm)");
        }
        else
        {
            var oldState = evaluation.Status.ToString();
            evaluation.Status = EvaluationStatus.Self_Submitted;
            
            // Cập nhật workflow
            if (evaluation.Workflow != null)
            {
                evaluation.Workflow.CurrentStep = "Manager_Review";
            }

            // Ghi nhật ký luân chuyển
            _context.WorkflowHistories.Add(new WorkflowHistory
            {
                EvaluationId = evaluation.Id,
                ActorId = currentUserId,
                FromState = oldState,
                ToState = EvaluationStatus.Self_Submitted.ToString(),
                Comments = request.OverallRemarks ?? "Cán bộ hoàn tất nộp tự đánh giá",
                ActionTime = DateTime.UtcNow
            });

            await RecordAuditAsync(currentUserId, "SUBMIT_SELF_ASSESSMENT", evaluation.Id, 
                $"Cán bộ nộp chính thức phiếu tự đánh giá ({evaluation.SelfTotalScore} điểm). Chuyển quản lý thẩm định.");
        }

        await _context.SaveChangesAsync();
        return (await GetEvaluationByIdAsync(evaluation.Id, currentUserId))!;
    }

    /// <summary>
    /// Lưu nháp hoặc hoàn tất thẩm định của Quản lý (BR-04, BR-05)
    /// </summary>
    public async Task<EvaluationDto> SaveOrSubmitManagerReviewAsync(SubmitManagerReviewRequest request, long currentUserId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
            .Include(e => e.Period)
            .Include(e => e.EvaluationDetails)
                .ThenInclude(d => d.AssignmentDetail)
            .Include(e => e.Workflow)
            .FirstOrDefaultAsync(e => e.Id == request.EvaluationId);

        if (evaluation == null)
        {
            throw new BusinessRuleException("BR-404", "Không tìm thấy hồ sơ đánh giá.");
        }

        // BR-04: Quản lý chỉ được thẩm định khi nhân viên đã nộp tự chấm
        if (evaluation.Status != EvaluationStatus.Self_Submitted && evaluation.Status != EvaluationStatus.Manager_Reviewing)
        {
            throw new BusinessRuleException("BR-04", 
                $"Quản lý chỉ được phép thẩm định khi nhân viên đã nộp bản tự đánh giá. Trạng thái hiện tại: {EvaluationDto.GetStatusDisplayName(evaluation.Status)}");
        }

        if (evaluation.IsLocked)
        {
            throw new BusinessRuleException("BR-15", "Hồ sơ đánh giá đã bị khóa.");
        }

        // Cập nhật từng chỉ tiêu thẩm định
        foreach (var item in request.Items)
        {
            var detail = evaluation.EvaluationDetails.FirstOrDefault(d => d.Id == item.DetailId || d.AssignmentDetailId == item.DetailId);
            if (detail == null) continue;

            detail.ManagerScore = Math.Round(item.ManagerScore, 2);
            detail.ManagerRemarks = item.ManagerRemarks;
            detail.ScoreGapReason = item.ScoreGapReason;
            detail.ScoreGap = Math.Abs(detail.SelfScore - detail.ManagerScore);

            // BR-05: Khi độ lệch điểm > 10% bắt buộc giải trình >= 50 ký tự
            if (!request.IsDraft && detail.ScoreGap > 10.0m)
            {
                if (string.IsNullOrWhiteSpace(item.ScoreGapReason) || item.ScoreGapReason.Trim().Length < 50)
                {
                    throw new BusinessRuleException("BR-05", 
                        $"Chỉ tiêu '{detail.AssignmentDetail.KpiName}' có chênh lệch điểm giữa Tự chấm ({detail.SelfScore}) và Quản lý chấm ({detail.ManagerScore}) là {detail.ScoreGap} điểm (> 10 điểm). Quy định BR-05 bắt buộc phải có nội dung giải trình tối thiểu 50 ký tự. Hiện tại: {item.ScoreGapReason?.Trim().Length ?? 0} ký tự.");
                }
            }
        }

        // Tính tổng điểm Quản lý thẩm định
        var totalMgr = evaluation.EvaluationDetails.Sum(d => (d.ManagerScore * d.AssignmentDetail.Weight) / 100.0m);
        evaluation.ManagerTotalScore = Math.Round(totalMgr, 2);
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (request.IsDraft)
        {
            evaluation.Status = EvaluationStatus.Manager_Reviewing;
            await RecordAuditAsync(currentUserId, "MANAGER_REVIEW_DRAFT", evaluation.Id, 
                $"Quản lý lưu nháp thẩm định ({evaluation.ManagerTotalScore} điểm)");
        }
        else
        {
            var oldState = evaluation.Status.ToString();
            evaluation.Status = EvaluationStatus.Pending_Director_Approval;

            if (evaluation.Workflow != null)
            {
                evaluation.Workflow.CurrentStep = "Director_Approval";
            }

            _context.WorkflowHistories.Add(new WorkflowHistory
            {
                EvaluationId = evaluation.Id,
                ActorId = currentUserId,
                FromState = oldState,
                ToState = EvaluationStatus.Pending_Director_Approval.ToString(),
                Comments = request.OverallRemarks ?? "Quản lý hoàn thành thẩm định và trình Lãnh đạo",
                ActionTime = DateTime.UtcNow
            });

            await RecordAuditAsync(currentUserId, "SUBMIT_MANAGER_REVIEW", evaluation.Id, 
                $"Quản lý hoàn thành thẩm định ({evaluation.ManagerTotalScore} điểm). Chuyển Giám đốc phê duyệt.");
        }

        await _context.SaveChangesAsync();
        return (await GetEvaluationByIdAsync(evaluation.Id, currentUserId))!;
    }

    /// <summary>
    /// Lãnh đạo viện phê duyệt chính thức và đóng băng dữ liệu (BR-15)
    /// </summary>
    public async Task<EvaluationDto> ApproveByDirectorAsync(DirectorApproveRequest request, long currentUserId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Workflow)
            .FirstOrDefaultAsync(e => e.Id == request.EvaluationId);

        if (evaluation == null)
        {
            throw new BusinessRuleException("BR-404", "Không tìm thấy hồ sơ đánh giá.");
        }

        if (evaluation.IsLocked)
        {
            throw new BusinessRuleException("BR-15", "Hồ sơ đánh giá đã được phê duyệt và đóng băng trước đó.");
        }

        var oldState = evaluation.Status.ToString();
        evaluation.FinalScore = Math.Round(request.FinalScore, 2);
        evaluation.FinalClassification = request.FinalClassification;
        evaluation.Status = EvaluationStatus.Final_Approved;
        evaluation.IsLocked = true; // BR-15: Lock toàn bộ
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (evaluation.Workflow != null)
        {
            evaluation.Workflow.CurrentStep = "Final_Approved";
            evaluation.Workflow.Status = "Completed";
        }

        _context.WorkflowHistories.Add(new WorkflowHistory
        {
            EvaluationId = evaluation.Id,
            ActorId = currentUserId,
            FromState = oldState,
            ToState = EvaluationStatus.Final_Approved.ToString(),
            Comments = request.Remarks ?? $"Lãnh đạo phê chuẩn kết quả: {evaluation.FinalClassification} ({evaluation.FinalScore} điểm)",
            ActionTime = DateTime.UtcNow
        });

        await RecordAuditAsync(currentUserId, "FINAL_APPROVE_EVALUATION", evaluation.Id, 
            $"Lãnh đạo phê chuẩn kết quả: {evaluation.FinalClassification} ({evaluation.FinalScore} điểm). Kích hoạt khóa BR-15.");

        await _context.SaveChangesAsync();
        return (await GetEvaluationByIdAsync(evaluation.Id, currentUserId))!;
    }

    /// <summary>
    /// Trả lại hồ sơ đánh giá cho Quản lý hoặc Nhân viên
    /// </summary>
    public async Task<EvaluationDto> RejectEvaluationAsync(RejectEvaluationRequest request, long currentUserId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Workflow)
            .FirstOrDefaultAsync(e => e.Id == request.EvaluationId);

        if (evaluation == null)
        {
            throw new BusinessRuleException("BR-404", "Không tìm thấy hồ sơ đánh giá.");
        }

        if (evaluation.IsLocked)
        {
            throw new BusinessRuleException("BR-15", "Hồ sơ đã bị khóa, không thể trả lại.");
        }

        var oldState = evaluation.Status.ToString();
        EvaluationStatus nextState;

        if (request.TargetRole.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            nextState = EvaluationStatus.Self_Assessing;
            if (evaluation.Workflow != null) evaluation.Workflow.CurrentStep = "Self_Assessment";
        }
        else
        {
            nextState = EvaluationStatus.Manager_Reviewing;
            if (evaluation.Workflow != null) evaluation.Workflow.CurrentStep = "Manager_Review";
        }

        evaluation.Status = nextState;
        evaluation.UpdatedAt = DateTime.UtcNow;

        _context.WorkflowHistories.Add(new WorkflowHistory
        {
            EvaluationId = evaluation.Id,
            ActorId = currentUserId,
            FromState = oldState,
            ToState = nextState.ToString(),
            Comments = $"Trả hồ sơ: {request.Reason}",
            ActionTime = DateTime.UtcNow
        });

        await RecordAuditAsync(currentUserId, "REJECT_EVALUATION", evaluation.Id, 
            $"Trả hồ sơ về bước '{nextState}'. Lý do: {request.Reason}");

        await _context.SaveChangesAsync();
        return (await GetEvaluationByIdAsync(evaluation.Id, currentUserId))!;
    }

    /// <summary>
    /// Tải lên tệp minh chứng số hóa cho một chỉ tiêu (BR-09)
    /// </summary>
    public async Task<EvidenceItemDto> UploadEvidenceAsync(long assignmentDetailId, string fileName, string fileType, long fileSize, Stream fileStream, string? description, long currentUserId)
    {
        var detail = await _context.KpiAssignmentDetails
            .Include(d => d.Assignment)
            .FirstOrDefaultAsync(d => d.Id == assignmentDetailId);

        if (detail == null)
        {
            throw new BusinessRuleException("BR-404", "Không tìm thấy chỉ tiêu KPI.");
        }

        if (fileSize > MaxFileSizeBytes)
        {
            throw new BusinessRuleException("BR-FILE-SIZE", $"Dung lượng tệp vượt quá giới hạn 25MB cho phép ({fileSize / (1024 * 1024):F1}MB).");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new BusinessRuleException("BR-FILE-TYPE", $"Định dạng tệp '{extension}' không hợp lệ. Hệ thống chỉ hỗ trợ: PDF, DOCX, XLSX, Ảnh (PNG, JPG) và Tệp nén (ZIP, RAR).");
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "evidences");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var safeFileName = $"evidence_{assignmentDetailId}_{DateTime.UtcNow.Ticks}{extension}";
        var physicalPath = Path.Combine(uploadsFolder, safeFileName);
        var relativeUrl = $"/uploads/evidences/{safeFileName}";

        using (var fileDestStream = new FileStream(physicalPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileDestStream);
        }

        var evidence = new KpiEvidence
        {
            AssignmentDetailId = assignmentDetailId,
            FileName = Path.GetFileName(fileName),
            FilePath = relativeUrl,
            FileType = extension.TrimStart('.').ToUpperInvariant(),
            FileSize = fileSize,
            Description = description,
            UploadedAt = DateTime.UtcNow
        };

        _context.KpiEvidences.Add(evidence);
        await _context.SaveChangesAsync();

        await RecordAuditAsync(currentUserId, "UPLOAD_EVIDENCE", evidence.Id, 
            $"Đính kèm minh chứng '{evidence.FileName}' ({evidence.FileSize} bytes) cho chỉ tiêu '{detail.KpiName}'");

        return new EvidenceItemDto
        {
            Id = evidence.Id,
            AssignmentDetailId = evidence.AssignmentDetailId,
            FileName = evidence.FileName,
            FilePath = evidence.FilePath,
            FileType = evidence.FileType,
            FileSize = evidence.FileSize,
            Description = evidence.Description,
            UploadedAt = evidence.UploadedAt
        };
    }

    /// <summary>
    /// Xóa tệp minh chứng số hóa
    /// </summary>
    public async Task<bool> DeleteEvidenceAsync(long evidenceId, long currentUserId)
    {
        var evidence = await _context.KpiEvidences
            .Include(e => e.AssignmentDetail)
                .ThenInclude(ad => ad.Assignment)
            .FirstOrDefaultAsync(e => e.Id == evidenceId);

        if (evidence == null) return false;

        // Xóa file vật lý
        try
        {
            var physicalPath = Path.Combine(_env.WebRootPath, evidence.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch { /* Bỏ qua lỗi xóa tệp vật lý */ }

        _context.KpiEvidences.Remove(evidence);
        await _context.SaveChangesAsync();

        await RecordAuditAsync(currentUserId, "DELETE_EVIDENCE", evidenceId, $"Xóa tệp minh chứng '{evidence.FileName}'");
        return true;
    }

    /// <summary>
    /// Nộp đơn khiếu nại kết quả đánh giá trong 72h (BR-14)
    /// </summary>
    public async Task<EvaluationDisputeDto> SubmitDisputeAsync(SubmitDisputeRequest request, long currentUserId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Employee)
            .FirstOrDefaultAsync(e => e.Id == request.EvaluationId);

        if (evaluation == null)
        {
            throw new BusinessRuleException("BR-404", "Không tìm thấy hồ sơ đánh giá.");
        }

        if (evaluation.Status != EvaluationStatus.Final_Approved && evaluation.Status != EvaluationStatus.Locked)
        {
            throw new BusinessRuleException("BR-DISPUTE", "Chỉ có thể khiếu nại đối với hồ sơ đã được phê chuẩn kết quả.");
        }

        // Kiểm tra thời hạn 72h (3 ngày) kể từ khi phê duyệt
        var approvedAt = evaluation.UpdatedAt ?? evaluation.CreatedAt;
        var elapsed = (DateTime.UtcNow - approvedAt).TotalHours;
        if (elapsed > 72.0)
        {
            throw new BusinessRuleException("BR-14", 
                $"Đã quá thời hạn khiếu nại 72 giờ kể từ khi công bố kết quả (Thời gian trôi qua: {elapsed:F1} giờ). Quyền khiếu nại đã đóng.");
        }

        var dispute = new EvaluationDispute
        {
            EvaluationId = evaluation.Id,
            AppellantId = evaluation.EmployeeId,
            Reason = request.Reason,
            ProposedAdjustment = request.ProposedAdjustment,
            Status = DisputeStatus.Open,
            SubmittedAt = DateTime.UtcNow
        };

        evaluation.IsDisputed = true;
        _context.EvaluationDisputes.Add(dispute);
        await _context.SaveChangesAsync();

        await RecordAuditAsync(currentUserId, "SUBMIT_DISPUTE", dispute.Id, 
            $"Cán bộ nộp đơn khiếu nại kết quả đánh giá. Lý do: {request.Reason}");

        return new EvaluationDisputeDto
        {
            Id = dispute.Id,
            EvaluationId = dispute.EvaluationId,
            AppellantId = dispute.AppellantId,
            AppellantName = evaluation.Assignment.Employee.FullName,
            Reason = dispute.Reason,
            ProposedAdjustment = dispute.ProposedAdjustment,
            Status = dispute.Status,
            SubmittedAt = dispute.SubmittedAt
        };
    }

    /// <summary>
    /// Xử lý và giải quyết đơn khiếu nại kết quả đánh giá
    /// </summary>
    public async Task<EvaluationDisputeDto> ResolveDisputeAsync(ResolveDisputeRequest request, long currentUserId)
    {
        var dispute = await _context.EvaluationDisputes
            .Include(d => d.Evaluation)
            .Include(d => d.Appellant)
            .FirstOrDefaultAsync(d => d.Id == request.DisputeId);

        if (dispute == null)
        {
            throw new BusinessRuleException("BR-404", "Không tìm thấy đơn khiếu nại.");
        }

        dispute.ResolverId = currentUserId;
        dispute.ResolutionNotes = request.ResolutionNotes;
        dispute.ResolvedAt = DateTime.UtcNow;

        if (request.IsAccepted)
        {
            dispute.Status = DisputeStatus.Resolved;
            if (request.AdjustedScore.HasValue)
            {
                dispute.Evaluation.FinalScore = request.AdjustedScore.Value;
            }
            if (!string.IsNullOrEmpty(request.AdjustedClassification))
            {
                dispute.Evaluation.FinalClassification = request.AdjustedClassification;
            }
        }
        else
        {
            dispute.Status = DisputeStatus.Rejected;
        }

        dispute.Evaluation.IsDisputed = false;
        await _context.SaveChangesAsync();

        await RecordAuditAsync(currentUserId, "RESOLVE_DISPUTE", dispute.Id, 
            $"Xử lý đơn khiếu nại: {dispute.Status}. Ghi chú: {request.ResolutionNotes}");

        return new EvaluationDisputeDto
        {
            Id = dispute.Id,
            EvaluationId = dispute.EvaluationId,
            AppellantId = dispute.AppellantId,
            AppellantName = dispute.Appellant.FullName,
            Reason = dispute.Reason,
            ProposedAdjustment = dispute.ProposedAdjustment,
            Status = dispute.Status,
            ResolverId = dispute.ResolverId,
            ResolutionNotes = dispute.ResolutionNotes,
            SubmittedAt = dispute.SubmittedAt,
            ResolvedAt = dispute.ResolvedAt
        };
    }

    /// <summary>
    /// Lấy danh sách các đơn khiếu nại trong kỳ
    /// </summary>
    public async Task<List<EvaluationDisputeDto>> GetDisputesAsync(long? periodId = null)
    {
        var query = _context.EvaluationDisputes
            .Include(d => d.Evaluation)
            .Include(d => d.Appellant)
            .Include(d => d.Resolver)
            .AsQueryable();

        if (periodId.HasValue)
        {
            query = query.Where(d => d.Evaluation.PeriodId == periodId.Value);
        }

        var list = await query
            .OrderByDescending(d => d.SubmittedAt)
            .ToListAsync();

        return list.Select(d => new EvaluationDisputeDto
        {
            Id = d.Id,
            EvaluationId = d.EvaluationId,
            AppellantId = d.AppellantId,
            AppellantName = d.Appellant.FullName,
            Reason = d.Reason,
            ProposedAdjustment = d.ProposedAdjustment,
            ResolverId = d.ResolverId,
            ResolverName = d.Resolver?.Username,
            ResolutionNotes = d.ResolutionNotes,
            Status = d.Status,
            SubmittedAt = d.SubmittedAt,
            ResolvedAt = d.ResolvedAt
        }).ToList();
    }

    /// <summary>
    /// Đánh giá 360 độ từ đồng nghiệp (Peer Review)
    /// </summary>
    public async Task<PeerReviewDto> SubmitPeerReviewAsync(SubmitPeerReviewRequest request, long currentUserId)
    {
        var reviewer = await _context.EmployeeProfiles.FirstOrDefaultAsync(e => e.UserId == currentUserId);
        if (reviewer == null)
        {
            throw new BusinessRuleException("BR-AUTH", "Không tìm thấy hồ sơ cán bộ của người đánh giá.");
        }

        var evaluation = await _context.Evaluations.FindAsync(request.EvaluationId);
        if (evaluation == null)
        {
            throw new BusinessRuleException("BR-404", "Không tìm thấy hồ sơ đánh giá.");
        }

        var peerReview = new PeerReview
        {
            EvaluationId = evaluation.Id,
            ReviewerId = reviewer.Id,
            BehaviorScore = Math.Clamp(request.BehaviorScore, 1.0m, 10.0m),
            FeedbackComments = request.FeedbackComments,
            IsAnonymous = request.IsAnonymous,
            SubmittedAt = DateTime.UtcNow
        };

        _context.PeerReviews.Add(peerReview);
        await _context.SaveChangesAsync();

        await RecordAuditAsync(currentUserId, "SUBMIT_PEER_REVIEW", peerReview.Id, 
            $"Gửi phiếu đánh giá đồng nghiệp ({peerReview.BehaviorScore}/10 điểm)");

        return new PeerReviewDto
        {
            Id = peerReview.Id,
            EvaluationId = peerReview.EvaluationId,
            ReviewerId = peerReview.ReviewerId,
            ReviewerName = peerReview.IsAnonymous ? "Đồng nghiệp (Ẩn danh)" : reviewer.FullName,
            BehaviorScore = peerReview.BehaviorScore,
            FeedbackComments = peerReview.FeedbackComments,
            IsAnonymous = peerReview.IsAnonymous,
            SubmittedAt = peerReview.SubmittedAt
        };
    }

    /// <summary>
    /// Tự động quét và khóa/gán 0 điểm cho các hồ sơ quá hạn nộp tự chấm (BR-12)
    /// </summary>
    public async Task<int> CheckAndAutoCloseExpiredEvaluationsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredPeriods = await _context.EvaluationPeriods
            .Where(p => p.SelfDeadline < now)
            .Select(p => p.Id)
            .ToListAsync();

        if (!expiredPeriods.Any()) return 0;

        var overdueEvaluations = await _context.Evaluations
            .Include(e => e.EvaluationDetails)
            .Where(e => expiredPeriods.Contains(e.PeriodId) && 
                       (e.Status == EvaluationStatus.Not_Started || e.Status == EvaluationStatus.Self_Assessing))
            .ToListAsync();

        int closedCount = 0;
        foreach (var eval in overdueEvaluations)
        {
            var oldState = eval.Status.ToString();
            eval.SelfTotalScore = 0.0m;
            foreach (var detail in eval.EvaluationDetails)
            {
                detail.ActualValue = 0.0m;
                detail.CompletionRate = 0.0m;
                detail.SelfScore = 0.0m;
                detail.SelfRemarks = "Hệ thống tự động gán 0 điểm do quá thời hạn nộp tự đánh giá (BR-12)";
            }

            eval.Status = EvaluationStatus.Self_Submitted;
            eval.UpdatedAt = now;

            _context.WorkflowHistories.Add(new WorkflowHistory
            {
                EvaluationId = eval.Id,
                ActorId = 1, // System admin / Automated job
                FromState = oldState,
                ToState = EvaluationStatus.Self_Submitted.ToString(),
                Comments = "Hệ thống tự động chốt nộp 0 điểm do quá hạn nộp (BR-12)",
                ActionTime = now
            });

            closedCount++;
        }

        if (closedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return closedCount;
    }

    #region Helpers & Mappers

    private static EvaluationDto MapToDto(Evaluation evaluation, KpiAssignment assignment)
    {
        var emp = assignment?.Employee;
        var period = assignment?.Period ?? evaluation.Period;

        var approvedAt = evaluation.UpdatedAt;
        double? remainingDisputeHours = null;
        if (evaluation.Status == EvaluationStatus.Final_Approved && approvedAt.HasValue)
        {
            var hoursPassed = (DateTime.UtcNow - approvedAt.Value).TotalHours;
            remainingDisputeHours = Math.Max(0, 72.0 - hoursPassed);
        }

        var details = evaluation.EvaluationDetails.Select(d =>
        {
            var ad = d.AssignmentDetail;
            return new EvaluationDetailDto
            {
                Id = d.Id,
                EvaluationId = d.EvaluationId,
                AssignmentDetailId = d.AssignmentDetailId,
                KpiName = ad?.KpiName ?? $"Chỉ tiêu #{d.AssignmentDetailId}",
                Unit = ad?.Unit ?? "",
                TargetValue = ad?.TargetValue ?? 100m,
                Weight = ad?.Weight ?? 0m,
                MaxCap = ad?.MaxCap ?? 150m,
                ScoringFormula = ad?.ScoringFormula ?? "HigherIsBetter",
                ActualValue = d.ActualValue,
                CompletionRate = d.CompletionRate,
                SelfScore = d.SelfScore,
                SelfRemarks = d.SelfRemarks,
                ManagerScore = d.ManagerScore,
                ScoreGap = d.ScoreGap,
                ManagerRemarks = d.ManagerRemarks,
                ScoreGapReason = d.ScoreGapReason,
                Evidences = ad?.Evidences?.Select(ev => new EvidenceItemDto
                {
                    Id = ev.Id,
                    AssignmentDetailId = ev.AssignmentDetailId,
                    FileName = ev.FileName,
                    FilePath = ev.FilePath,
                    FileType = ev.FileType,
                    FileSize = ev.FileSize,
                    Description = ev.Description,
                    UploadedAt = ev.UploadedAt
                }).ToList() ?? new List<EvidenceItemDto>()
            };
        }).ToList();

        var peerScore = evaluation.PeerReviews.Any()
            ? Math.Round(evaluation.PeerReviews.Average(p => p.BehaviorScore), 1)
            : 0.0m;

        return new EvaluationDto
        {
            Id = evaluation.Id,
            AssignmentId = evaluation.AssignmentId,
            EmployeeId = evaluation.EmployeeId,
            EmployeeName = emp?.FullName ?? "Cán bộ",
            EmployeeCode = emp?.StaffCode ?? "",
            PositionName = emp?.Position?.Name ?? "Chuyên viên",
            DepartmentName = emp?.Department?.Name ?? "Phòng ban",
            AvatarUrl = "/images/default-avatar.png",
            PeriodId = evaluation.PeriodId,
            PeriodName = period?.Name ?? "Kỳ đánh giá",
            SelfDeadline = period?.SelfDeadline ?? DateTime.UtcNow.AddDays(7),
            ManagerDeadline = period?.ManagerDeadline ?? DateTime.UtcNow.AddDays(14),
            ClosingDate = period?.ClosingDate ?? DateTime.UtcNow.AddDays(21),
            SelfTotalScore = evaluation.SelfTotalScore,
            ManagerTotalScore = evaluation.ManagerTotalScore,
            FinalScore = evaluation.FinalScore,
            FinalClassification = evaluation.FinalClassification,
            Status = evaluation.Status,
            IsDisputed = evaluation.IsDisputed,
            IsLocked = evaluation.IsLocked,
            CreatedAt = evaluation.CreatedAt,
            UpdatedAt = evaluation.UpdatedAt,
            ApprovedAt = evaluation.UpdatedAt,
            DisputeRemainingHours = remainingDisputeHours,
            Details = details,
            WorkflowHistories = evaluation.WorkflowHistories.Select(h => new WorkflowHistoryDto
            {
                Id = h.Id,
                EvaluationId = h.EvaluationId,
                ActorId = h.ActorId,
                ActorName = h.Actor?.Username ?? "Hệ thống",
                FromState = h.FromState,
                ToState = h.ToState,
                Comments = h.Comments,
                ActionTime = h.ActionTime
            }).OrderByDescending(h => h.ActionTime).ToList(),
            PeerReviews = evaluation.PeerReviews.Select(p => new PeerReviewDto
            {
                Id = p.Id,
                EvaluationId = p.EvaluationId,
                ReviewerId = p.ReviewerId,
                ReviewerName = p.IsAnonymous ? "Đồng nghiệp (Ẩn danh)" : (p.Reviewer?.FullName ?? "Đồng nghiệp"),
                BehaviorScore = p.BehaviorScore,
                FeedbackComments = p.FeedbackComments,
                IsAnonymous = p.IsAnonymous,
                SubmittedAt = p.SubmittedAt
            }).ToList(),
            AveragePeerScore = peerScore,
            Disputes = evaluation.Disputes.Select(dis => new EvaluationDisputeDto
            {
                Id = dis.Id,
                EvaluationId = dis.EvaluationId,
                AppellantId = dis.AppellantId,
                Reason = dis.Reason,
                ProposedAdjustment = dis.ProposedAdjustment,
                Status = dis.Status,
                SubmittedAt = dis.SubmittedAt,
                ResolvedAt = dis.ResolvedAt
            }).ToList()
        };
    }

    private async Task RecordAuditAsync(long userId, string action, long entityId, string details)
    {
        _context.AuditLogs.Add(new KPI.Domain.Entities.Identity.AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = "evaluations",
            EntityId = entityId,
            NewValues = details,
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1"
        });
        await _context.SaveChangesAsync();
    }

    #endregion
}
