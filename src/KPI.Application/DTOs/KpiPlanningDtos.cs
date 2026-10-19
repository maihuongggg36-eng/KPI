using KPI.Domain.Enums;

namespace KPI.Application.DTOs;

#region 1. DTOs Chi tiết Bản giao KPI
public class KpiAssignmentDto
{
    public long Id { get; set; }
    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public long EmployeeId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public long ApproverId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal TotalWeight { get; set; }
    public AssignmentStatus Status { get; set; }
    public string StatusName => Status switch
    {
        AssignmentStatus.Draft => "Bản nháp",
        AssignmentStatus.Pending_Staff_Confirm => "Chờ nhân viên cam kết",
        AssignmentStatus.Pending_Approval => "Chờ lãnh đạo duyệt",
        AssignmentStatus.Approved_Locked => "Đã duyệt (Đã khóa)",
        AssignmentStatus.In_Evaluation => "Đang trong kỳ đánh giá",
        AssignmentStatus.Completed => "Hoàn thành",
        AssignmentStatus.Archived => "Lưu trữ",
        _ => Status.ToString()
    };
    public string StatusBadgeClass => Status switch
    {
        AssignmentStatus.Draft => "bg-secondary",
        AssignmentStatus.Pending_Staff_Confirm => "bg-warning text-dark",
        AssignmentStatus.Pending_Approval => "bg-info text-dark",
        AssignmentStatus.Approved_Locked => "bg-success",
        AssignmentStatus.In_Evaluation => "bg-primary",
        AssignmentStatus.Completed => "bg-success",
        _ => "bg-light text-dark"
    };
    public DateTime? CommittedAt { get; set; }
    public string? CommittedAtStr => CommittedAt?.ToString("dd/MM/yyyy HH:mm");
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedAtStr => ApprovedAt?.ToString("dd/MM/yyyy HH:mm");
    public DateTime CreatedAt { get; set; }
    public string CreatedAtStr => CreatedAt.ToString("dd/MM/yyyy HH:mm");
    public int VersionNumber { get; set; } = 1;
    public List<KpiAssignmentDetailDto> Details { get; set; } = new();
}

public class KpiAssignmentDetailDto
{
    public long Id { get; set; }
    public long AssignmentId { get; set; }
    public long KpiItemId { get; set; }
    public string KpiCode { get; set; } = string.Empty;
    public string KpiName { get; set; } = string.Empty;
    public string Perspective { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal? MinThreshold { get; set; }
    public decimal MaxCap { get; set; } = 150m;
    public decimal Weight { get; set; }
    public string ScoringFormula { get; set; } = "HigherIsBetter";
    public string ScoringFormulaName => ScoringFormula switch
    {
        "HigherIsBetter" => "Càng cao càng tốt",
        "LowerIsBetter" => "Càng thấp càng tốt",
        "TargetRange" => "Trong khoảng mục tiêu",
        _ => ScoringFormula
    };
    public string? Description { get; set; }
    public int EvidenceCount { get; set; } = 0;
}

public class KpiAssignmentSummaryDto
{
    public long Id { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal TotalWeight { get; set; }
    public int ItemCount { get; set; }
    public AssignmentStatus Status { get; set; }
    public string StatusName => Status switch
    {
        AssignmentStatus.Draft => "Bản nháp",
        AssignmentStatus.Pending_Staff_Confirm => "Chờ nhân viên cam kết",
        AssignmentStatus.Pending_Approval => "Chờ lãnh đạo duyệt",
        AssignmentStatus.Approved_Locked => "Đã duyệt (Đã khóa)",
        AssignmentStatus.In_Evaluation => "Đang đánh giá",
        _ => Status.ToString()
    };
    public string StatusBadgeClass => Status switch
    {
        AssignmentStatus.Draft => "bg-secondary",
        AssignmentStatus.Pending_Staff_Confirm => "bg-warning text-dark",
        AssignmentStatus.Pending_Approval => "bg-info text-dark",
        AssignmentStatus.Approved_Locked => "bg-success",
        AssignmentStatus.In_Evaluation => "bg-primary",
        _ => "bg-light text-dark"
    };
    public DateTime? CommittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
#endregion

#region 2. Request Models cho Lập kế hoạch & Giao KPI
public class CreateAssignmentRequest
{
    public long? Id { get; set; } // null nếu tạo mới
    public long PeriodId { get; set; }
    public long EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDraft { get; set; } = false; // true: Lưu nháp, false: Gửi cam kết
    public List<AssignmentItemRequest> Items { get; set; } = new();
}

public class AssignmentItemRequest
{
    public long? DetailId { get; set; }
    public long KpiItemId { get; set; }
    public string KpiName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal? MinThreshold { get; set; } = 0m;
    public decimal MaxCap { get; set; } = 150m;
    public decimal Weight { get; set; } // BR-02: 5% - 50%
    public string ScoringFormula { get; set; } = "HigherIsBetter";
}

public class StaffConfirmRequest
{
    public long AssignmentId { get; set; }
    public bool Agree { get; set; } = true;
    public string? ElectronicSignatureNote { get; set; }
    public string? AdjustmentReason { get; set; }
}

public class ApproveAssignmentRequest
{
    public long AssignmentId { get; set; }
    public string? Comments { get; set; }
}

public class RejectAssignmentRequest
{
    public long AssignmentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
#endregion

#region 3. DTOs Gợi ý Template & Dropdowns
public class TemplateSuggestDto
{
    public long TemplateId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public List<TemplateSuggestItemDto> Items { get; set; } = new();
}

public class TemplateSuggestItemDto
{
    public long KpiItemId { get; set; }
    public string KpiCode { get; set; } = string.Empty;
    public string KpiName { get; set; } = string.Empty;
    public string Perspective { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal? MinThreshold { get; set; }
    public decimal Weight { get; set; }
    public string ScoringFormula { get; set; } = "HigherIsBetter";
    public string? GuidanceNotes { get; set; }
}

public class EmployeeSelectDto
{
    public long EmployeeId { get; set; }
    public long UserId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public long PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public bool HasActiveAssignment { get; set; }
}

public class PeriodSelectDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActiveForPlanning { get; set; }
}
#endregion

#region 4. DTOs Điều chỉnh KPI Giữa kỳ (Revision & Diff)
public class RevisionRequest
{
    public long AssignmentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<AssignmentItemRequest> RevisedItems { get; set; } = new();
}

public class DiffComparisonDto
{
    public long AssignmentId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public int VersionFrom { get; set; } = 1;
    public int VersionTo { get; set; } = 2;
    public string RevisionReason { get; set; } = string.Empty;
    public DateTime RevisionTime { get; set; }
    public List<DiffItemDto> Differences { get; set; } = new();
}

public class DiffItemDto
{
    public long KpiItemId { get; set; }
    public string KpiCode { get; set; } = string.Empty;
    public string KpiName { get; set; } = string.Empty;
    public string ChangeType { get; set; } = "MODIFIED"; // ADDED, REMOVED, MODIFIED, UNCHANGED
    public decimal OldWeight { get; set; }
    public decimal NewWeight { get; set; }
    public decimal OldTarget { get; set; }
    public decimal NewTarget { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
#endregion
