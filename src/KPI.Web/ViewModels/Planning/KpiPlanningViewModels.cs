using KPI.Application.DTOs;

namespace KPI.Web.ViewModels.Planning;

public class AssignKpiViewModel
{
    public List<EmployeeSelectDto> Employees { get; set; } = new();
    public List<PeriodSelectDto> Periods { get; set; } = new();
    public List<KpiMasterItemDetailDto> MasterKpis { get; set; } = new();
    public long? SelectedEmployeeId { get; set; }
    public long? SelectedPeriodId { get; set; }
    public KpiAssignmentDto? ExistingAssignment { get; set; }
    public TemplateSuggestDto? SuggestedTemplate { get; set; }
}

public class MyKpiViewModel
{
    public KpiAssignmentDto? Assignment { get; set; }
    public List<PeriodSelectDto> Periods { get; set; } = new();
    public long SelectedPeriodId { get; set; }
    public decimal OverallCompletionRate { get; set; } = 0m;
    public string ViewMode { get; set; } = "cards";
}

public class StaffConfirmViewModel
{
    public KpiAssignmentDto Assignment { get; set; } = new();
    public bool CanConfirm { get; set; } = true;
}

public class ApprovalListViewModel
{
    public List<KpiAssignmentSummaryDto> Assignments { get; set; } = new();
    public List<PeriodSelectDto> Periods { get; set; } = new();
    public List<DepartmentTreeNodeDto> Departments { get; set; } = new();
    public long? SelectedPeriodId { get; set; }
    public long? SelectedDepartmentId { get; set; }
    public string? SelectedStatus { get; set; }
    public string? SearchKeyword { get; set; }
    public int TotalPending => Assignments.Count(a => a.Status == Domain.Enums.AssignmentStatus.Pending_Approval || a.Status == Domain.Enums.AssignmentStatus.Pending_Staff_Confirm);
    public int TotalApproved => Assignments.Count(a => a.Status == Domain.Enums.AssignmentStatus.Approved_Locked);
}

public class RevisionDiffViewModel
{
    public DiffComparisonDto DiffData { get; set; } = new();
    public KpiAssignmentDto? Assignment { get; set; }
}
