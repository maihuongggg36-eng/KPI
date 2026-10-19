using KPI.Domain.Common;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Enums;

namespace KPI.Domain.Entities.Reporting;

/// <summary>
/// 36. Cấu hình bảng điều khiển người dùng (dashboard_configs)
/// </summary>
public class DashboardConfig : BaseEntity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string WidgetKey { get; set; } = string.Empty;
    public int GridX { get; set; } = 0;
    public int GridY { get; set; } = 0;
    public int GridWidth { get; set; } = 4;
    public int GridHeight { get; set; } = 3;
}

/// <summary>
/// 37. Kết xuất tài liệu báo cáo (reports)
/// </summary>
public class Report : BaseEntity
{
    public long PeriodId { get; set; }
    public virtual EvaluationPeriod Period { get; set; } = null!;

    public long GeneratedBy { get; set; }
    public virtual User GeneratedByUser { get; set; } = null!;

    public string ReportType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ParametersJson { get; set; }
    public ExportFormat ExportFormat { get; set; } = ExportFormat.PDF;
    public string FileUrl { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 38. Thông báo chuông In-App (notifications)
/// </summary>
public class Notification : BaseEntity
{
    public long RecipientId { get; set; }
    public virtual User Recipient { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 39. Mẫu thông báo & mẫu email (notification_templates)
/// </summary>
public class NotificationTemplate : BaseEntity
{
    public string EventCode { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string Channels { get; set; } = "IN_APP,EMAIL";
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 40. Hàng đợi gửi email tự động (email_queues)
/// </summary>
public class EmailQueue : BaseEntity
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
