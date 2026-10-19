using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KPI.Application.Common.Interfaces;
using KPI.Application.Common.Security;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Dịch vụ Xác thực người dùng, Quản lý Khóa tài khoản và Phiên làm việc
/// </summary>
public class AuthService : IAuthService
{
    private readonly KpiDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(KpiDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(string username, string password, string ipAddress, string? userAgent, bool rememberMe = false)
    {
        var normalizedUser = username.Trim().ToLower();
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUser || u.Email.ToLower() == normalizedUser);

        if (user == null)
        {
            _logger.LogWarning("Đăng nhập thất bại: Tài khoản '{Username}' không tồn tại. IP: {IP}", username, ipAddress);
            return new LoginResult { Success = false, ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác." };
        }

        // Kiểm tra khóa tài khoản (Account Lockout)
        if (user.Status == UserStatus.Locked)
        {
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                var remainingMinutes = Math.Ceiling((user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
                _logger.LogWarning("Tài khoản '{Username}' đang bị khóa đến {LockedUntil}. IP: {IP}", username, user.LockedUntil, ipAddress);
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = $"Tài khoản tạm thời bị khóa do nhập sai mật khẩu quá 5 lần. Vui lòng thử lại sau {remainingMinutes} phút."
                };
            }
            else
            {
                // Hết hạn khóa, tự động mở khóa
                user.Status = UserStatus.Active;
                user.FailedAttempts = 0;
                user.LockedUntil = null;
            }
        }

        if (user.Status != UserStatus.Active)
        {
            return new LoginResult { Success = false, ErrorMessage = $"Tài khoản của bạn đang ở trạng thái {user.Status}, không thể đăng nhập." };
        }

        // Kiểm tra mật khẩu (Hỗ trợ BCrypt và fallback SHA256)
        bool isPasswordValid = false;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch
        {
            // Fallback SHA256 nếu hash cũ
            isPasswordValid = VerifySha256(password, user.Salt, user.PasswordHash);
        }

        if (!isPasswordValid)
        {
            user.FailedAttempts++;
            _logger.LogWarning("Tài khoản '{Username}' nhập sai mật khẩu lần {Attempts}. IP: {IP}", username, user.FailedAttempts, ipAddress);

            // Ghi vết kiểm toán đăng nhập thất bại
            await LogAuditAsync(user.Id, "LOGIN_FAILED", ipAddress, userAgent);

            // Khóa tài khoản nếu nhập sai 5 lần
            if (user.FailedAttempts >= 5)
            {
                user.Status = UserStatus.Locked;
                user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                await _context.SaveChangesAsync();
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = "Bạn đã nhập sai mật khẩu 5 lần. Tài khoản bị tạm khóa 30 phút để bảo vệ an toàn."
                };
            }

            await _context.SaveChangesAsync();
            return new LoginResult
            {
                Success = false,
                ErrorMessage = $"Tên đăng nhập hoặc mật khẩu không chính xác. Còn {5 - user.FailedAttempts} lần thử."
            };
        }

        // Đăng nhập thành công: Reset số lần thử sai
        user.FailedAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        return await BuildSuccessResultAsync(user, ipAddress, userAgent);
    }

    public async Task<LoginResult> QuickSwitchRoleAsync(string usernameOrRole, string ipAddress, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(usernameOrRole))
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Vui lòng chọn hoặc nhập tên vai trò cần chuyển đổi."
            };
        }

        var targetUser = usernameOrRole.Trim().ToLower();

        // Map role name to standard test username if needed
        var mappedUsername = targetUser switch
        {
            "director" => "director",
            "manager" => "manager",
            "officer" or "officer1" or "employee" => "officer1",
            "hr" or "hr_admin" => "hr_admin",
            "accountant" => "accountant",
            "admin" => "admin",
            _ => targetUser
        };

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == mappedUsername);

        if (user == null)
        {
            return new LoginResult { Success = false, ErrorMessage = $"Không tìm thấy tài khoản thử nghiệm '{usernameOrRole}'." };
        }

        // Mở khóa nếu đang bị khóa trong chế độ switch nhanh
        user.Status = UserStatus.Active;
        user.FailedAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        _logger.LogInformation("Người dùng chuyển đổi nhanh sang vai trò '{Username}'. IP: {IP}", user.Username, ipAddress);
        return await BuildSuccessResultAsync(user, ipAddress, userAgent);
    }

    public async Task LogoutAsync(long userId, string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session != null)
            {
                session.IsRevoked = true;
            }
        }

        await LogAuditAsync(userId, "LOGOUT", "127.0.0.1", null);
        await _context.SaveChangesAsync();
    }

    private async Task<LoginResult> BuildSuccessResultAsync(User user, string ipAddress, string? userAgent)
    {
        // Lấy thông tin hồ sơ nhân sự
        var employee = await _context.EmployeeProfiles
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.UserId == user.Id);

        var primaryRole = user.UserRoles.FirstOrDefault()?.Role.Code ?? AppRoles.Officer;
        var fullName = employee?.FullName ?? user.Username;
        var deptName = employee?.Department?.Name ?? "Ban Giám đốc";
        var dataScope = AppRoles.GetDefaultDataScope(primaryRole);

        // Tạo phiên làm việc mới (Session)
        var sessionId = Guid.NewGuid().ToString("N");
        var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId + DateTime.UtcNow.Ticks)));

        var session = new UserSession
        {
            SessionId = sessionId,
            UserId = user.Id,
            RefreshTokenHash = refreshTokenHash,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        _context.UserSessions.Add(session);

        // Tạo Claims Identity
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, fullName),
            new(ClaimTypes.Role, primaryRole),
            new("DepartmentId", employee?.DepartmentId.ToString() ?? "0"),
            new("DepartmentName", deptName),
            new("PositionId", employee?.PositionId.ToString() ?? "0"),
            new("PositionName", employee?.Position?.Name ?? ""),
            new("StaffCode", employee?.StaffCode ?? ""),
            new("DataScope", dataScope.ToString()),
            new("SessionId", sessionId)
        };

        // Gán Permission claims: ADMIN có toàn quyền, các vai trò khác nhận quyền từ RolePermissions
        if (primaryRole == AppRoles.Admin)
        {
            foreach (var perm in AppPermissions.GetAllPermissions())
            {
                claims.Add(new Claim("Permission", perm.Code));
            }
        }
        else
        {
            var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
            var permissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToListAsync();

            foreach (var code in permissions)
            {
                claims.Add(new Claim("Permission", code));
            }
        }

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(claimsIdentity);

        // Ghi Audit Trail
        await LogAuditAsync(user.Id, "LOGIN_SUCCESS", ipAddress, userAgent);
        await _context.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            Principal = principal,
            UserId = user.Id,
            Username = user.Username,
            FullName = fullName,
            Role = primaryRole,
            DepartmentName = deptName,
            SessionId = sessionId
        };
    }

    private async Task LogAuditAsync(long? userId, string action, string ipAddress, string? userAgent)
    {
        var audit = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = "User",
            EntityId = userId ?? 0,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 255 ? userAgent[..255] : userAgent,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(audit);
    }

    private static bool VerifySha256(string password, string salt, string expectedHash)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash) == expectedHash;
    }
}
