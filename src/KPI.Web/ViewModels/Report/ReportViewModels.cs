using KPI.Application.DTOs;

namespace KPI.Web.ViewModels.Report;

/// <summary>
/// ViewModel chính cho Trung tâm Báo cáo Tổng hợp & Phân tích Đa chiều
/// </summary>
public class ReportIndexViewModel
{
    public ExecutiveReportSummaryDto Summary { get; set; } = new();
    public List<LeaderboardItemDto> Leaderboard { get; set; } = new();
    public GaussDistributionDto Gauss { get; set; } = new();
    public TrendAnalysisDto Trends { get; set; } = new();
    public DataVizExtrasDto DataViz { get; set; } = new();

    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public List<DepartmentTreeNodeDto> Departments { get; set; } = new();

    public ReportFilterRequest Filter { get; set; } = new();
    public List<SavedFilterDto> SavedFilters { get; set; } = new();

    public string CurrentTab { get; set; } = "overview"; // overview, leaderboard, gauss, trends, dataviz
}

/// <summary>
/// ViewModel chuyên biệt cho Bảng Xếp Hạng Hiệu Suất (Leaderboard)
/// </summary>
public class LeaderboardViewModel
{
    public List<LeaderboardItemDto> Items { get; set; } = new();
    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public List<DepartmentTreeNodeDto> Departments { get; set; } = new();
    public ReportFilterRequest Filter { get; set; } = new();
}

/// <summary>
/// ViewModel chuyên biệt cho Chuông Gauss & Kiểm soát Tuân thủ
/// </summary>
public class GaussAnalysisViewModel
{
    public GaussDistributionDto Gauss { get; set; } = new();
    public ExecutiveReportSummaryDto Summary { get; set; } = new();
    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public List<DepartmentTreeNodeDto> Departments { get; set; } = new();
    public ReportFilterRequest Filter { get; set; } = new();
}
