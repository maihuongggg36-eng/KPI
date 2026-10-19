using KPI.Application.DTOs;

namespace KPI.Web.ViewModels.Dashboard;

public class DashboardIndexViewModel
{
    public string ActiveRole { get; set; } = "Director";
    public string UserFullName { get; set; } = string.Empty;
    public string CurrentUsername { get; set; } = string.Empty;
    public string PrimaryRole { get; set; } = string.Empty;
    public bool CanSwitchRole { get; set; } = true;

    // Dữ liệu cho từng phân hệ
    public DirectorDashboardDto DirectorData { get; set; } = new();
    public ManagerDashboardDto ManagerData { get; set; } = new();
    public EmployeeDashboardDto EmployeeData { get; set; } = new();
    public AccountantDashboardDto AccountantData { get; set; } = new();
    public HrDashboardDto HrData { get; set; } = new();
}

public class QuickApproveRequest
{
    public long EvaluationId { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class QuickDisburseRequest
{
    public long VoucherId { get; set; }
}
