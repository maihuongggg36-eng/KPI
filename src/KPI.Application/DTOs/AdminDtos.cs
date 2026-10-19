namespace KPI.Application.DTOs;

/// <summary>
/// DTO Danh sách và chi tiết Quản lý Người dùng dành cho ADMIN
/// </summary>
public class UserManagementDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public long? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public long? PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public int Age => DateOfBirth.HasValue ? DateTime.UtcNow.Year - DateOfBirth.Value.Year : 0;
    public bool IsLocked => Status == "Locked" || (LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow);
}

/// <summary>
/// DTO Yêu cầu chỉnh sửa thông tin cá nhân (Tên, tuổi, email, phòng ban, chức vụ, vai trò) do ADMIN thực hiện
/// </summary>
public class UpdateUserInfoDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public long DepartmentId { get; set; }
    public long PositionId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
}

/// <summary>
/// DTO Cấp lại mật khẩu cho User
/// </summary>
public class ResetPasswordDto
{
    public long UserId { get; set; }
    public string NewPassword { get; set; } = "123456";
}

/// <summary>
/// DTO Quản lý Menu động hệ thống
/// </summary>
public class MenuItemDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string? ParentTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public string? PermissionCode { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SectionTitle { get; set; }

    public List<MenuItemDto> Children { get; set; } = new();
}

/// <summary>
/// DTO Ma trận phân quyền Role - Permission
/// </summary>
public class RolePermissionMatrixDto
{
    public long RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> AssignedPermissionCodes { get; set; } = new();
}

/// <summary>
/// DTO Chi tiết quyền hạn
/// </summary>
public class PermissionItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAssigned { get; set; } = false;
}
