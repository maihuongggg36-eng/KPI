using KPI.Domain.Enums;

namespace KPI.Application.DTOs;

#region 1. KpiMasterItem (Ngân hàng KPI Chuẩn)
public class KpiMasterItemDetailDto
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Perspective { get; set; } = string.Empty;
    public long UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitSymbol { get; set; } = string.Empty;
    public long MethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ScoringFormula { get; set; } = "HigherIsBetter";
    public bool IsCivilServantOnly { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; } = 0;
    public string CreatedAtStr { get; set; } = string.Empty;
}

public class CreateKpiMasterItemRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public long UnitId { get; set; }
    public long MethodId { get; set; }
    public string? Description { get; set; }
    public string ScoringFormula { get; set; } = "HigherIsBetter";
    public bool IsCivilServantOnly { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class UpdateKpiMasterItemRequest : CreateKpiMasterItemRequest
{
    public long Id { get; set; }
}

public class CloneKpiMasterItemRequest
{
    public long SourceId { get; set; }
    public string NewCode { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
}
#endregion

#region 2. Departments (Cây Phòng ban Hierarchy)
public class DepartmentTreeNodeDto
{
    public long Id { get; set; }
    public long OrgId { get; set; }
    public long? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TreePath { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Status { get; set; } = "Active";
    public int EmployeeCount { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public List<DepartmentTreeNodeDto> Children { get; set; } = new();
}

public class CreateDepartmentRequest
{
    public long OrgId { get; set; } = 1;
    public long? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}

public class UpdateDepartmentRequest : CreateDepartmentRequest
{
    public long Id { get; set; }
}

public class MoveDepartmentRequest
{
    public long DepartmentId { get; set; }
    public long? NewParentId { get; set; }
}
#endregion

#region 3. Positions & PositionRanks (Chức danh & Ngạch bậc)
public class PositionDetailDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RankLevel { get; set; } = string.Empty;
    public bool IsLeadership { get; set; }
    public int EmployeeCount { get; set; }
    public int TemplateCount { get; set; }
}

public class CreatePositionRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RankLevel { get; set; } = "Chuyên viên";
    public bool IsLeadership { get; set; } = false;
}

public class PositionRankDetailDto
{
    public string RankCode { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public string RankGroup { get; set; } = string.Empty;
    public int MaxGrade { get; set; } = 9;
    public int CivilServantCount { get; set; }
}

public class CreatePositionRankRequest
{
    public string RankCode { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public string RankGroup { get; set; } = "Công chức loại A1";
    public int MaxGrade { get; set; } = 9;
}
#endregion

#region 4. Categories, Units, Methods
public class KpiCategoryDetailDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Perspective { get; set; } = string.Empty;
    public string TargetSector { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int ItemCount { get; set; }
}

public class CreateKpiCategoryRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Perspective { get; set; } = "InternalProcess";
    public string TargetSector { get; set; } = "All";
    public int DisplayOrder { get; set; } = 1;
}

public class MeasurementUnitDetailDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class CreateMeasurementUnitRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}

public class MeasurementMethodDetailDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DataSource { get; set; }
    public int ItemCount { get; set; }
}

public class CreateMeasurementMethodRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DataSource { get; set; }
}
#endregion

#region 5. Evaluation Periods (Kỳ Đánh giá & Lifecycle)
public class EvaluationPeriodDetailDto
{
    public long Id { get; set; }
    public long ScaleId { get; set; }
    public string ScaleName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PeriodType { get; set; } = string.Empty;
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SelfDeadline { get; set; }
    public DateTime ManagerDeadline { get; set; }
    public DateTime ClosingDate { get; set; }
    public string Status { get; set; } = "Open";
    public int TotalAssignments { get; set; }
    public int CompletedEvaluations { get; set; }
}

public class CreateEvaluationPeriodRequest
{
    public long ScaleId { get; set; } = 1;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PeriodType { get; set; } = "Annual";
    public int Year { get; set; } = DateTime.Now.Year;
    public DateTime StartDate { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);
    public DateTime EndDate { get; set; } = new DateTime(DateTime.Now.Year, 12, 31);
    public DateTime SelfDeadline { get; set; } = new DateTime(DateTime.Now.Year, 11, 20);
    public DateTime ManagerDeadline { get; set; } = new DateTime(DateTime.Now.Year, 11, 30);
    public DateTime ClosingDate { get; set; } = new DateTime(DateTime.Now.Year, 12, 15);
    public string Status { get; set; } = "Open";
}

public class ChangePeriodStatusRequest
{
    public long PeriodId { get; set; }
    public string NewStatus { get; set; } = "Open"; // Draft, Open, Evaluating, Finalizing, Closed, Archived
}
#endregion

#region 6. Score Scales & Classification Levels
public class ScoreScaleDetailDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MinScore { get; set; } = 0m;
    public decimal MaxScore { get; set; } = 100m;
    public bool IsDefault { get; set; }
    public List<ClassificationLevelDetailDto> Levels { get; set; } = new();
}

public class ClassificationLevelDetailDto
{
    public long Id { get; set; }
    public long ScaleId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MinPercentage { get; set; }
    public decimal MaxPercentage { get; set; }
    public bool RequiresInitiative { get; set; }
    public bool IsCappedAt20Percent { get; set; }
}
#endregion

#region 7. KPI Templates (Bộ mẫu KPI theo Chức danh)
public class KpiTemplateDetailDto
{
    public long Id { get; set; }
    public long PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public decimal TotalWeight { get; set; }
    public List<KpiTemplateItemDto> Items { get; set; } = new();
}

public class KpiTemplateItemDto
{
    public long Id { get; set; }
    public long TemplateId { get; set; }
    public long KpiItemId { get; set; }
    public string KpiCode { get; set; } = string.Empty;
    public string KpiName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TargetValue { get; set; } = 100.0m;
    public decimal? MinThreshold { get; set; } = 0.0m;
    public decimal Weight { get; set; } // BR-02: 5% - 50%
    public string? GuidanceNotes { get; set; }
}
#endregion

#region 8. System Configs & Notification Templates
public class SystemConfigDetailDto
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string DataType { get; set; } = "STRING";
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public string UpdatedAtStr { get; set; } = string.Empty;
}

public class UpdateSystemConfigItemRequest
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class NotificationTemplateDetailDto
{
    public long Id { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string Channels { get; set; } = "IN_APP,EMAIL";
    public bool IsActive { get; set; } = true;
}
#endregion

#region 9. Common Bulk & Search DTOs
public class BulkActionRequest
{
    public string CatalogType { get; set; } = string.Empty; // kpi-items, departments, positions...
    public string ActionType { get; set; } = string.Empty;  // delete, activate, deactivate, export
    public List<long> Ids { get; set; } = new();
}

public class GlobalSearchResultDto
{
    public string CatalogType { get; set; } = string.Empty;
    public string CatalogTitle { get; set; } = string.Empty;
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
}

public class CatalogActivityLogDto
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
#endregion
