using KPI.Domain.Enums;

namespace KPI.Application.DTOs;

/// <summary>
/// DTO thông báo hiển thị cho người dùng
/// </summary>
public class NotificationDto
{
    public long Id { get; set; }
    public long RecipientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
}

/// <summary>
/// DTO tổng hợp thông báo
/// </summary>
public class NotificationSummaryDto
{
    public int UnreadCount { get; set; }
    public List<NotificationDto> RecentNotifications { get; set; } = new();
}

/// <summary>
/// DTO tạo thông báo mới
/// </summary>
public class CreateNotificationRequest
{
    public long RecipientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string? TargetUrl { get; set; }
    public bool SendEmail { get; set; } = true;
    public string? RecipientEmail { get; set; }
}

/// <summary>
/// DTO hàng đợi gửi email
/// </summary>
public class EmailQueueDto
{
    public long Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string StatusBadgeClass => Status switch
    {
        EmailStatus.Sent => "bg-success text-white",
        EmailStatus.Failed => "bg-danger text-white",
        _ => "bg-warning text-dark"
    };
}
