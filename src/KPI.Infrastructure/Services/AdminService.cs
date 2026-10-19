using KPI.Application.Common.Security;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Dịch vụ Quản trị Hệ thống tập trung — Dành riêng cho Quản trị viên (ADMIN)
/// </summary>
public class AdminService : IAdminService
{
    private readonly KpiDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(KpiDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region 1. Quản lý Người dùng

    public async Task<List<UserManagementDto>> GetUsersListAsync(string? searchTerm = null, long? departmentId = null)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        var users = await query.ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();

        var employees = await _context.EmployeeProfiles
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => userIds.Contains(e.UserId))
            .AsNoTracking()
            .ToDictionaryAsync(e => e.UserId);

        var result = new List<UserManagementDto>();
        foreach (var u in users)
        {
            employees.TryGetValue(u.Id, out var emp);

            if (departmentId.HasValue && emp?.DepartmentId != departmentId.Value)
            {
                continue;
            }

            var primaryRole = u.UserRoles.FirstOrDefault()?.Role;

            result.Add(new UserManagementDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = emp?.FullName ?? u.Username,
                StaffCode = emp?.StaffCode ?? $"CB{u.Id:D3}",
                Phone = emp?.Phone,
                DateOfBirth = emp?.DateOfBirth,
                DepartmentId = emp?.DepartmentId,
                DepartmentName = emp?.Department?.Name ?? "Chưa phân bổ",
                PositionId = emp?.PositionId,
                PositionName = emp?.Position?.Name ?? "Chưa bổ nhiệm",
                RoleCode = primaryRole?.Code ?? "OFFICER",
                RoleName = primaryRole?.Name ?? "Cán bộ nhân viên",
                Status = u.Status.ToString(),
                LockedUntil = u.LockedUntil,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            });
        }

        return result.OrderBy(x => x.Id).ToList();
    }

    public async Task<UserManagementDto?> GetUserDetailAsync(long userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        var emp = await _context.EmployeeProfiles
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.UserId == user.Id);

        var primaryRole = user.UserRoles.FirstOrDefault()?.Role;

        return new UserManagementDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = emp?.FullName ?? user.Username,
            StaffCode = emp?.StaffCode ?? $"CB{user.Id:D3}",
            Phone = emp?.Phone,
            DateOfBirth = emp?.DateOfBirth,
            DepartmentId = emp?.DepartmentId,
            DepartmentName = emp?.Department?.Name ?? "",
            PositionId = emp?.PositionId,
            PositionName = emp?.Position?.Name ?? "",
            RoleCode = primaryRole?.Code ?? "OFFICER",
            RoleName = primaryRole?.Name ?? "",
            Status = user.Status.ToString(),
            LockedUntil = user.LockedUntil,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UpdateUserAsync(UpdateUserInfoDto dto, long currentAdminId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId);

        if (user == null) return false;

        var oldEmail = user.Email;
        user.Email = dto.Email.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var emp = await _context.EmployeeProfiles.FirstOrDefaultAsync(e => e.UserId == user.Id);
        if (emp != null)
        {
            emp.FullName = dto.FullName.Trim();
            emp.Phone = dto.Phone?.Trim();
            emp.DateOfBirth = dto.DateOfBirth;
            if (dto.DepartmentId > 0) emp.DepartmentId = dto.DepartmentId;
            if (dto.PositionId > 0) emp.PositionId = dto.PositionId;
            emp.UpdatedAt = DateTime.UtcNow;
        }

        // Cập nhật Role nếu có thay đổi
        if (!string.IsNullOrEmpty(dto.RoleCode))
        {
            var targetRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == dto.RoleCode);
            if (targetRole != null)
            {
                var existingRole = user.UserRoles.FirstOrDefault();
                if (existingRole == null)
                {
                    user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = targetRole.Id, AssignedBy = currentAdminId });
                }
                else if (existingRole.RoleId != targetRole.Id)
                {
                    _context.UserRoles.Remove(existingRole);
                    user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = targetRole.Id, AssignedBy = currentAdminId });
                }
            }
        }

        // Ghi vết kiểm toán
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentAdminId,
            Action = "UPDATE_USER_INFO",
            EntityName = "User",
            EntityId = user.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(dto),
            IpAddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin {AdminId} đã cập nhật thông tin User {UserId} ({FullName})", currentAdminId, user.Id, dto.FullName);
        return true;
    }

    public async Task<bool> ResetUserPasswordAsync(long userId, string newPassword, long currentAdminId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.FailedAttempts = 0;
        user.LockedUntil = null;
        user.Status = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        // Ghi Audit Log
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentAdminId,
            Action = "RESET_PASSWORD",
            EntityName = "User",
            EntityId = user.Id,
            NewValues = "Mật khẩu đã được cấp lại",
            IpAddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin {AdminId} đã cấp lại mật khẩu thành công cho User {UserId}", currentAdminId, user.Id);
        return true;
    }

    public async Task<bool> ToggleUserLockAsync(long userId, long currentAdminId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        if (user.Status == UserStatus.Locked)
        {
            user.Status = UserStatus.Active;
            user.LockedUntil = null;
            user.FailedAttempts = 0;
        }
        else
        {
            user.Status = UserStatus.Locked;
            user.LockedUntil = DateTime.UtcNow.AddYears(10); // Khóa vô thời hạn
        }

        user.UpdatedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentAdminId,
            Action = "TOGGLE_USER_LOCK",
            EntityName = "User",
            EntityId = user.Id,
            NewValues = $"Status: {user.Status}",
            IpAddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region 2. Quản lý Menu động

    public async Task<List<MenuItemDto>> GetAllMenuItemsAsync()
    {
        var items = await _context.MenuItems
            .Include(m => m.Parent)
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        return items.Select(m => new MenuItemDto
        {
            Id = m.Id,
            ParentId = m.ParentId,
            ParentTitle = m.Parent?.Title,
            Title = m.Title,
            Icon = m.Icon,
            Url = m.Url,
            PermissionCode = m.PermissionCode,
            SortOrder = m.SortOrder,
            IsActive = m.IsActive,
            SectionTitle = m.SectionTitle
        }).ToList();
    }

    public async Task<bool> SaveMenuItemAsync(MenuItemDto dto, long currentAdminId)
    {
        if (dto.Id > 0)
        {
            var menu = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == dto.Id && !m.IsDeleted);
            if (menu == null) return false;

            menu.Title = dto.Title.Trim();
            menu.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "bi-circle" : dto.Icon.Trim();
            menu.Url = string.IsNullOrWhiteSpace(dto.Url) ? "#" : dto.Url.Trim();
            menu.PermissionCode = string.IsNullOrWhiteSpace(dto.PermissionCode) ? null : dto.PermissionCode.Trim();
            menu.SortOrder = dto.SortOrder;
            menu.IsActive = dto.IsActive;
            menu.ParentId = dto.ParentId > 0 ? dto.ParentId : null;
            menu.SectionTitle = string.IsNullOrWhiteSpace(dto.SectionTitle) ? "Chức năng khác" : dto.SectionTitle.Trim();
        }
        else
        {
            var menu = new MenuItem
            {
                Title = dto.Title.Trim(),
                Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "bi-circle" : dto.Icon.Trim(),
                Url = string.IsNullOrWhiteSpace(dto.Url) ? "#" : dto.Url.Trim(),
                PermissionCode = string.IsNullOrWhiteSpace(dto.PermissionCode) ? null : dto.PermissionCode.Trim(),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                IsDeleted = false,
                ParentId = dto.ParentId > 0 ? dto.ParentId : null,
                SectionTitle = string.IsNullOrWhiteSpace(dto.SectionTitle) ? "Chức năng khác" : dto.SectionTitle.Trim()
            };
            _context.MenuItems.Add(menu);
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentAdminId,
            Action = dto.Id > 0 ? "UPDATE_MENU_ITEM" : "CREATE_MENU_ITEM",
            EntityName = "MenuItem",
            EntityId = dto.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(dto),
            IpAddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMenuItemAsync(long menuItemId, long currentAdminId)
    {
        var menu = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId && !m.IsDeleted);
        if (menu == null) return false;

        menu.IsDeleted = true;
        menu.IsActive = false;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentAdminId,
            Action = "DELETE_MENU_ITEM",
            EntityName = "MenuItem",
            EntityId = menuItemId,
            NewValues = "Deleted",
            IpAddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleMenuItemActiveAsync(long menuItemId, long currentAdminId)
    {
        var menu = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId && !m.IsDeleted);
        if (menu == null) return false;

        menu.IsActive = !menu.IsActive;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentAdminId,
            Action = "TOGGLE_MENU_ITEM_ACTIVE",
            EntityName = "MenuItem",
            EntityId = menuItemId,
            NewValues = $"IsActive: {menu.IsActive}",
            IpAddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region 3. Quản trị Phân quyền Role - Permission

    public async Task<List<RolePermissionMatrixDto>> GetRolePermissionsMatrixAsync()
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Id)
            .AsNoTracking()
            .ToListAsync();

        return roles.Select(r => new RolePermissionMatrixDto
        {
            RoleId = r.Id,
            RoleCode = r.Code,
            RoleName = r.Name,
            Description = r.Description,
            AssignedPermissionCodes = r.RolePermissions.Select(rp => rp.Permission.Code).ToList()
        }).ToList();
    }

    public async Task<bool> UpdateRolePermissionsAsync(long roleId, List<string> permissionCodes, long currentAdminId)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null) return false;

        // Xóa các quyền cũ
        _context.RolePermissions.RemoveRange(role.RolePermissions);

        // Lấy danh sách ID tương ứng với các mã quyền hạn
        var permissions = await _context.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .ToListAsync();

        foreach (var perm in permissions)
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = perm.Id
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentAdminId,
            Action = "UPDATE_ROLE_PERMISSIONS",
            EntityName = "Role",
            EntityId = roleId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(permissionCodes),
            IpAddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin {AdminId} đã cập nhật {Count} quyền cho Vai trò {RoleCode}", currentAdminId, permissions.Count, role.Code);
        return true;
    }

    #endregion
}
