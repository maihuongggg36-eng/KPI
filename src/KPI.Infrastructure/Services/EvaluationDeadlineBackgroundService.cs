using KPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Background Service định kỳ kiểm tra các kỳ đánh giá đã quá hạn SelfDeadline (BR-12)
/// </summary>
public class EvaluationDeadlineBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EvaluationDeadlineBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Chạy mỗi giờ

    public EvaluationDeadlineBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EvaluationDeadlineBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EvaluationDeadlineBackgroundService đã khởi chạy.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var evaluationService = scope.ServiceProvider.GetRequiredService<IEvaluationService>();

                var closedCount = await evaluationService.CheckAndAutoCloseExpiredEvaluationsAsync();
                if (closedCount > 0)
                {
                    _logger.LogInformation("Đã tự động khóa và gán 0 điểm cho {ClosedCount} hồ sơ quá hạn (BR-12).", closedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét hạn chót tự đánh giá (BR-12).");
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

        _logger.LogInformation("EvaluationDeadlineBackgroundService đã dừng.");
    }
}
