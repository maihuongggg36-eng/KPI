using KPI.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KPI.Web.Health;

/// <summary>
/// Kiểm tra kết nối Cơ sở dữ liệu KpiDbContext
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly KpiDbContext _dbContext;

    public DatabaseHealthCheck(KpiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                return HealthCheckResult.Healthy("Kết nối Cơ sở dữ liệu SQL Server hoạt động bình thường.");
            }
            return HealthCheckResult.Degraded("Không thể kết nối trực tiếp đến Cơ sở dữ liệu SQL Server.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Lỗi kết nối CSDL: " + ex.Message, ex);
        }
    }
}

/// <summary>
/// Kiểm tra dung lượng RAM đang được cấp phát bởi ứng dụng
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var allocatedMb = allocatedBytes / 1024.0 / 1024.0;

        var data = new Dictionary<string, object>
        {
            { "AllocatedMemoryMB", Math.Round(allocatedMb, 2) },
            { "Gen0Collections", GC.CollectionCount(0) },
            { "Gen1Collections", GC.CollectionCount(1) },
            { "Gen2Collections", GC.CollectionCount(2) }
        };

        // Ngưỡng cảnh báo: 1024 MB
        if (allocatedMb > 1024)
        {
            return Task.FromResult(HealthCheckResult.Degraded($"Bộ nhớ ứng dụng cao: {allocatedMb:F1} MB", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Bộ nhớ ứng dụng tối ưu: {allocatedMb:F1} MB", data));
    }
}

/// <summary>
/// Kiểm tra dung lượng ổ đĩa lưu trữ của hệ thống
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var appDrive = Path.GetPathRoot(AppContext.BaseDirectory) ?? "C:\\";
            var driveInfo = new DriveInfo(appDrive);

            if (driveInfo.IsReady)
            {
                var freeMb = driveInfo.AvailableFreeSpace / 1024.0 / 1024.0;
                var totalMb = driveInfo.TotalSize / 1024.0 / 1024.0;
                var freePercent = (freeMb / totalMb) * 100.0;

                var data = new Dictionary<string, object>
                {
                    { "DriveName", driveInfo.Name },
                    { "AvailableFreeSpaceMB", Math.Round(freeMb, 2) },
                    { "TotalSizeMB", Math.Round(totalMb, 2) },
                    { "FreePercentage", Math.Round(freePercent, 2) }
                };

                if (freePercent < 5.0 || freeMb < 500)
                {
                    return Task.FromResult(HealthCheckResult.Degraded($"Dung lượng ổ đĩa {driveInfo.Name} sắp hết ({freePercent:F1}% trống)", null, data));
                }

                return Task.FromResult(HealthCheckResult.Healthy($"Ổ đĩa {driveInfo.Name} dung lượng đảm bảo ({freePercent:F1}% trống - {freeMb / 1024:F1} GB)", data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Không thể truy xuất ổ đĩa trực tiếp, bỏ qua kiểm tra"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Degraded("Không thể kiểm tra dung lượng ổ đĩa: " + ex.Message, ex));
        }
    }
}
