using KPI.Application.DTOs;

namespace KPI.Web.ViewModels.Catalog;

public class CatalogIndexViewModel
{
    public string ActiveTab { get; set; } = "kpi-items";
    public string SearchQuery { get; set; } = string.Empty;

    public List<KpiMasterItemDetailDto> KpiItems { get; set; } = new();
    public List<DepartmentTreeNodeDto> Departments { get; set; } = new();
    public List<PositionDetailDto> Positions { get; set; } = new();
    public List<PositionRankDetailDto> PositionRanks { get; set; } = new();
    public List<KpiCategoryDetailDto> Categories { get; set; } = new();
    public List<MeasurementUnitDetailDto> Units { get; set; } = new();
    public List<MeasurementMethodDetailDto> Methods { get; set; } = new();
    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public List<ScoreScaleDetailDto> Scales { get; set; } = new();
    public List<KpiTemplateDetailDto> Templates { get; set; } = new();
    public List<SystemConfigDetailDto> Configs { get; set; } = new();
    public List<NotificationTemplateDetailDto> Notifications { get; set; } = new();
    public List<CatalogActivityLogDto> ActivityLogs { get; set; } = new();
}

public class KpiBankViewModel
{
    public List<KpiMasterItemDetailDto> Items { get; set; } = new();
    public List<KpiCategoryDetailDto> Categories { get; set; } = new();
    public List<MeasurementUnitDetailDto> Units { get; set; } = new();
    public List<MeasurementMethodDetailDto> Methods { get; set; } = new();
    public string ActivePerspective { get; set; } = "All";
    public string ActiveSector { get; set; } = "All";
    public string SearchQuery { get; set; } = string.Empty;
    public string ViewMode { get; set; } = "grid"; // grid | table
}

public class DepartmentTreeViewModel
{
    public List<DepartmentTreeNodeDto> Tree { get; set; } = new();
    public List<DepartmentTreeNodeDto> FlatList { get; set; } = new();
    public int TotalDepartments { get; set; }
    public int TotalStaff { get; set; }
}
