using KPI.Domain.Common;
using KPI.Domain.Enums;

namespace KPI.Domain.Entities.Identity;

/// <summary>
/// 1. Tài khoản người dùng (users)
/// </summary>
public class User : BaseAuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public bool Is2FaEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int FailedAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

/// <summary>
/// 2. Vai trò người dùng (roles)
/// </summary>
public class Role : BaseEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// 3. Quyền hạn chi tiết (permissions)
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// 4. Bảng liên kết N-N Người dùng - Vai trò (user_roles)
/// </summary>
public class UserRole
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public long RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public long? AssignedBy { get; set; }
    public virtual User? AssignedByUser { get; set; }
}

/// <summary>
/// 5. Bảng liên kết N-N Vai trò - Quyền hạn (role_permissions)
/// </summary>
public class RolePermission
{
    public long RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public long PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = null!;
}

/// <summary>
/// 6. Nhật ký kiểm toán hệ thống (audit_logs)
/// </summary>
public class AuditLog : BaseEntity
{
    public long? UserId { get; set; }
    public virtual User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string? OldValues { get; set; } // JSON format
    public string? NewValues { get; set; } // JSON format
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 7. Tham số cấu hình hệ thống (system_configs)
/// </summary>
public class SystemConfig
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string DataType { get; set; } = "STRING";
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long? UpdatedBy { get; set; }
    public virtual User? UpdatedByUser { get; set; }
}

/// <summary>
/// 8. Quản lý phiên làm việc & Refresh Token (user_sessions)
/// </summary>
public class UserSession
{
    public string SessionId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
