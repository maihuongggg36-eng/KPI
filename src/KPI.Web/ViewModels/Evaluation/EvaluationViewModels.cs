using KPI.Application.DTOs;

namespace KPI.Web.ViewModels.Evaluation;

/// <summary>
/// ViewModel cho màn hình Phiếu tự đánh giá (Employee)
/// </summary>
public class SelfAssessmentViewModel
{
    public EvaluationDto Evaluation { get; set; } = new();
    public string CurrentUserRole { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; } = false;
    public string ReturnUrl { get; set; } = "/Evaluation/SelfAssess";
}

/// <summary>
/// ViewModel cho danh sách hồ sơ Quản lý cần thẩm định
/// </summary>
public class ManagerEvaluationListViewModel
{
    public List<EvaluationDto> Evaluations { get; set; } = new();
    public long? SelectedPeriodId { get; set; }
    public long? SelectedDepartmentId { get; set; }
    public string? SelectedStatus { get; set; }

    public int TotalPending => Evaluations.Count(e => e.Status == Domain.Enums.EvaluationStatus.Self_Submitted || e.Status == Domain.Enums.EvaluationStatus.Manager_Reviewing);
    public int TotalApproved => Evaluations.Count(e => e.Status == Domain.Enums.EvaluationStatus.Pending_Director_Approval || e.Status == Domain.Enums.EvaluationStatus.Final_Approved || e.Status == Domain.Enums.EvaluationStatus.Locked);
    public int TotalOverdue => Evaluations.Count(e => e.IsSelfDeadlineExpired && (e.Status == Domain.Enums.EvaluationStatus.Not_Started || e.Status == Domain.Enums.EvaluationStatus.Self_Assessing));
}

/// <summary>
/// ViewModel cho màn hình Quản lý thẩm định chi tiết
/// </summary>
public class ManagerReviewViewModel
{
    public EvaluationDto Evaluation { get; set; } = new();
    public string CurrentUserRole { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; } = false;
}

/// <summary>
/// ViewModel cho danh sách hồ sơ Lãnh đạo phê duyệt
/// </summary>
public class DirectorEvaluationListViewModel
{
    public List<EvaluationDto> Evaluations { get; set; } = new();
    public long? SelectedPeriodId { get; set; }
    public long? SelectedDepartmentId { get; set; }

    public int PendingCount => Evaluations.Count(e => e.Status == Domain.Enums.EvaluationStatus.Pending_Director_Approval);
    public int ApprovedCount => Evaluations.Count(e => e.Status == Domain.Enums.EvaluationStatus.Final_Approved || e.Status == Domain.Enums.EvaluationStatus.Locked);
    public int DisputedCount => Evaluations.Count(e => e.IsDisputed);
}

/// <summary>
/// ViewModel cho màn hình Phê duyệt chi tiết của Lãnh đạo
/// </summary>
public class DirectorReviewViewModel
{
    public EvaluationDto Evaluation { get; set; } = new();
    public bool HasApprovedInitiative { get; set; } = false;
    public bool HasDisciplinaryAction { get; set; } = false;
    public string RecommendedRank { get; set; } = "Hoàn thành tốt nhiệm vụ";
}

/// <summary>
/// ViewModel cho màn hình Quản lý khiếu nại (Disputes)
/// </summary>
public class EvaluationDisputesViewModel
{
    public List<EvaluationDisputeDto> Disputes { get; set; } = new();
    public long? SelectedPeriodId { get; set; }
}
