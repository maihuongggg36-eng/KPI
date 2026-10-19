using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KPI.Web.Services;

/// <summary>
/// Thực thi IRealtimeNotifier: Đẩy thông điệp realtime tới SignalR NotificationHub
/// </summary>
public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRRealtimeNotifier> _logger;

    public SignalRRealtimeNotifier(IHubContext<NotificationHub> hubContext, ILogger<SignalRRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationToUserAsync(long userId, NotificationDto notification)
    {
        try
        {
            // Gửi tới group của user: user_{userId}
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Đã phát SignalR ReceiveNotification tới user_{UserId}: {Title}", userId, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi phát SignalR tới user_{UserId}", userId);
        }
    }

    public async Task SendNotificationToAllAsync(NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Đã broadcast SignalR ReceiveNotification: {Title}", notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi broadcast SignalR tới tất cả người dùng");
        }
    }
}
