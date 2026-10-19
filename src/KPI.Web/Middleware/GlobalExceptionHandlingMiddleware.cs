using System.Net;
using System.Text.Json;
using KPI.Application.Common.Exceptions;

namespace KPI.Web.Middleware;

/// <summary>
/// Middleware xử lý ngoại lệ toàn cục (Global Exception Handling Middleware)
/// Bắt các vi phạm Business Rule (BR-01 -> BR-20), Validation và lỗi hệ thống
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var isApiOrAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
            || context.Request.Path.StartsWithSegments("/api")
            || context.Request.Headers.Accept.Any(a => a?.Contains("application/json") == true);

        HttpStatusCode statusCode;
        object responseBody;

        switch (exception)
        {
            case BusinessRuleException brEx:
                statusCode = HttpStatusCode.BadRequest;
                _logger.LogWarning("Vi phạm Quy tắc nghiệp vụ [{RuleCode}]: {Message}", brEx.RuleCode, brEx.Message);
                responseBody = new
                {
                    success = false,
                    errorType = "BusinessRuleViolation",
                    ruleCode = brEx.RuleCode,
                    message = brEx.Message
                };
                break;

            case ValidationException valEx:
                statusCode = HttpStatusCode.BadRequest;
                _logger.LogWarning("Dữ liệu đầu vào không hợp lệ: {Errors}", JsonSerializer.Serialize(valEx.Errors));
                responseBody = new
                {
                    success = false,
                    errorType = "ValidationError",
                    message = valEx.Message,
                    errors = valEx.Errors
                };
                break;

            case NotFoundException nfEx:
                statusCode = HttpStatusCode.NotFound;
                _logger.LogWarning("Không tìm thấy dữ liệu: {Message}", nfEx.Message);
                responseBody = new
                {
                    success = false,
                    errorType = "NotFound",
                    message = nfEx.Message
                };
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                _logger.LogError(exception, "Lỗi máy chủ nội bộ chưa được xử lý tại {Path}", context.Request.Path);
                responseBody = new
                {
                    success = false,
                    errorType = "ServerError",
                    message = "Đã xảy ra sự cố nội bộ trong quá trình xử lý yêu cầu. Vui lòng liên hệ quản trị viên."
                };
                break;
        }

        if (isApiOrAjax)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody));
        }
        else
        {
            // Đối với request web MVC thông thường
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
