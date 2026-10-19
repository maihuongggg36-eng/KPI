using KPI.Application.DTOs;

namespace KPI.Web.ViewModels.CivilServant;

/// <summary>
/// ViewModel cho màn hình Bảng theo dõi Cán bộ Công chức
/// </summary>
public class CivilServantIndexViewModel
{
    public List<CivilServantOverviewDto> CivilServants { get; set; } = new();
    public CivilServantQuotaStatusDto QuotaStatus { get; set; } = new();

    public long? SelectedPeriodId { get; set; }
    public long? SelectedDepartmentId { get; set; }
    public string? SelectedRankCode { get; set; }
    public string? SelectedClassification { get; set; }

    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public List<DepartmentTreeNodeDto> Departments { get; set; } = new();
    public List<PositionRankDetailDto> PositionRanks { get; set; } = new();
}

/// <summary>
/// ViewModel cho màn hình Biểu mẫu Quốc gia Mẫu 01 / Mẫu 02
/// </summary>
public class Form01_02ViewModel
{
    public Form01_02Dto Form { get; set; } = new();
    public bool IsReadOnly { get; set; } = false;
    public string UserRole { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel cho Dashboard Công chức & Phân bổ Gauss
/// </summary>
public class CivilServantDashboardViewModel
{
    public CivilServantDashboardDto Dashboard { get; set; } = new();
    public long? SelectedPeriodId { get; set; }
    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
}
