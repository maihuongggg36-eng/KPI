using KPI.Domain.Common;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Enums;

namespace KPI.Domain.Entities.Evaluation;

/// <summary>
/// 28. Phiếu đánh giá hiệu suất tổng hợp (evaluations)
/// </summary>
public class Evaluation : BaseEntity, ISoftDeletable
{
    public long AssignmentId { get; set; }
    public virtual KpiAssignment Assignment { get; set; } = null!;

    public long EmployeeId { get; set; }
    public virtual EmployeeProfile Employee { get; set; } = null!;

    public long PeriodId { get; set; }
    public virtual EvaluationPeriod Period { get; set; } = null!;

    public decimal SelfTotalScore { get; set; } = 0.0m;
    public decimal ManagerTotalScore { get; set; } = 0.0m;
    public decimal FinalScore { get; set; } = 0.0m;
    public string? FinalClassification { get; set; }

    public EvaluationStatus Status { get; set; } = EvaluationStatus.Not_Started;
    public bool IsDisputed { get; set; } = false;
    public bool IsLocked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<EvaluationDetail> EvaluationDetails { get; set; } = new List<EvaluationDetail>();
    public virtual CivilServantClassification? CivilServantClassification { get; set; }
    public virtual ICollection<PeerReview> PeerReviews { get; set; } = new List<PeerReview>();
    public virtual EvaluationWorkflow? Workflow { get; set; }
    public virtual ICollection<WorkflowHistory> WorkflowHistories { get; set; } = new List<WorkflowHistory>();
    public virtual ICollection<EvaluationDispute> Disputes { get; set; } = new List<EvaluationDispute>();
}

/// <summary>
/// 29. Bảng chi tiết điểm số từng chỉ tiêu (evaluation_details)
/// </summary>
public class EvaluationDetail : BaseEntity
{
    public long EvaluationId { get; set; }
    public virtual Evaluation Evaluation { get; set; } = null!;

    public long AssignmentDetailId { get; set; }
    public virtual KpiAssignmentDetail AssignmentDetail { get; set; } = null!;

    public decimal ActualValue { get; set; } = 0.0m;
    public decimal CompletionRate { get; set; } = 0.0m;
    public decimal SelfScore { get; set; } = 0.0m;
    public decimal ManagerScore { get; set; } = 0.0m;
    public decimal ScoreGap { get; set; } = 0.0m;
    public string? SelfRemarks { get; set; }
    public string? ManagerRemarks { get; set; }
    public string? ScoreGapReason { get; set; } // BR-05: Bắt buộc >= 50 ký tự nếu lệch > 10%
}

/// <summary>
/// 30. Phân loại cán bộ công chức theo Nghị định 90/2020 & Nghị định 48/2023 (civil_servant_classifications)
/// </summary>
public class CivilServantClassification : BaseEntity
{
    public long EvaluationId { get; set; }
    public virtual Evaluation Evaluation { get; set; } = null!;

    public long CivilServantId { get; set; }
    public virtual CivilServantProfile CivilServant { get; set; } = null!;

    public int PoliticalEthicsScore { get; set; } = 100; // 0 -> 100
    public int WorkDisciplineScore { get; set; } = 100;
    public int DutyExecutionScore { get; set; } = 100;

    public bool HasDisciplinaryAction { get; set; } = false; // BR-07: Điểm liệt kỷ luật
    public bool HasApprovedInitiative { get; set; } = false; // BR-08: Điều kiện bắt buộc cho Xuất sắc

    public string? CollectiveComments { get; set; } // Ý kiến tập thể phòng ban
    public string? PartyOpinion { get; set; } // Ý kiến cấp ủy nơi công tác
    public string ProposedRank { get; set; } = string.Empty; // Đề xuất mức xếp loại
    public string FinalizedRank { get; set; } = string.Empty; // Kết luận của Lãnh đạo

    public long? ApprovedBy { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 31. Đánh giá ngang hàng 360 độ (peer_reviews)
/// </summary>
public class PeerReview : BaseEntity
{
    public long EvaluationId { get; set; }
    public virtual Evaluation Evaluation { get; set; } = null!;

    public long ReviewerId { get; set; }
    public virtual EmployeeProfile Reviewer { get; set; } = null!;

    public decimal BehaviorScore { get; set; }
    public string? FeedbackComments { get; set; }
    public bool IsAnonymous { get; set; } = true;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 32. Quản lý tiến trình luồng duyệt (evaluation_workflows)
/// </summary>
public class EvaluationWorkflow : BaseEntity
{
    public long EvaluationId { get; set; }
    public virtual Evaluation Evaluation { get; set; } = null!;

    public string CurrentStep { get; set; } = "Self_Assessment";
    public string Status { get; set; } = "Running";

    // Navigation properties
    public virtual ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
}

/// <summary>
/// 33. Các bước phê duyệt định tuyến (workflow_steps)
/// </summary>
public class WorkflowStep : BaseEntity
{
    public long WorkflowId { get; set; }
    public virtual EvaluationWorkflow Workflow { get; set; } = null!;

    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;

    public long RequiredRoleId { get; set; }
    public virtual Role RequiredRole { get; set; } = null!;

    public long? AssignedActorId { get; set; }
    public virtual User? AssignedActor { get; set; }

    public string? ActionTaken { get; set; } = "Pending";
    public string? Comments { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 34. Nhật ký luân chuyển hồ sơ đánh giá (workflow_history)
/// </summary>
public class WorkflowHistory : BaseEntity
{
    public long EvaluationId { get; set; }
    public virtual Evaluation Evaluation { get; set; } = null!;

    public long ActorId { get; set; }
    public virtual User Actor { get; set; } = null!;

    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime ActionTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 35. Giải quyết khiếu nại kết quả đánh giá (evaluation_disputes - BR-14)
/// </summary>
public class EvaluationDispute : BaseEntity
{
    public long EvaluationId { get; set; }
    public virtual Evaluation Evaluation { get; set; } = null!;

    public long AppellantId { get; set; }
    public virtual EmployeeProfile Appellant { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public string? ProposedAdjustment { get; set; }

    public long? ResolverId { get; set; }
    public virtual User? Resolver { get; set; }

    public string? ResolutionNotes { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
