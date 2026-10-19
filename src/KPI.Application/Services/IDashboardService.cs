using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Giao diện dịch vụ phân hệ Bàn làm việc Điều hành Đa Vai trò (Role-Based Dashboards)
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Lấy dữ liệu tổng hợp cho Bàn làm việc Giám đốc / Lãnh đạo cấp cao (SCR-03A)
    /// </summary>
    Task<DirectorDashboardDto> GetDirectorDashboardDataAsync();

    /// <summary>
    /// Lấy dữ liệu cho Bàn làm việc Trưởng phòng / Quản lý chuyên môn (SCR-03B)
    /// </summary>
    Task<ManagerDashboardDto> GetManagerDashboardDataAsync(long? departmentId = null);

    /// <summary>
    /// Lấy dữ liệu cho Bàn làm việc Cán bộ / Nhân viên cá nhân
    /// </summary>
    Task<EmployeeDashboardDto> GetEmployeeDashboardDataAsync(long userId);

    /// <summary>
    /// Lấy dữ liệu cho Bàn làm việc Kế toán viên (Lương & Quỹ tiền mặt)
    /// </summary>
    Task<AccountantDashboardDto> GetAccountantDashboardDataAsync();

    /// <summary>
    /// Lấy dữ liệu cho Bàn làm việc Cán bộ Tổ chức Cán bộ (HR Admin) & Giám sát Nghị định 90
    /// </summary>
    Task<HrDashboardDto> GetHrDashboardDataAsync();

    /// <summary>
    /// Thao tác nhanh: Phê duyệt kết quả đánh giá (Giám đốc / Trưởng phòng)
    /// </summary>
    Task<bool> QuickApproveEvaluationAsync(long evaluationId, long currentUserId, string comment);

    /// <summary>
    /// Thao tác nhanh: Xác nhận chi trả tiền mặt (Kế toán viên)
    /// </summary>
    Task<bool> QuickDisburseCashVoucherAsync(long voucherId, long accountantUserId);
}
