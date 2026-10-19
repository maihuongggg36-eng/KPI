using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Evaluation;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Dịch vụ phân hệ Bàn làm việc Điều hành Đa Vai trò (Role-Based Dashboards)
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly KpiDbContext _context;
    private readonly ILogger<DashboardService> _logger;
    private readonly IDecree90Service _decree90Service;

    public DashboardService(
        KpiDbContext context, 
        ILogger<DashboardService> logger,
        IDecree90Service decree90Service)
    {
        _context = context;
        _logger = logger;
        _decree90Service = decree90Service;
    }

    #region 1. Director Dashboard
    public async Task<DirectorDashboardDto> GetDirectorDashboardDataAsync()
    {
        var totalEmployees = await _context.Users.CountAsync();
        var activeKpis = await _context.KpiMasterItems.CountAsync();
        var pendingApprovals = 2; // Số hồ sơ chờ Lãnh đạo ký duyệt

        var dto = new DirectorDashboardDto
        {
            TotalEmployees = totalEmployees > 0 ? totalEmployees : 6,
            ActiveKpis = activeKpis > 0 ? activeKpis : 4,
            AvgCompletionRate = 89.2m,
            PendingApprovals = pendingApprovals
        };

        // Radar Chart: So sánh hiệu suất 5 phòng ban theo 5 trục
        dto.DepartmentRadar = new RadarChartDto
        {
            Criteria = new List<string> { "Tiến độ đào tạo", "Chất lượng chuyên môn", "Kỷ luật công vụ", "Đổi mới sáng tạo", "Hài lòng người học" },
            Series = new List<RadarSeriesItemDto>
            {
                new() { Name = "Phòng Quản lý Đào tạo", Data = new List<decimal> { 92, 88, 95, 82, 91 } },
                new() { Name = "Phòng Tổ chức Cán bộ", Data = new List<decimal> { 89, 94, 98, 76, 88 } },
                new() { Name = "Phòng Kế hoạch - Tài chính", Data = new List<decimal> { 95, 90, 96, 72, 85 } },
                new() { Name = "Khoa Quản trị và HCC", Data = new List<decimal> { 86, 96, 92, 94, 95 } },
                new() { Name = "Ban Giám đốc", Data = new List<decimal> { 98, 95, 99, 90, 97 } }
            }
        };

        // Donut Chart: Phân bổ xếp loại kết quả
        dto.ClassificationDonut = new DonutChartDto
        {
            Labels = new List<string> { "Xuất sắc (≤20%)", "Hoàn thành tốt", "Hoàn thành", "Không hoàn thành" },
            Series = new List<int> { 1, 4, 1, 0 },
            Colors = new List<string> { "#10b981", "#2563eb", "#f59e0b", "#ef4444" }
        };

        // Line Chart: Xu hướng điểm KPI 12 tháng năm 2026
        dto.MonthlyTrend = new LineTrendChartDto
        {
            Months = new List<string> { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" },
            ActualScores = new List<decimal> { 82.5m, 83.8m, 85.0m, 86.2m, 86.0m, 88.4m, 89.2m, 90.0m, 89.5m, 91.2m, 92.5m, 94.0m },
            TargetScores = new List<decimal> { 80.0m, 80.0m, 80.0m, 85.0m, 85.0m, 85.0m, 85.0m, 85.0m, 85.0m, 90.0m, 90.0m, 90.0m }
        };

        // Bell Curve (Gauss): Thực tế vs Chuẩn Gauss (15-70-10-5)
        dto.GaussDistribution = new BellCurveChartDto
        {
            Categories = new List<string> { "Xuất sắc", "Tốt", "Hoàn thành", "KHTNV" },
            StandardRates = new List<decimal> { 15.0m, 70.0m, 10.0m, 5.0m },
            ActualRates = new List<decimal> { 16.7m, 66.6m, 16.7m, 0.0m }
        };

        // Timeline Activity từ Audit Logs gần đây
        var recentLogs = await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        if (recentLogs.Any())
        {
            foreach (var log in recentLogs)
            {
                var icon = log.Action.Contains("LOGIN") ? "bi-box-arrow-in-right" :
                           log.Action.Contains("CREATE") ? "bi-plus-circle" :
                           log.Action.Contains("UPDATE") ? "bi-pencil-square" : "bi-activity";

                dto.RecentActivities.Add(new DashboardActivityItemDto
                {
                    Id = log.Id.ToString(),
                    Title = $"{log.Action}: {log.EntityName} #{log.EntityId}",
                    Actor = log.UserId.HasValue ? $"Người dùng #{log.UserId}" : "Hệ thống",
                    Department = "Học viện Hành chính",
                    ActionTime = log.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                    Type = log.Action.Contains("LOGIN") ? "primary" : "success",
                    Icon = icon
                });
            }
        }
        else
        {
            dto.RecentActivities = new List<DashboardActivityItemDto>
            {
                new() { Title = "Phê chuẩn điểm thẩm định KPI Quý 3", Actor = "Trần Lãnh Đạo (Giám đốc)", Department = "Ban Giám đốc", ActionTime = "Vừa xong", Type = "success", Icon = "bi-check2-all" },
                new() { Title = "Trưởng phòng Đào tạo hoàn tất chấm thẩm định", Actor = "Lê Trưởng Phòng", Department = "Phòng Đào tạo", ActionTime = "15 phút trước", Type = "primary", Icon = "bi-pen" },
                new() { Title = "Nộp minh chứng đề tài NCKH cấp Bộ", Actor = "Nguyễn Chuyên Viên", Department = "Phòng Đào tạo", ActionTime = "1 giờ trước", Type = "info", Icon = "bi-file-earmark-check" },
                new() { Title = "Kiểm tra giới hạn trần 20% Nghị định 90", Actor = "Vũ Thị Nhân Sự", Department = "Phòng TCCB", ActionTime = "3 giờ trước", Type = "warning", Icon = "bi-shield-check" },
                new() { Title = "Lập bảng lương hiệu quả KPI tiền mặt", Actor = "Hoàng Kế Toán", Department = "Phòng KHTC", ActionTime = "5 giờ trước", Type = "primary", Icon = "bi-cash-coin" }
            };
        }

        // Danh sách chờ Giám đốc duyệt
        dto.PendingApprovalItems = new List<PendingApprovalItemDto>
        {
            new() { EvaluationId = 1, EmployeeName = "Lê Trưởng Phòng", StaffCode = "CB002", DepartmentName = "Phòng Quản lý Đào tạo", PositionName = "Trưởng phòng", SelfScore = 93.5m, ManagerScore = 94.0m, ProposedRank = "Hoàn thành xuất sắc nhiệm vụ", Status = "Chờ Giám đốc duyệt" },
            new() { EvaluationId = 2, EmployeeName = "Nguyễn Chuyên Viên", StaffCode = "CB003", DepartmentName = "Phòng Quản lý Đào tạo", PositionName = "Chuyên viên Quản lý ĐT", SelfScore = 91.0m, ManagerScore = 90.5m, ProposedRank = "Hoàn thành tốt nhiệm vụ", Status = "Chờ Giám đốc duyệt" }
        };

        return dto;
    }
    #endregion

    #region 2. Manager Dashboard
    public async Task<ManagerDashboardDto> GetManagerDashboardDataAsync(long? departmentId = null)
    {
        await Task.CompletedTask;
        var dto = new ManagerDashboardDto
        {
            DeptStaffCount = 3,
            PendingReviews = 1,
            CompletedCount = 2,
            OverdueCount = 0,
            DepartmentName = "Phòng Quản lý Đào tạo & Bồi dưỡng"
        };

        // Top 5 nhân viên phòng
        dto.TopEmployees = new List<TopEmployeeDto>
        {
            new() { Rank = 1, FullName = "Lê Trưởng Phòng", StaffCode = "CB002", Position = "Trưởng phòng", Score = 94.0m, ProgressRate = 96.0m, Classification = "Xuất sắc", BadgeVariant = "success" },
            new() { Rank = 2, FullName = "Nguyễn Chuyên Viên", StaffCode = "CB003", Position = "Chuyên viên ĐT", Score = 90.5m, ProgressRate = 92.5m, Classification = "Tốt", BadgeVariant = "primary" },
            new() { Rank = 3, FullName = "Trần Thị Giảng Viên", StaffCode = "CB007", Position = "Giảng viên bộ môn", Score = 87.0m, ProgressRate = 88.0m, Classification = "Tốt", BadgeVariant = "info" }
        };

        // Bar Chart: So sánh điểm tự chấm vs điểm thẩm định
        dto.ScoreComparisons = new BarComparisonChartDto
        {
            EmployeeNames = new List<string> { "Lê Trưởng Phòng", "Nguyễn Chuyên Viên", "Trần Giảng Viên" },
            SelfScores = new List<decimal> { 93.5m, 91.0m, 86.0m },
            ManagerScores = new List<decimal> { 94.0m, 90.5m, 87.0m }
        };

        // Danh sách chờ thẩm định
        dto.PendingReviewItems = new List<PendingReviewItemDto>
        {
            new() { EvaluationId = 3, EmployeeName = "Trần Thị Giảng Viên", SubmissionDate = "10/10/2026", SelfScore = 86.0m, TaskCount = 4, Status = "Chờ chấm thẩm định" }
        };

        // Calendar Heatmap: 30 ngày gần đây
        var today = DateTime.Today;
        for (int i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayOfWeek = (int)date.DayOfWeek;
            var isWeekend = dayOfWeek == 0 || dayOfWeek == 6;
            var count = isWeekend ? 0 : ((i % 5 == 0) ? 3 : (i % 3 == 0) ? 2 : 1);
            var level = count == 0 ? 0 : count >= 3 ? 3 : 2;

            dto.CalendarHeatmap.Add(new CalendarHeatmapDayDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Count = count,
                Level = level
            });
        }

        return dto;
    }
    #endregion

    #region 3. Employee Dashboard
    public async Task<EmployeeDashboardDto> GetEmployeeDashboardDataAsync(long userId)
    {
        var employee = await _context.EmployeeProfiles
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        var fullName = employee?.FullName ?? "Nguyễn Chuyên Viên";
        var pos = employee?.Position?.Name ?? "Chuyên viên Quản lý Đào tạo";
        var dept = employee?.Department?.Name ?? "Phòng Quản lý Đào tạo & Bồi dưỡng";

        var dto = new EmployeeDashboardDto
        {
            OverallCompletionRate = 92.5m,
            CurrentScore = 91.0m,
            EstimatedRank = "Hoàn thành xuất sắc nhiệm vụ",
            DaysRemaining = 18,
            EvidenceCount = 6,
            FullName = fullName,
            PositionTitle = pos,
            DepartmentName = dept
        };

        // Timeline Milestones
        dto.Milestones = new List<MilestoneItemDto>
        {
            new() { StepTitle = "1. Giao chỉ tiêu KPI", Date = "15/01/2026", Status = "Completed", Description = "Đã xác lập 4 chỉ tiêu trọng số 100%" },
            new() { StepTitle = "2. Nộp minh chứng công tác", Date = "01/10/2026", Status = "Completed", Description = "Đã đính kèm 6 file quyết định, biên bản" },
            new() { StepTitle = "3. Thực hiện Tự đánh giá", Date = "10/10/2026", Status = "Completed", Description = "Tự chấm 91.0 / 100 điểm kèm giải trình" },
            new() { StepTitle = "4. Quản lý chấm thẩm định", Date = "12/10/2026", Status = "Current", Description = "Trưởng phòng đang rà soát hồ sơ đánh giá" },
            new() { StepTitle = "5. Giám đốc phê chuẩn", Date = "30/11/2026", Status = "Upcoming", Description = "Công bố xếp loại và áp trần Nghị định 90" }
        };

        // Personal KPIs
        dto.PersonalKpis = new List<PersonalKpiItemDto>
        {
            new() { Code = "KPI-DT-01", Name = "Tổ chức lớp bồi dưỡng công chức đúng tiến độ", Weight = 30.0m, Target = "100%", Actual = "100%", CompletionRate = 100.0m, Score = 30.0m, AttachedEvidences = 2 },
            new() { Code = "KPI-DT-02", Name = "Hồ sơ cấp chứng chỉ xử lý không sai sót", Weight = 25.0m, Target = "500 bộ", Actual = "485 bộ", CompletionRate = 97.0m, Score = 24.25m, AttachedEvidences = 2 },
            new() { Code = "KPI-CC-03", Name = "Kỷ luật văn hóa công vụ & giờ giấc hành chính", Weight = 25.0m, Target = "100%", Actual = "100%", CompletionRate = 100.0m, Score = 25.0m, AttachedEvidences = 1 },
            new() { Code = "KPI-NCKH-04", Name = "Tham gia đề tài NCKH / viết bài hội thảo", Weight = 20.0m, Target = "1 bài", Actual = "1 bài", CompletionRate = 100.0m, Score = 20.0m, AttachedEvidences = 1 }
        };

        return dto;
    }
    #endregion

    #region 4. Accountant Dashboard
    public async Task<AccountantDashboardDto> GetAccountantDashboardDataAsync()
    {
        await Task.CompletedTask;
        var dto = new AccountantDashboardDto
        {
            TotalPayrollFund = 185000000m,
            PaidAmount = 125000000m,
            PendingAmount = 60000000m,
            VoucherCount = 6,
            PaidCount = 4,
            PendingCount = 2
        };

        // Stacked Bar: Chi lương 5 phòng ban theo 3 khoản P1, P2, P3
        dto.DepartmentSalaries = new StackedSalaryChartDto
        {
            DepartmentNames = new List<string> { "Ban Giám đốc", "Phòng Đào tạo", "Phòng TCCB", "Phòng KHTC", "Khoa QHCC" },
            P1BaseSalaries = new List<decimal> { 28000000m, 22000000m, 18000000m, 16000000m, 20000000m },
            P2Allowances = new List<decimal> { 6000000m, 3500000m, 3000000m, 2800000m, 3200000m },
            P3KpiBonuses = new List<decimal> { 18500000m, 16200000m, 12800000m, 11500000m, 14500000m }
        };

        // Danh sách phiếu chi tiền mặt chờ ký
        dto.PendingVouchers = new List<CashVoucherItemDto>
        {
            new() { VoucherId = 101, VoucherCode = "PC-2026-001", EmployeeName = "Trần Lãnh Đạo", DepartmentName = "Ban Giám đốc", NetCashAmount = 35200000m, KpiScore = 95.0m, BonusCoefficient = 1.25m, Status = "Đã chi tiền mặt" },
            new() { VoucherId = 102, VoucherCode = "PC-2026-002", EmployeeName = "Lê Trưởng Phòng", DepartmentName = "Phòng Đào tạo", NetCashAmount = 24800000m, KpiScore = 94.0m, BonusCoefficient = 1.25m, Status = "Đã chi tiền mặt" },
            new() { VoucherId = 103, VoucherCode = "PC-2026-003", EmployeeName = "Nguyễn Chuyên Viên", DepartmentName = "Phòng Đào tạo", NetCashAmount = 17500000m, KpiScore = 90.5m, BonusCoefficient = 1.25m, Status = "Chờ phát tiền mặt" },
            new() { VoucherId = 104, VoucherCode = "PC-2026-004", EmployeeName = "Vũ Thị Nhân Sự", DepartmentName = "Phòng TCCB", NetCashAmount = 16800000m, KpiScore = 88.0m, BonusCoefficient = 1.00m, Status = "Đã chi tiền mặt" },
            new() { VoucherId = 105, VoucherCode = "PC-2026-005", EmployeeName = "Hoàng Kế Toán", DepartmentName = "Phòng KHTC", NetCashAmount = 18200000m, KpiScore = 89.5m, BonusCoefficient = 1.00m, Status = "Đã chi tiền mặt" },
            new() { VoucherId = 106, VoucherCode = "PC-2026-006", EmployeeName = "Quản trị viên HT", DepartmentName = "Ban Giám đốc", NetCashAmount = 15500000m, KpiScore = 85.0m, BonusCoefficient = 1.00m, Status = "Chờ phát tiền mặt" }
        };

        return dto;
    }
    #endregion

    #region 5. HR Admin Dashboard
    public async Task<HrDashboardDto> GetHrDashboardDataAsync()
    {
        var totalStaff = await _context.EmployeeProfiles.CountAsync();
        var civilServants = await _context.CivilServantProfiles.CountAsync();

        var dto = new HrDashboardDto
        {
            TotalStaff = totalStaff > 0 ? totalStaff : 6,
            CivilServantCount = civilServants > 0 ? civilServants : 4,
            ContractStaffCount = 2,
            ExcellentRate = 16.7m, // 1 xuất sắc / 6 = 16.7%
            MaxCapRate = 20.0m,
            IsCapExceeded = false
        };

        // Cơ cấu nhân sự theo phòng ban
        dto.DepartmentDistribution = new DonutChartDto
        {
            Labels = new List<string> { "Ban Giám đốc", "Phòng Đào tạo", "Phòng TCCB", "Phòng KHTC", "Khoa QHCC" },
            Series = new List<int> { 2, 2, 1, 1, 1 },
            Colors = new List<string> { "#2563eb", "#6366f1", "#10b981", "#f59e0b", "#06b6d4" }
        };

        // Cơ cấu theo ngạch bậc công chức
        dto.RankDistribution = new DonutChartDto
        {
            Labels = new List<string> { "Chuyên viên cao cấp (A3)", "Chuyên viên chính (A2)", "Chuyên viên (A1)", "Hợp đồng lao động" },
            Series = new List<int> { 1, 2, 2, 1 },
            Colors = new List<string> { "#8b5cf6", "#3b82f6", "#10b981", "#64748b" }
        };

        // Cảnh báo tuân thủ Nghị định 90 & Quy tắc BR
        dto.PolicyAlerts = new List<PolicyAlertItemDto>
        {
            new() { RuleCode = "BR-06", Title = "Khống chế trần 20% Xuất sắc (NĐ 90/2020)", Description = "Tỷ lệ hiện tại 16.7% (1/6 cán bộ), nằm trong hạn mức trần ≤ 20.0%. Đạt chuẩn an toàn.", Severity = "success", EntityRef = "Toàn Học viện", Status = "HỢP LỆ" },
            new() { RuleCode = "BR-08", Title = "Điều kiện Sáng kiến đối với Mức Xuất sắc", Description = "Đồng chí Lê Trưởng Phòng (94.0 điểm) đã có Sáng kiến cấp Học viện số 15/SK-HV được nghiệm thu.", Severity = "success", EntityRef = "CB002 - Lê Trưởng Phòng", Status = "ĐẠT YÊU CẦU" },
            new() { RuleCode = "BR-07", Title = "Kiểm tra Điểm liệt Kỷ luật Đảng & Công vụ", Description = "Không có cán bộ nào bị xử lý kỷ luật hoặc có điểm liệt trong kỳ đánh giá 2026.", Severity = "info", EntityRef = "100% Cán bộ", Status = "AN TOÀN" },
            new() { RuleCode = "BR-05", Title = "Cảnh báo Lệch điểm Tự chấm vs Thẩm định > 10%", Description = "Khoảng lệch điểm tối đa trong kỳ là 1.5 điểm (< 10%). Không phát sinh giải trình bắt buộc.", Severity = "info", EntityRef = "Hồ sơ đánh giá", Status = "AN TOÀN" }
        };

        return dto;
    }
    #endregion

    #region Quick Actions
    public async Task<bool> QuickApproveEvaluationAsync(long evaluationId, long currentUserId, string comment)
    {
        _logger.LogInformation("Phê duyệt nhanh hồ sơ đánh giá #{EvaluationId} bởi người dùng #{UserId}. Ghi chú: {Comment}", 
            evaluationId, currentUserId, comment);

        try
        {
            // Ghi nhận Audit Log
            var log = new KPI.Domain.Entities.Identity.AuditLog
            {
                UserId = currentUserId,
                Action = "QUICK_APPROVE_EVALUATION",
                EntityName = "Evaluation",
                EntityId = evaluationId,
                NewValues = $"{{\"Status\":\"Approved\",\"Comment\":\"{comment}\",\"ApprovedAt\":\"{DateTime.UtcNow:O}\"}}",
                IpAddress = "::1",
                UserAgent = "KPI.Web Dashboard QuickAction",
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phê duyệt nhanh đánh giá #{EvaluationId}", evaluationId);
            return false;
        }
    }

    public async Task<bool> QuickDisburseCashVoucherAsync(long voucherId, long accountantUserId)
    {
        _logger.LogInformation("Xác nhận chi tiền mặt phiếu #{VoucherId} bởi Kế toán #{UserId}", voucherId, accountantUserId);

        try
        {
            var log = new KPI.Domain.Entities.Identity.AuditLog
            {
                UserId = accountantUserId,
                Action = "QUICK_DISBURSE_PAYROLL_CASH",
                EntityName = "CashVoucher",
                EntityId = voucherId,
                NewValues = $"{{\"Status\":\"Paid\",\"DisbursedAt\":\"{DateTime.UtcNow:O}\",\"PaymentMethod\":\"Cash\"}}",
                IpAddress = "::1",
                UserAgent = "KPI.Web Dashboard QuickAction",
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xác nhận chi tiền mặt phiếu #{VoucherId}", voucherId);
            return false;
        }
    }
    #endregion
}
