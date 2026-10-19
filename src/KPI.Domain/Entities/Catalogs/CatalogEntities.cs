using KPI.Domain.Common;
using KPI.Domain.Enums;

namespace KPI.Domain.Entities.Catalogs;

/// <summary>
/// 16. Nhóm chỉ số KPI (kpi_categories - 4 viễn cảnh BSC & đạo đức công vụ)
/// </summary>
public class KpiCategory : BaseEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public KpiPerspective Perspective { get; set; } = KpiPerspective.InternalProcess;
    public TargetSector TargetSector { get; set; } = TargetSector.All;
    public int DisplayOrder { get; set; } = 0;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<KpiMasterItem> MasterItems { get; set; } = new List<KpiMasterItem>();
}

/// <summary>
/// 17. Đơn vị đo lường (measurement_units)
/// </summary>
public class MeasurementUnit : BaseEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<KpiMasterItem> MasterItems { get; set; } = new List<KpiMasterItem>();
}

/// <summary>
/// 18. Phương pháp đo lường (measurement_methods)
/// </summary>
public class MeasurementMethod : BaseEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DataSource { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<KpiMasterItem> MasterItems { get; set; } = new List<KpiMasterItem>();
}

/// <summary>
/// 19. Ngân hàng chỉ số KPI chuẩn (kpi_master_items)
/// </summary>
public class KpiMasterItem : BaseEntity, ISoftDeletable
{
    public long CategoryId { get; set; }
    public virtual KpiCategory Category { get; set; } = null!;

    public long UnitId { get; set; }
    public virtual MeasurementUnit Unit { get; set; } = null!;

    public long MethodId { get; set; }
    public virtual MeasurementMethod Method { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScoringFormula ScoringFormula { get; set; } = ScoringFormula.HigherIsBetter;
    public bool IsCivilServantOnly { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}

/// <summary>
/// 20. Thang điểm đánh giá (score_scales)
/// </summary>
public class ScoreScale : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MinScore { get; set; } = 0.0m;
    public decimal MaxScore { get; set; } = 100.0m;
    public bool IsDefault { get; set; } = false;

    // Navigation properties
    public virtual ICollection<ClassificationLevel> ClassificationLevels { get; set; } = new List<ClassificationLevel>();
    public virtual ICollection<EvaluationPeriod> EvaluationPeriods { get; set; } = new List<EvaluationPeriod>();
}

/// <summary>
/// 21. Mức xếp loại chất lượng (classification_levels - Nghị định 90/2020)
/// </summary>
public class ClassificationLevel : BaseEntity
{
    public long ScaleId { get; set; }
    public virtual ScoreScale Scale { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MinPercentage { get; set; }
    public decimal MaxPercentage { get; set; }
    public bool RequiresInitiative { get; set; } = false;
    public bool IsCappedAt20Percent { get; set; } = false;
}

/// <summary>
/// 22. Kỳ đánh giá hiệu suất (evaluation_periods)
/// </summary>
public class EvaluationPeriod : BaseEntity, ISoftDeletable
{
    public long ScaleId { get; set; }
    public virtual ScoreScale Scale { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PeriodType PeriodType { get; set; } = PeriodType.Annual;
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SelfDeadline { get; set; }
    public DateTime ManagerDeadline { get; set; }
    public DateTime ClosingDate { get; set; }
    public PeriodStatus Status { get; set; } = PeriodStatus.Planning;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
