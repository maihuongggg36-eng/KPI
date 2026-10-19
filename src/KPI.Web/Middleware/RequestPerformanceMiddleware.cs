using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KPI.Web.Middleware;

/// <summary>
/// Middleware giám sát hiệu năng thực thi Request (Performance Monitoring)
/// Gắn header X-Response-Time-Ms và ghi log cảnh báo khi truy vấn chậm (>500ms)
/// </summary>
public class RequestPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestPerformanceMiddleware> _logger;

    public RequestPerformanceMiddleware(RequestDelegate next, ILogger<RequestPerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        context.Response.OnStarting(state =>
        {
            var (httpCtx, stopwatch) = ((HttpContext, Stopwatch))state;
            if (!httpCtx.Response.HasStarted)
            {
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                httpCtx.Response.Headers["X-Response-Time-Ms"] = elapsedMs.ToString();
            }
            return Task.CompletedTask;
        }, (context, sw));

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            var path = context.Request.Path.Value ?? "";

            // Bỏ qua polling / hubs SignalR khỏi log cảnh báo
            if (!path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase) && elapsed > 500)
            {
                _logger.LogWarning("⚠️ [CẢNH BÁO HIỆU NĂNG] Truy vấn {Method} {Path} mất {Elapsed}ms (Trạng thái: {StatusCode})",
                    context.Request.Method,
                    path,
                    elapsed,
                    context.Response.StatusCode);
            }
        }
    }
}
