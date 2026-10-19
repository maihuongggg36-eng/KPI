using System.Security.Claims;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

/// <summary>
/// Controller xử lý API Thông báo Real-time, Đánh dấu đã đọc và Nhật ký Email
/// </summary>
[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    private long GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : 1;
    }

    /// <summary>
    /// Lấy danh sách thông báo gần nhất và số lượng chưa đọc (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRecent(int limit = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var summary = await _notificationService.GetUserNotificationsAsync(userId, limit);
            return Json(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải thông báo");
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Đánh dấu 1 thông báo là đã đọc
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead([FromForm] long id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ok = await _notificationService.MarkAsReadAsync(id, userId);
            return Json(new { success = ok });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo là đã đọc
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true, count = count, message = $"Đã đánh dấu {count} thông báo là đã đọc." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Gửi thông báo thử nghiệm (phục vụ test và demo realtime)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TriggerTest([FromForm] string title, [FromForm] string content, [FromForm] string type)
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifType = type?.ToUpper() switch
            {
                "WARNING" => NotificationType.Warning,
                "SUCCESS" => NotificationType.Success,
                "URGENT" or "ERROR" => NotificationType.Urgent,
                _ => NotificationType.Info
            };

            var notif = await _notificationService.CreateAndSendAsync(new CreateNotificationRequest
            {
                RecipientId = userId,
                Title = string.IsNullOrWhiteSpace(title) ? "Thông báo thử nghiệm hệ thống KPI" : title,
                Content = string.IsNullOrWhiteSpace(content) ? "Hệ thống kiểm tra kết nối SignalR thành công! Âm thanh và popup hoạt động hoàn hảo." : content,
                Type = notifType,
                TargetUrl = "/Dashboard",
                SendEmail = false
            });

            return Json(new { success = true, data = notif });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Trang quản lý Nhật ký gửi Email (Admin & Director)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,DIRECTOR,HR_ADMIN")]
    public async Task<IActionResult> EmailLogs(int limit = 50)
    {
        var logs = await _emailService.GetEmailLogsAsync(limit);
        return View(logs);
    }

    /// <summary>
    /// Thử gửi lại một email trong hàng đợi
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,DIRECTOR,HR_ADMIN")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryEmail([FromForm] long id)
    {
        try
        {
            var ok = await _emailService.RetryEmailAsync(id);
            return Json(new { success = ok, message = ok ? "Đã đưa email vào hàng đợi gửi lại!" : "Không tìm thấy email." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
