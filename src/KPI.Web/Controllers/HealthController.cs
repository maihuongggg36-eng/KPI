using KPI.Web.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace KPI.Web.Controllers;

/// <summary>
/// Quản lý Giám sát Sức khỏe Hệ thống (Health Check & Telemetry)
/// </summary>
public class HealthController : Controller
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Giao diện Dashboard Trực quan giám sát Sức khỏe Hệ thống
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        
        var proc = Process.GetCurrentProcess();
        ViewBag.ProcessStartTime = proc.StartTime;
        ViewBag.Uptime = DateTime.Now - proc.StartTime;
        ViewBag.MemoryUsedMb = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2);
        ViewBag.ThreadsCount = proc.Threads.Count;
        ViewBag.MachineName = Environment.MachineName;
        ViewBag.OsVersion = Environment.OSVersion.ToString();
        ViewBag.DotnetVersion = Environment.Version.ToString();

        return View(report);
    }

    /// <summary>
    /// JSON Endpoint: Trả về trạng thái chi tiết theo chuẩn REST
    /// </summary>
    [HttpGet("/health/status")]
    [Produces("application/json")]
    public async Task<IActionResult> GetStatus()
    {
        var sw = Stopwatch.StartNew();
        var report = await _healthCheckService.CheckHealthAsync();
        sw.Stop();

        var result = new
        {
            status = report.Status.ToString(),
            totalDurationMs = sw.ElapsedMilliseconds,
            timestamp = DateTime.UtcNow,
            system = new
            {
                machineName = Environment.MachineName,
                os = Environment.OSVersion.ToString(),
                runtime = Environment.Version.ToString(),
                uptime = (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")
            },
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
        };

        var statusCode = report.Status == HealthStatus.Healthy ? 200 : (report.Status == HealthStatus.Degraded ? 200 : 503);
        return StatusCode(statusCode, result);
    }
}
