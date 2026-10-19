using System.Net;
using System.Net.Mail;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Reporting;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Thực thi IEmailService: Xếp hàng đợi, gửi SMTP có cơ chế thử lại (Retry) và quản lý nhật ký
/// </summary>
public class EmailService : IEmailService
{
    private readonly KpiDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(KpiDbContext context, IConfiguration config, ILogger<EmailService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<long> EnqueueEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var emailItem = new EmailQueue
        {
            ToEmail = toEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            Status = EmailStatus.Pending,
            RetryCount = 0,
            ScheduledAt = DateTime.UtcNow
        };

        _context.EmailQueues.Add(emailItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Đã xếp email vào hàng đợi: {Subject} -> {ToEmail} (ID: {Id})", subject, toEmail, emailItem.Id);
        return emailItem.Id;
    }

    public async Task<int> ProcessPendingEmailsAsync(int batchSize = 10)
    {
        var pendingList = await _context.EmailQueues
            .Where(e => e.Status == EmailStatus.Pending && e.RetryCount < 3)
            .OrderBy(e => e.ScheduledAt)
            .Take(batchSize)
            .ToListAsync();

        if (!pendingList.Any()) return 0;

        int sentCount = 0;
        var host = _config["SmtpSettings:Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(_config["SmtpSettings:Port"], out var p) ? p : 587;
        var enableSsl = bool.TryParse(_config["SmtpSettings:EnableSsl"], out var ssl) && ssl;
        var userName = _config["SmtpSettings:UserName"] ?? "kpi.notifications@napa.edu.vn";
        var password = _config["SmtpSettings:Password"] ?? "mock-password";
        var senderEmail = _config["SmtpSettings:SenderEmail"] ?? "no-reply@napa.edu.vn";
        var senderName = _config["SmtpSettings:SenderName"] ?? "Hệ thống KPI Học viện NAPA";

        foreach (var email in pendingList)
        {
            try
            {
                // Thử gửi qua SmtpClient nếu cấu hình đầy đủ
                if (!string.IsNullOrEmpty(host) && !host.Contains("mock") && !password.Contains("mock"))
                {
                    using var client = new SmtpClient(host, port)
                    {
                        EnableSsl = enableSsl,
                        Credentials = new NetworkCredential(userName, password),
                        Timeout = 10000
                    };

                    using var mailMsg = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = email.Subject,
                        Body = email.HtmlBody,
                        IsBodyHtml = true
                    };
                    mailMsg.To.Add(email.ToEmail);

                    await client.SendMailAsync(mailMsg);
                }
                else
                {
                    // Chế độ Development / Mock SMTP: Ghi log mô phỏng gửi thành công
                    _logger.LogInformation("[DEV SMTP MOCK] Gửi email thành công tới: {ToEmail} | Tiêu đề: {Subject}", email.ToEmail, email.Subject);
                }

                email.Status = EmailStatus.Sent;
                email.SentAt = DateTime.UtcNow;
                email.ErrorMessage = null;
                sentCount++;
            }
            catch (Exception ex)
            {
                email.RetryCount++;
                email.ErrorMessage = $"Lần thử {email.RetryCount}: {ex.Message}";
                if (email.RetryCount >= 3)
                {
                    email.Status = EmailStatus.Failed;
                    _logger.LogError(ex, "Gửi email thất bại sau 3 lần thử tới: {ToEmail}", email.ToEmail);
                }
                else
                {
                    _logger.LogWarning("Gửi email thất bại lần {Count} tới: {ToEmail}. Sẽ thử lại sau.", email.RetryCount, email.ToEmail);
                }
            }
        }

        await _context.SaveChangesAsync();
        return sentCount;
    }

    public async Task<List<EmailQueueDto>> GetEmailLogsAsync(int limit = 50)
    {
        return await _context.EmailQueues
            .OrderByDescending(e => e.ScheduledAt)
            .Take(limit)
            .Select(e => new EmailQueueDto
            {
                Id = e.Id,
                ToEmail = e.ToEmail,
                Subject = e.Subject,
                HtmlBody = e.HtmlBody,
                Status = e.Status,
                RetryCount = e.RetryCount,
                ErrorMessage = e.ErrorMessage,
                ScheduledAt = e.ScheduledAt,
                SentAt = e.SentAt
            })
            .ToListAsync();
    }

    public async Task<bool> RetryEmailAsync(long emailQueueId)
    {
        var email = await _context.EmailQueues.FindAsync(emailQueueId);
        if (email == null) return false;

        email.Status = EmailStatus.Pending;
        email.RetryCount = 0;
        email.ErrorMessage = null;
        email.ScheduledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
