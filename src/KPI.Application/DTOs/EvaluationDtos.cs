using KPI.Domain.Enums;

namespace KPI.Application.DTOs;

/// <summary>
/// DTO thông tin tệp minh chứng số hóa đính kèm (BR-09)
/// </summary>
public class EvidenceItemDto
{
    public long Id { get; set; }
    public long AssignmentDetailId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeFormatted => FormatFileSize(FileSize);
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{(bytes / 1024.0):F1} KB";
        return $"{(bytes / (1024.0 * 1024.0)):F2} MB";
    }
}

/// <summary>
/// DTO chi tiết từng chỉ tiêu đánh giá (Self vs Manager)
/// </summary>
public class EvaluationDetailDto
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public long AssignmentDetailId { get; set; }
    public string KpiName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal Weight { get; set; }
    public decimal MaxCap { get; set; } = 150.0m;
    public string ScoringFormula { get; set; } = "HigherIsBetter";

    // Số liệu tự đánh giá của cán bộ
    public decimal ActualValue { get; set; } = 0.0m;
    public decimal CompletionRate { get; set; } = 0.0m;
    public decimal SelfScore { get; set; } = 0.0m;
    public string? SelfRemarks { get; set; }

    // Số liệu thẩm định của Quản lý trực tiếp
    public decimal ManagerScore { get; set; } = 0.0m;
    public decimal ScoreGap { get; set; } = 0.0m;
    public string? ManagerRemarks { get; set; }
    public string? ScoreGapReason { get; set; } // BR-05: Bắt buộc >= 50 ký tự nếu lệch > 10%

    // Danh sách tài liệu minh chứng
    public List<EvidenceItemDto> Evidences { get; set; } = new();

    // Thuộc tính tính toán hỗ trợ UI
    public bool RequiresEvidence => CompletionRate >= 120.0m; // BR-09
    public bool HasEvidence => Evidences.Count > 0;
    public bool IsGapExceeded => Math.Abs(SelfScore - ManagerScore) > 10.0m; // BR-05
}

/// <summary>
/// DTO phiếu đánh giá hiệu suất tổng hợp
/// </summary>
public class EvaluationDto
{
    public long Id { get; set; }
    public long AssignmentId { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = "/images/default-avatar.png";

    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime SelfDeadline { get; set; }
    public DateTime ManagerDeadline { get; set; }
    public DateTime ClosingDate { get; set; }
    public bool IsSelfDeadlineExpired => DateTime.UtcNow > SelfDeadline;

    public decimal SelfTotalScore { get; set; }
    public decimal ManagerTotalScore { get; set; }
    public decimal FinalScore { get; set; }
    public string? FinalClassification { get; set; }

    public EvaluationStatus Status { get; set; } = EvaluationStatus.Not_Started;
    public string StatusName => GetStatusDisplayName(Status);
    public string StatusBadgeClass => GetStatusBadgeClass(Status);

    public bool IsDisputed { get; set; }
    public bool IsLocked { get; set; }

    public string? SelfRemarksOverall { get; set; }
    public string? ManagerRemarksOverall { get; set; }
    public string? DirectorRemarksOverall { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Thời gian còn lại để khiếu nại (72h từ khi Final_Approved)
    public double? DisputeRemainingHours { get; set; }
    public bool CanDispute => Status == EvaluationStatus.Final_Approved && DisputeRemainingHours > 0 && !IsDisputed;

    // Chi tiết chỉ tiêu
    public List<EvaluationDetailDto> Details { get; set; } = new();

    // Lịch sử luân chuyển
    public List<WorkflowHistoryDto> WorkflowHistories { get; set; } = new();

    // Đánh giá đồng nghiệp 360 độ
    public List<PeerReviewDto> PeerReviews { get; set; } = new();
    public decimal AveragePeerScore { get; set; } = 0.0m;

    // Khiếu nại hiện tại nếu có
    public List<EvaluationDisputeDto> Disputes { get; set; } = new();

    public static string GetStatusDisplayName(EvaluationStatus status) => status switch
    {
        EvaluationStatus.Not_Started => "Chưa bắt đầu",
        EvaluationStatus.Self_Assessing => "Đang tự đánh giá",
        EvaluationStatus.Self_Submitted => "Đã nộp tự chấm",
        EvaluationStatus.Manager_Reviewing => "Quản lý đang thẩm định",
        EvaluationStatus.Pending_Director_Approval => "Chờ Lãnh đạo phê duyệt",
        EvaluationStatus.Final_Approved => "Đã phê duyệt chính thức",
        EvaluationStatus.Locked => "Đã đóng băng dữ liệu (Khóa)",
        _ => "Không xác định"
    };

    public static string GetStatusBadgeClass(EvaluationStatus status) => status switch
    {
        EvaluationStatus.Not_Started => "badge-neutral",
        EvaluationStatus.Self_Assessing => "badge-info",
        EvaluationStatus.Self_Submitted => "badge-warning",
        EvaluationStatus.Manager_Reviewing => "badge-primary",
        EvaluationStatus.Pending_Director_Approval => "badge-warning",
        EvaluationStatus.Final_Approved => "badge-success",
        EvaluationStatus.Locked => "badge-dark",
        _ => "badge-neutral"
    };
}

/// <summary>
/// Yêu cầu lưu nháp hoặc nộp phiếu tự đánh giá (Employee)
/// </summary>
public class SubmitSelfAssessmentRequest
{
    public long EvaluationId { get; set; }
    public long AssignmentId { get; set; }
    public string? OverallRemarks { get; set; }
    public bool IsDraft { get; set; } = false; // true: Lưu nháp, false: Nộp chính thức
    public List<SelfAssessmentItemRequest> Items { get; set; } = new();
}

public class SelfAssessmentItemRequest
{
    public long AssignmentDetailId { get; set; }
    public decimal ActualValue { get; set; }
    public string? SelfRemarks { get; set; }
}

/// <summary>
/// Yêu cầu lưu nháp hoặc hoàn thành thẩm định của Quản lý (Manager)
/// </summary>
public class SubmitManagerReviewRequest
{
    public long EvaluationId { get; set; }
    public string? OverallRemarks { get; set; }
    public bool IsDraft { get; set; } = false; // true: Lưu nháp, false: Hoàn tất thẩm định
    public List<ManagerReviewItemRequest> Items { get; set; } = new();
}

public class ManagerReviewItemRequest
{
    public long DetailId { get; set; }
    public decimal ManagerScore { get; set; }
    public string? ManagerRemarks { get; set; }
    public string? ScoreGapReason { get; set; } // BR-05: >= 50 ký tự nếu lệch > 10%
}

/// <summary>
/// Yêu cầu phê duyệt kết quả cuối cùng của Lãnh đạo viện (Director)
/// </summary>
public class DirectorApproveRequest
{
    public long EvaluationId { get; set; }
    public decimal FinalScore { get; set; }
    public string FinalClassification { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

/// <summary>
/// Yêu cầu trả lại hồ sơ đánh giá
/// </summary>
public class RejectEvaluationRequest
{
    public long EvaluationId { get; set; }
    public string TargetRole { get; set; } = "Manager"; // "Staff" hoặc "Manager"
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO và Request khiếu nại kết quả đánh giá (BR-14)
/// </summary>
public class EvaluationDisputeDto
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public long AppellantId { get; set; }
    public string AppellantName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ProposedAdjustment { get; set; }
    public long? ResolverId { get; set; }
    public string? ResolverName { get; set; }
    public string? ResolutionNotes { get; set; }
    public DisputeStatus Status { get; set; }
    public string StatusName => Status switch
    {
        DisputeStatus.Open => "Đang mở khiếu nại",
        DisputeStatus.UnderReview => "Đang thụ lý xác minh",
        DisputeStatus.Resolved => "Đã giải quyết (Chấp thuận)",
        DisputeStatus.Rejected => "Bác đơn khiếu nại",
        _ => "Không xác định"
    };
    public DateTime SubmittedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class SubmitDisputeRequest
{
    public long EvaluationId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ProposedAdjustment { get; set; }
}

public class ResolveDisputeRequest
{
    public long DisputeId { get; set; }
    public bool IsAccepted { get; set; }
    public decimal? AdjustedScore { get; set; }
    public string? AdjustedClassification { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
}

/// <summary>
/// DTO Đánh giá 360 độ (Peer Review)
/// </summary>
public class PeerReviewDto
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public long ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public decimal BehaviorScore { get; set; }
    public string? FeedbackComments { get; set; }
    public bool IsAnonymous { get; set; } = true;
    public DateTime SubmittedAt { get; set; }
}

public class SubmitPeerReviewRequest
{
    public long EvaluationId { get; set; }
    public decimal BehaviorScore { get; set; } // Thang 1 - 10
    public string? FeedbackComments { get; set; }
    public bool IsAnonymous { get; set; } = true;
}

/// <summary>
/// DTO Nhật ký luân chuyển hồ sơ (Workflow History)
/// </summary>
public class WorkflowHistoryDto
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public long ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime ActionTime { get; set; }
}
