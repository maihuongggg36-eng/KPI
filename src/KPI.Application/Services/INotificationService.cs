using KPI.Application.DTOs;
using KPI.Domain.Enums;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ quản lý Thông báo In-App Real-time & Sự kiện tự động
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Lấy danh sách 20 thông báo gần nhất kèm số lượng chưa đọc của người dùng
    /// </summary>
    Task<NotificationSummaryDto> GetUserNotificationsAsync(long userId, int limit = 20);

    /// <summary>
    /// Đánh dấu 1 thông báo là đã đọc
    /// </summary>
    Task<bool> MarkAsReadAsync(long notificationId, long userId);

    /// <summary>
    /// Đánh dấu tất cả thông báo của người dùng là đã đọc
    /// </summary>
    Task<int> MarkAllAsReadAsync(long userId);

    /// <summary>
    /// Tạo thông báo mới, lưu DB và gửi realtime qua SignalR + đưa vào hàng đợi Email
    /// </summary>
    Task<NotificationDto> CreateAndSendAsync(CreateNotificationRequest request);

    #region Sự kiện thông báo tự động theo quy trình KPI

    /// <summary>
    /// KPI được giao -> Thông báo nhân viên
    /// </summary>
    Task NotifyKpiAssignedAsync(long assignmentId);

    /// <summary>
    /// Nhân viên nộp tự chấm -> Thông báo Quản lý trực tiếp
    /// </summary>
    Task NotifySelfAssessmentSubmittedAsync(long evaluationId);

    /// <summary>
    /// Quản lý hoàn thành thẩm định -> Thông báo Lãnh đạo (Director)
    /// </summary>
    Task NotifyManagerReviewCompletedAsync(long evaluationId);

    /// <summary>
    /// Lãnh đạo phê duyệt chính thức -> Thông báo Nhân viên & Phòng Nhân sự (HR)
    /// </summary>
    Task NotifyDirectorApprovedAsync(long evaluationId);

    /// <summary>
    /// Kiểm tra thời hạn kỳ đánh giá và gửi cảnh báo (3 ngày, 1 ngày, quá hạn)
    /// </summary>
    Task CheckAndNotifyDeadlinesAsync(long periodId);

    /// <summary>
    /// Cảnh báo vi phạm trần 20% Xuất sắc (Nghị định 90 & BR-06) -> Thông báo HR & Director
    /// </summary>
    Task NotifyQuotaWarningAsync(long periodId, string departmentName, int currentCount, int maxQuota);

    #endregion
}
