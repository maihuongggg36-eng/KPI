using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ Hàng đợi Email & Gửi email tự động
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Đưa email vào hàng đợi gửi (email_queues)
    /// </summary>
    Task<long> EnqueueEmailAsync(string toEmail, string subject, string htmlBody);

    /// <summary>
    /// Quét và gửi các email trong hàng đợi có trạng thái Pending (tối đa 3 lần thử)
    /// </summary>
    Task<int> ProcessPendingEmailsAsync(int batchSize = 10);

    /// <summary>
    /// Lấy danh sách nhật ký gửi email cho trang quản trị Admin
    /// </summary>
    Task<List<EmailQueueDto>> GetEmailLogsAsync(int limit = 50);

    /// <summary>
    /// Thử gửi lại một email bị lỗi
    /// </summary>
    Task<bool> RetryEmailAsync(long emailQueueId);
}
