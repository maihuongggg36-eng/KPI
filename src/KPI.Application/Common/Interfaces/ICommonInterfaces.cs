namespace KPI.Application.Common.Interfaces;

/// <summary>
/// Giao diện lấy thông tin tài khoản đang đăng nhập trong ngữ cảnh HTTP Request
/// </summary>
public interface ICurrentUserService
{
    long? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

/// <summary>
/// Giao diện thời gian hệ thống để dễ dàng Unit Test
/// </summary>
public interface IDateTime
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
