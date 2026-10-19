using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Giao diện trừu tượng hóa gửi thông báo Real-time (SignalR / WebSockets)
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>
    /// Gửi thông báo tới một người dùng cụ thể
    /// </summary>
    Task SendNotificationToUserAsync(long userId, NotificationDto notification);

    /// <summary>
    /// Gửi thông báo tới toàn bộ người dùng đang online
    /// </summary>
    Task SendNotificationToAllAsync(NotificationDto notification);
}
