using System.Security.Claims;

namespace KPI.Application.Common.Interfaces;

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string username, string password, string ipAddress, string? userAgent, bool rememberMe = false);
    Task<LoginResult> QuickSwitchRoleAsync(string usernameOrRole, string ipAddress, string? userAgent);
    Task LogoutAsync(long userId, string? sessionId);
}
