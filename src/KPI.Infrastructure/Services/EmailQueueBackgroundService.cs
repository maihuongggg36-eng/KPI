using KPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Hosted Service nền tự động quét và gửi các email trong hàng đợi (email_queues) định kỳ
/// </summary>
public class EmailQueueBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public EmailQueueBackgroundService(IServiceProvider serviceProvider, ILogger<EmailQueueBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueBackgroundService đã khởi chạy. Chu kỳ quét: {Seconds}s.", _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var sentCount = await emailService.ProcessPendingEmailsAsync(batchSize: 10);
                if (sentCount > 0)
                {
                    _logger.LogInformation("Đã xử lý gửi thành công {Count} email từ hàng đợi.", sentCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong tiến trình EmailQueueBackgroundService.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("EmailQueueBackgroundService đã dừng.");
    }
}
