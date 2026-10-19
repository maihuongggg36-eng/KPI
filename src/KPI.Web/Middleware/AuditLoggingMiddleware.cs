using System.Security.Claims;
using KPI.Domain.Entities.Identity;
using KPI.Infrastructure.Data;

namespace KPI.Web.Middleware;

/// <summary>
/// Middleware ghi nhận nhật ký kiểm toán cho các HTTP Requests thay đổi trạng thái (POST / PUT / DELETE)
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, KpiDbContext dbContext)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;

        // Chỉ ghi nhận với các tác vụ POST / PUT / DELETE (state-changing requests)
        var shouldAudit = method is "POST" or "PUT" or "DELETE";

        if (shouldAudit && !path.Contains("/lib/") && !path.Contains("/css/") && !path.Contains("/js/"))
        {
            try
            {
                var userIdStr = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var userId = long.TryParse(userIdStr, out var id) ? id : (long?)null;
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var userAgent = context.Request.Headers.UserAgent.ToString();

                var audit = new AuditLog
                {
                    UserId = userId,
                    Action = $"HTTP_{method}",
                    EntityName = "HttpRequest",
                    EntityId = 0,
                    OldValues = null,
                    NewValues = $"{{\"path\":\"{path}\",\"query\":\"{context.Request.QueryString}\"}}",
                    IpAddress = ipAddress,
                    UserAgent = userAgent.Length > 255 ? userAgent[..255] : userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.AuditLogs.Add(audit);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể ghi AuditLog cho request {Method} {Path}: {Message}", method, path, ex.Message);
            }
        }

        await _next(context);
    }
}
