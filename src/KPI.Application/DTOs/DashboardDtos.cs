namespace KPI.Application.DTOs;

#region 1. Director Dashboard DTOs
public class DirectorDashboardDto
{
    // 4 Stat Cards
    public int TotalEmployees { get; set; } = 6;
    public int ActiveKpis { get; set; } = 4;
    public decimal AvgCompletionRate { get; set; } = 88.5m;
    public int PendingApprovals { get; set; } = 2;

    // Biểu đồ Radar: So sánh hiệu suất 5 phòng ban theo 5 chiều tiêu chí
    public RadarChartDto DepartmentRadar { get; set; } = new();

    // Biểu đồ Donut: Phân bổ xếp loại
    public DonutChartDto ClassificationDonut { get; set; } = new();

    // Biểu đồ Line: Xu hướng điểm 12 tháng gần nhất
    public LineTrendChartDto MonthlyTrend { get; set; } = new();

    // Biểu đồ Bell Curve (Gauss): Thực tế vs Chuẩn 15-70-10-5
    public BellCurveChartDto GaussDistribution { get; set; } = new();

    // Timeline Activity: Hoạt động gần đây
    public List<DashboardActivityItemDto> RecentActivities { get; set; } = new();

    // Danh sách chờ phê duyệt & Phê duyệt nhanh
    public List<PendingApprovalItemDto> PendingApprovalItems { get; set; } = new();
}

public class RadarChartDto
{
    public List<string> Criteria { get; set; } = new();
    public List<RadarSeriesItemDto> Series { get; set; } = new();
}

public class RadarSeriesItemDto
{
    public string Name { get; set; } = string.Empty;
    public List<decimal> Data { get; set; } = new();
}

public class DonutChartDto
{
    public List<string> Labels { get; set; } = new();
    public List<int> Series { get; set; } = new();
    public List<string> Colors { get; set; } = new();
}

public class LineTrendChartDto
{
    public List<string> Months { get; set; } = new();
    public List<decimal> ActualScores { get; set; } = new();
    public List<decimal> TargetScores { get; set; } = new();
}

public class BellCurveChartDto
{
    public List<string> Categories { get; set; } = new();
    public List<decimal> StandardRates { get; set; } = new(); // 15%, 70%, 10%, 5%
    public List<decimal> ActualRates { get; set; } = new();
}

public class DashboardActivityItemDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ActionTime { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // success, warning, primary, info
    public string Icon { get; set; } = "bi-check-circle";
}

public class PendingApprovalItemDto
{
    public long EvaluationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public decimal SelfScore { get; set; }
    public decimal ManagerScore { get; set; }
    public string ProposedRank { get; set; } = string.Empty;
    public string Status { get; set; } = "Chờ Giám đốc duyệt";
}
#endregion

#region 2. Manager Dashboard DTOs
public class ManagerDashboardDto
{
    public int DeptStaffCount { get; set; } = 3;
    public int PendingReviews { get; set; } = 1;
    public int CompletedCount { get; set; } = 2;
    public int OverdueCount { get; set; } = 0;
    public string DepartmentName { get; set; } = "Phòng Quản lý Đào tạo & Bồi dưỡng";

    // Bảng xếp hạng Top 5 nhân viên phòng
    public List<TopEmployeeDto> TopEmployees { get; set; } = new();

    // Biểu đồ Bar: So sánh điểm KPI nhân viên trong phòng
    public BarComparisonChartDto ScoreComparisons { get; set; } = new();

    // Danh sách chờ thẩm định
    public List<PendingReviewItemDto> PendingReviewItems { get; set; } = new();

    // Calendar Heatmap: Mật độ hoàn thành
    public List<CalendarHeatmapDayDto> CalendarHeatmap { get; set; } = new();
}

public class TopEmployeeDto
{
    public int Rank { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal ProgressRate { get; set; }
    public string Classification { get; set; } = string.Empty;
    public string BadgeVariant { get; set; } = "primary";
}

public class BarComparisonChartDto
{
    public List<string> EmployeeNames { get; set; } = new();
    public List<decimal> SelfScores { get; set; } = new();
    public List<decimal> ManagerScores { get; set; } = new();
}

public class PendingReviewItemDto
{
    public long EvaluationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string SubmissionDate { get; set; } = string.Empty;
    public decimal SelfScore { get; set; }
    public int TaskCount { get; set; }
    public string Status { get; set; } = "Chờ chấm thẩm định";
}

public class CalendarHeatmapDayDto
{
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD
    public int Count { get; set; }
    public int Level { get; set; } // 0 -> 4
}
#endregion

#region 3. Employee Dashboard DTOs
public class EmployeeDashboardDto
{
    public decimal OverallCompletionRate { get; set; } = 92.5m;
    public decimal CurrentScore { get; set; } = 91.0m;
    public string EstimatedRank { get; set; } = "Hoàn thành xuất sắc nhiệm vụ";
    public int DaysRemaining { get; set; } = 18;
    public int EvidenceCount { get; set; } = 6;
    public string FullName { get; set; } = "Nguyễn Chuyên Viên";
    public string PositionTitle { get; set; } = "Chuyên viên Quản lý Đào tạo";
    public string DepartmentName { get; set; } = "Phòng Quản lý Đào tạo & Bồi dưỡng";

    // Timeline Milestone cá nhân
    public List<MilestoneItemDto> Milestones { get; set; } = new();

    // Danh sách KPI chi tiết cá nhân
    public List<PersonalKpiItemDto> PersonalKpis { get; set; } = new();
}

public class MilestoneItemDto
{
    public string StepTitle { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = "Upcoming"; // Completed, Current, Upcoming
    public string Description { get; set; } = string.Empty;
}

public class PersonalKpiItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string Target { get; set; } = string.Empty;
    public string Actual { get; set; } = string.Empty;
    public decimal CompletionRate { get; set; }
    public decimal Score { get; set; }
    public int AttachedEvidences { get; set; }
}
#endregion

#region 4. Accountant Dashboard DTOs
public class AccountantDashboardDto
{
    // 4 Stat Cards
    public decimal TotalPayrollFund { get; set; } = 185000000m;
    public decimal PaidAmount { get; set; } = 125000000m;
    public decimal PendingAmount { get; set; } = 60000000m;
    public int VoucherCount { get; set; } = 6;
    public int PaidCount { get; set; } = 4;
    public int PendingCount { get; set; } = 2;

    // Biểu đồ Stacked Bar: Chi lương theo 5 phòng ban (P1, P2, P3)
    public StackedSalaryChartDto DepartmentSalaries { get; set; } = new();

    // Danh sách phiếu chi chờ phát tiền mặt
    public List<CashVoucherItemDto> PendingVouchers { get; set; } = new();
}

public class StackedSalaryChartDto
{
    public List<string> DepartmentNames { get; set; } = new();
    public List<decimal> P1BaseSalaries { get; set; } = new(); // Lương ngạch bậc
    public List<decimal> P2Allowances { get; set; } = new();   // Phụ cấp công vụ
    public List<decimal> P3KpiBonuses { get; set; } = new();   // Lương hiệu quả KPI
}

public class CashVoucherItemDto
{
    public long VoucherId { get; set; }
    public string VoucherCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal NetCashAmount { get; set; }
    public decimal KpiScore { get; set; }
    public decimal BonusCoefficient { get; set; }
    public string Status { get; set; } = "Chờ ký phát tiền mặt";
}
#endregion

#region 5. HR Admin Dashboard DTOs
public class HrDashboardDto
{
    // Gauge Meter: Trần 20% Nghị định 90/2020 (BR-06)
    public decimal ExcellentRate { get; set; } = 16.7m; // Hiện tại 1/6 = 16.7%
    public decimal MaxCapRate { get; set; } = 20.0m;
    public bool IsCapExceeded { get; set; } = false;

    // Thống kê nhân sự
    public int TotalStaff { get; set; } = 6;
    public int CivilServantCount { get; set; } = 4;
    public int ContractStaffCount { get; set; } = 2;

    // Cơ cấu nhân sự theo phòng ban
    public DonutChartDto DepartmentDistribution { get; set; } = new();

    // Cơ cấu nhân sự theo ngạch bậc công chức
    public DonutChartDto RankDistribution { get; set; } = new();

    // Cảnh báo & Tuân thủ Nghị định (Alerts)
    public List<PolicyAlertItemDto> PolicyAlerts { get; set; } = new();
}

public class PolicyAlertItemDto
{
    public string RuleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "info"; // danger, warning, success, info
    public string EntityRef { get; set; } = string.Empty;
    public string Status { get; set; } = "Tuân thủ";
}
#endregion
