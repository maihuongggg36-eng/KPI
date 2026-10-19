using KPI.Domain.Common;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Enums;

namespace KPI.Domain.Entities.Planning;

/// <summary>
/// 23. Bộ KPI mẫu theo chức danh (kpi_templates)
/// </summary>
public class KpiTemplate : BaseEntity, ISoftDeletable
{
    public long PositionId { get; set; }
    public virtual Position Position { get; set; } = null!;

    public long? DepartmentId { get; set; }
    public virtual Department? Department { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<KpiTemplateDetail> TemplateDetails { get; set; } = new List<KpiTemplateDetail>();
}

/// <summary>
/// 24. Chi tiết chỉ tiêu trong bộ mẫu (kpi_template_details)
/// </summary>
public class KpiTemplateDetail : BaseEntity
{
    public long TemplateId { get; set; }
    public virtual KpiTemplate Template { get; set; } = null!;

    public long KpiItemId { get; set; }
    public virtual KpiMasterItem KpiItem { get; set; } = null!;

    public decimal TargetValue { get; set; } = 100.0m;
    public decimal? MinThreshold { get; set; } = 0.0m;
    public decimal Weight { get; set; } // BR-02: 5% -> 50%
    public string? GuidanceNotes { get; set; }
}

/// <summary>
/// 25. Bản giao KPI cá nhân (kpi_assignments)
/// </summary>
public class KpiAssignment : BaseAuditableEntity
{
    public long PeriodId { get; set; }
    public virtual EvaluationPeriod Period { get; set; } = null!;

    public long EmployeeId { get; set; }
    public virtual EmployeeProfile Employee { get; set; } = null!;

    public long ApproverId { get; set; }
    public virtual User Approver { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public decimal TotalWeight { get; set; } = 100.0m; // BR-01: Bắt buộc bằng 100.00%
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    public DateTime? CommittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public virtual ICollection<KpiAssignmentDetail> AssignmentDetails { get; set; } = new List<KpiAssignmentDetail>();
}

/// <summary>
/// 26. Chi tiết chỉ tiêu được giao (kpi_assignment_details - Snapshot tên & đơn vị)
/// </summary>
public class KpiAssignmentDetail : BaseEntity
{
    public long AssignmentId { get; set; }
    public virtual KpiAssignment Assignment { get; set; } = null!;

    public long KpiItemId { get; set; }
    public virtual KpiMasterItem KpiItem { get; set; } = null!;

    public string KpiName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal? MinThreshold { get; set; } = 0.0m;
    public decimal MaxCap { get; set; } = 150.0m; // BR-10: Trần tối đa
    public decimal Weight { get; set; } // BR-02: [5%, 50%]
    public string ScoringFormula { get; set; } = "HigherIsBetter";

    // Navigation properties
    public virtual ICollection<KpiEvidence> Evidences { get; set; } = new List<KpiEvidence>();
}

/// <summary>
/// 27. Tài liệu minh chứng số hóa (kpi_evidences - BR-09)
/// </summary>
public class KpiEvidence : BaseEntity
{
    public long AssignmentDetailId { get; set; }
    public virtual KpiAssignmentDetail AssignmentDetail { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; } // Tối đa 25MB
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
