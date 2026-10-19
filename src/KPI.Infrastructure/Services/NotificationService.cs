using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Reporting;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Thực thi INotificationService: Quản lý thông báo in-app, tích hợp SignalR realtime và kích hoạt sự kiện tự động
/// </summary>
public class NotificationService : INotificationService
{
    private readonly KpiDbContext _context;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        KpiDbContext context,
        IRealtimeNotifier realtimeNotifier,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _realtimeNotifier = realtimeNotifier;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<NotificationSummaryDto> GetUserNotificationsAsync(long userId, int limit = 20)
    {
        var query = _context.Notifications
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var unreadCount = await _context.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead);

        var entities = await query.Take(limit).ToListAsync();
        var now = DateTime.UtcNow;

        var dtos = entities.Select(n => MapToDto(n, now)).ToList();

        return new NotificationSummaryDto
        {
            UnreadCount = unreadCount,
            RecentNotifications = dtos
        };
    }

    public async Task<bool> MarkAsReadAsync(long notificationId, long userId)
    {
        var item = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId);

        if (item == null) return false;

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(long userId)
    {
        var unreadList = await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync();

        if (!unreadList.Any()) return 0;

        var now = DateTime.UtcNow;
        foreach (var item in unreadList)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await _context.SaveChangesAsync();
        return unreadList.Count;
    }

    public async Task<NotificationDto> CreateAndSendAsync(CreateNotificationRequest request)
    {
        var notif = new Notification
        {
            RecipientId = request.RecipientId,
            Title = request.Title,
            Content = request.Content,
            Type = request.Type,
            TargetUrl = request.TargetUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync();

        var dto = MapToDto(notif, DateTime.UtcNow);

        // 1. Gửi realtime qua SignalR
        try
        {
            await _realtimeNotifier.SendNotificationToUserAsync(request.RecipientId, dto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi phát SignalR realtime tới User {UserId}", request.RecipientId);
        }

        // 2. Nếu có yêu cầu gửi Email, tạo template và xếp vào hàng đợi
        if (request.SendEmail)
        {
            var recipientEmail = request.RecipientEmail;
            if (string.IsNullOrEmpty(recipientEmail))
            {
                var user = await _context.Users.FindAsync(request.RecipientId);
                recipientEmail = user?.Email;
            }

            if (!string.IsNullOrEmpty(recipientEmail))
            {
                var htmlBody = GenerateEmailTemplate(request.Title, request.Content, request.TargetUrl, request.Type);
                await _emailService.EnqueueEmailAsync(recipientEmail, $"[KPI Học viện] {request.Title}", htmlBody);
            }
        }

        return dto;
    }

    #region Sự kiện thông báo tự động theo quy trình KPI

    public async Task NotifyKpiAssignedAsync(long assignmentId)
    {
        var assignment = await _context.KpiAssignments
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null) return;

        await CreateAndSendAsync(new CreateNotificationRequest
        {
            RecipientId = assignment.Employee.UserId,
            Title = "Bạn vừa được giao Bản cam kết KPI mới",
            Content = $"Bản chỉ tiêu KPI kỳ này đã được quản lý thiết lập. Vui lòng kiểm tra và xác nhận cam kết.",
            Type = NotificationType.Info,
            TargetUrl = $"/KpiPlanning/Commit?assignmentId={assignmentId}",
            SendEmail = true
        });
    }

    public async Task NotifySelfAssessmentSubmittedAsync(long evaluationId)
    {
        var eval = await _context.Evaluations
            .Include(e => e.Employee)
                .ThenInclude(emp => emp.Department)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        if (eval == null) return;

        // Tìm quản lý của phòng ban đó
        var managerEmp = await _context.EmployeeProfiles
            .Include(p => p.Position)
            .Where(p => p.DepartmentId == eval.Employee.DepartmentId && p.Position.IsLeadership && !p.IsDeleted)
            .FirstOrDefaultAsync();

        long recipientUserId = managerEmp?.UserId ?? 1; // Fallback to Admin/Director nếu chưa gán manager

        await CreateAndSendAsync(new CreateNotificationRequest
        {
            RecipientId = recipientUserId,
            Title = "Cán bộ đã hoàn thành Tự đánh giá KPI",
            Content = $"Cán bộ {eval.Employee.FullName} ({eval.Employee.Department.Name}) đã nộp phiếu tự đánh giá. Vui lòng thẩm định điểm.",
            Type = NotificationType.Info,
            TargetUrl = $"/Evaluation/ManagerReview?evaluationId={evaluationId}",
            SendEmail = true
        });
    }

    public async Task NotifyManagerReviewCompletedAsync(long evaluationId)
    {
        var eval = await _context.Evaluations
            .Include(e => e.Employee)
                .ThenInclude(emp => emp.Department)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        if (eval == null) return;

        // Tìm Lãnh đạo Ban Giám đốc (Role Director)
        var directorUser = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.UserRoles.Any(r => r.Role.Code == "DIRECTOR") && !u.IsDeleted)
            .FirstOrDefaultAsync();

        long directorId = directorUser?.Id ?? 1;

        await CreateAndSendAsync(new CreateNotificationRequest
        {
            RecipientId = directorId,
            Title = "Hoàn tất Thẩm định điểm KPI cấp Đơn vị",
            Content = $"Kết quả thẩm định của cán bộ {eval.Employee.FullName} ({eval.Employee.Department.Name}) đã hoàn thành. Chờ Lãnh đạo phê duyệt.",
            Type = NotificationType.Success,
            TargetUrl = $"/Evaluation/DirectorReview?evaluationId={evaluationId}",
            SendEmail = true
        });
    }

    public async Task NotifyDirectorApprovedAsync(long evaluationId)
    {
        var eval = await _context.Evaluations
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        if (eval == null) return;

        // 1. Báo cho Nhân viên
        await CreateAndSendAsync(new CreateNotificationRequest
        {
            RecipientId = eval.Employee.UserId,
            Title = "Kết quả đánh giá KPI chính thức đã được Phê duyệt",
            Content = $"Ban Giám đốc đã phê duyệt chính thức kết quả đánh giá KPI của bạn với Điểm tổng kết: {eval.FinalScore.ToString("0.0")}.",
            Type = NotificationType.Success,
            TargetUrl = $"/Evaluation/SelfAssess",
            SendEmail = true
        });

        // 2. Báo cho Phòng Nhân sự (HR_ADMIN)
        var hrUser = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.UserRoles.Any(r => r.Role.Code == "HR_ADMIN") && !u.IsDeleted)
            .FirstOrDefaultAsync();

        if (hrUser != null)
        {
            await CreateAndSendAsync(new CreateNotificationRequest
            {
                RecipientId = hrUser.Id,
                Title = "Hồ sơ đánh giá cán bộ đã được phê duyệt chính thức",
                Content = $"Cán bộ {eval.Employee.FullName} đã được Ban Giám đốc phê duyệt KPI. Đã sẵn sàng cập nhật hồ sơ xếp loại NĐ 90.",
                Type = NotificationType.Info,
                TargetUrl = $"/CivilServant/Index",
                SendEmail = false
            });
        }
    }

    public async Task CheckAndNotifyDeadlinesAsync(long periodId)
    {
        var period = await _context.EvaluationPeriods.FindAsync(periodId);
        if (period == null) return;

        var now = DateTime.UtcNow;

        // Kiểm tra thời hạn Tự đánh giá
        if (period.SelfDeadline != default)
        {
            var timeLeft = period.SelfDeadline - now;
            string deadlineWarning = "";
            NotificationType type = NotificationType.Warning;

            if (timeLeft.TotalHours <= 0)
            {
                deadlineWarning = "Hạn chót Tự đánh giá KPI ĐÃ HẾT HẠN! Vui lòng nộp gấp hoặc liên hệ phòng Nhân sự.";
                type = NotificationType.Urgent;
            }
            else if (timeLeft.TotalDays <= 1)
            {
                deadlineWarning = "Chỉ còn dưới 24 giờ là hết hạn nộp Tự đánh giá KPI!";
                type = NotificationType.Urgent;
            }
            else if (timeLeft.TotalDays <= 3)
            {
                deadlineWarning = $"Còn {Math.Ceiling(timeLeft.TotalDays)} ngày tới hạn nộp Tự đánh giá KPI.";
                type = NotificationType.Warning;
            }

            if (!string.IsNullOrEmpty(deadlineWarning))
            {
                // Lấy danh sách nhân sự chưa hoàn thành nộp
                var unsubmittedUserIds = await _context.Evaluations
                    .Where(e => e.PeriodId == periodId && (e.Status == EvaluationStatus.Not_Started || e.Status == EvaluationStatus.Self_Assessing))
                    .Select(e => e.Employee.UserId)
                    .Distinct()
                    .Take(50)
                    .ToListAsync();

                foreach (var uid in unsubmittedUserIds)
                {
                    await CreateAndSendAsync(new CreateNotificationRequest
                    {
                        RecipientId = uid,
                        Title = "Nhắc nhở: Sắp tới hạn nộp Tự đánh giá KPI",
                        Content = deadlineWarning,
                        Type = type,
                        TargetUrl = "/Evaluation/SelfAssess",
                        SendEmail = true
                    });
                }
            }
        }
    }

    public async Task NotifyQuotaWarningAsync(long periodId, string departmentName, int currentCount, int maxQuota)
    {
        var directorsAndHr = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.UserRoles.Any(r => r.Role.Code == "DIRECTOR" || r.Role.Code == "HR_ADMIN") && !u.IsDeleted)
            .ToListAsync();

        foreach (var user in directorsAndHr)
        {
            await CreateAndSendAsync(new CreateNotificationRequest
            {
                RecipientId = user.Id,
                Title = $"CẢNH BÁO: Vi phạm trần 20% Xuất sắc tại {departmentName}",
                Content = $"Đơn vị '{departmentName}' hiện có {currentCount} cán bộ đề xuất Xuất sắc, vượt quá trần tối đa {maxQuota} cán bộ theo Nghị định 90 & BR-06.",
                Type = NotificationType.Urgent,
                TargetUrl = $"/CivilServant/Dashboard?periodId={periodId}",
                SendEmail = true
            });
        }
    }

    #endregion

    #region Helper Methods

    private static NotificationDto MapToDto(Notification n, DateTime now)
    {
        return new NotificationDto
        {
            Id = n.Id,
            RecipientId = n.RecipientId,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type,
            TargetUrl = n.TargetUrl,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            CreatedAt = n.CreatedAt,
            TimeAgo = FormatTimeAgo(n.CreatedAt, now),
            BadgeClass = n.Type switch
            {
                NotificationType.Urgent or NotificationType.Error => "bg-danger text-white",
                NotificationType.Warning => "bg-warning text-dark",
                NotificationType.Success => "bg-success text-white",
                _ => "bg-primary text-white"
            },
            IconClass = n.Type switch
            {
                NotificationType.Urgent or NotificationType.Error => "bi-shield-fill-exclamation text-danger",
                NotificationType.Warning => "bi-exclamation-triangle-fill text-warning",
                NotificationType.Success => "bi-check-circle-fill text-success",
                _ => "bi-info-circle-fill text-primary"
            }
        };
    }

    private static string FormatTimeAgo(DateTime past, DateTime now)
    {
        var diff = now - past;
        if (diff.TotalSeconds < 60) return "Vừa xong";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
        if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} ngày trước";
        return past.ToString("dd/MM/yyyy HH:mm");
    }

    private static string GenerateEmailTemplate(string title, string content, string? targetUrl, NotificationType type)
    {
        var color = type switch
        {
            NotificationType.Urgent or NotificationType.Error => "#dc2626",
            NotificationType.Warning => "#d97706",
            NotificationType.Success => "#16a34a",
            _ => "#2563eb"
        };

        var buttonHtml = !string.IsNullOrEmpty(targetUrl)
            ? $@"<div style='margin-top: 25px; text-align: center;'>
                    <a href='http://localhost:5000{targetUrl}' style='background-color: {color}; color: #ffffff; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; display: inline-block;'>
                        Xem Chi Tiết Ngay &rarr;
                    </a>
                 </div>"
            : "";

        return $@"
        <div style='font-family: Arial, sans-serif; background-color: #f1f5f9; padding: 30px; color: #1e293b;'>
            <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); border-top: 5px solid {color};'>
                <div style='padding: 24px; background: #0f172a; color: #ffffff; text-align: center;'>
                    <h2 style='margin: 0; font-size: 18px; text-transform: uppercase; letter-spacing: 1px;'>HỌC VIỆN HÀNH CHÍNH QUỐC GIA</h2>
                    <p style='margin: 4px 0 0 0; font-size: 13px; color: #94a3b8;'>Hệ Thống Đánh Giá & Quản Trị Hiệu Suất KPI</p>
                </div>
                <div style='padding: 30px;'>
                    <h3 style='margin-top: 0; color: {color}; font-size: 20px;'>{title}</h3>
                    <p style='font-size: 15px; line-height: 1.6; color: #334155;'>{content}</p>
                    {buttonHtml}
                </div>
                <div style='padding: 16px; background: #f8fafc; border-top: 1px solid #e2e8f0; font-size: 12px; color: #64748b; text-align: center;'>
                    Email này được gửi tự động từ Hệ thống KPI NAPA. Vui lòng không trả lời thư này.
                </div>
            </div>
        </div>";
    }

    #endregion
}
