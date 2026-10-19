using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ nghiệp vụ Giao chỉ tiêu KPI & Kế hoạch cá nhân (Phiên 6/12)
/// </summary>
public interface IKpiPlanningService
{
    /// <summary>
    /// Lấy danh sách các bản giao KPI theo bộ lọc (Hỗ trợ phân quyền Manager, Director, HR)
    /// </summary>
    Task<List<KpiAssignmentSummaryDto>> GetAssignmentsAsync(long? periodId, long? departmentId, string? status, string? search, long currentUserId, string role);

    /// <summary>
    /// Lấy chi tiết bản giao KPI theo ID
    /// </summary>
    Task<KpiAssignmentDto?> GetAssignmentByIdAsync(long id);

    /// <summary>
    /// Lấy bản giao KPI cá nhân của nhân viên đang đăng nhập ("KPI Của Tôi")
    /// </summary>
    Task<KpiAssignmentDto?> GetMyKpiAsync(long employeeUserId, long? periodId = null);

    /// <summary>
    /// Lấy danh sách nhân viên thuộc quyền quản lý để giao KPI
    /// </summary>
    Task<List<EmployeeSelectDto>> GetEmployeesForAssignmentAsync(long managerUserId);

    /// <summary>
    /// Lấy danh sách kỳ đánh giá phục vụ lập kế hoạch
    /// </summary>
    Task<List<PeriodSelectDto>> GetEvaluationPeriodsForPlanningAsync();

    /// <summary>
    /// Tự động gợi ý bộ chỉ tiêu KPI mẫu dựa trên chức danh của nhân viên
    /// </summary>
    Task<TemplateSuggestDto?> GetTemplateSuggestionsAsync(long employeeId);

    /// <summary>
    /// Lưu nháp hoặc Gửi cam kết bản giao KPI (Kiểm soát BR-01, BR-02, BR-03)
    /// </summary>
    Task<long> SaveOrSubmitAssignmentAsync(CreateAssignmentRequest request, long currentUserId);

    /// <summary>
    /// Nhân viên ký cam kết điện tử (Ghi nhận CommittedAt và chuyển trạng thái)
    /// </summary>
    Task<bool> StaffConfirmAssignmentAsync(StaffConfirmRequest request, long employeeUserId);

    /// <summary>
    /// Nhân viên từ chối và yêu cầu điều chỉnh bản giao KPI
    /// </summary>
    Task<bool> StaffRequestAdjustmentAsync(long assignmentId, string reason, long employeeUserId);

    /// <summary>
    /// Lãnh đạo phê duyệt bản giao KPI (Khóa toàn bộ bản giao BR-03)
    /// </summary>
    Task<bool> DirectorApproveAsync(ApproveAssignmentRequest request, long directorUserId);

    /// <summary>
    /// Lãnh đạo trả lại bản giao KPI kèm lý do
    /// </summary>
    Task<bool> DirectorRejectAsync(RejectAssignmentRequest request, long directorUserId);

    /// <summary>
    /// Điều chỉnh KPI giữa kỳ: Lưu phiên bản snapshot và tạo yêu cầu điều chỉnh
    /// </summary>
    Task<bool> CreateRevisionAsync(RevisionRequest request, long currentUserId);

    /// <summary>
    /// So sánh sự khác biệt (Diff Comparison) giữa các phiên bản điều chỉnh
    /// </summary>
    Task<DiffComparisonDto> GetRevisionDiffAsync(long assignmentId);

    /// <summary>
    /// Xóa bản giao nháp
    /// </summary>
    Task<bool> DeleteDraftAssignmentAsync(long assignmentId, long currentUserId);
}
