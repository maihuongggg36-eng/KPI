using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ Quản trị Hệ thống tập trung (Dành riêng cho ADMIN):
/// - Quản lý Người dùng: Chỉnh sửa tên, tuổi, phòng ban, chức vụ, vai trò
/// - Cấp lại mật khẩu (Reset password) cho bất kỳ tài khoản nào
/// - Quản lý Menu động: Thêm, sửa, xóa, sắp xếp và bật tắt menu
/// - Phân quyền hệ thống: Cấp và thu hồi quyền hạn cho các vai trò và user
/// </summary>
public interface IAdminService
{
    // Quản lý người dùng
    Task<List<UserManagementDto>> GetUsersListAsync(string? searchTerm = null, long? departmentId = null);
    Task<UserManagementDto?> GetUserDetailAsync(long userId);
    Task<bool> UpdateUserAsync(UpdateUserInfoDto dto, long currentAdminId);
    Task<bool> ResetUserPasswordAsync(long userId, string newPassword, long currentAdminId);
    Task<bool> ToggleUserLockAsync(long userId, long currentAdminId);

    // Quản lý Menu động
    Task<List<MenuItemDto>> GetAllMenuItemsAsync();
    Task<bool> SaveMenuItemAsync(MenuItemDto dto, long currentAdminId);
    Task<bool> DeleteMenuItemAsync(long menuItemId, long currentAdminId);
    Task<bool> ToggleMenuItemActiveAsync(long menuItemId, long currentAdminId);

    // Quản trị Phân quyền
    Task<List<RolePermissionMatrixDto>> GetRolePermissionsMatrixAsync();
    Task<bool> UpdateRolePermissionsAsync(long roleId, List<string> permissionCodes, long currentAdminId);
}
