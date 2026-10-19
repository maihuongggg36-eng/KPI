using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ Quản lý Hồ sơ Cá nhân, Đổi mật khẩu & Tùy chọn Hệ thống
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Lấy toàn bộ thông tin cá nhân, cài đặt giao diện/thông báo và lịch sử đăng nhập
    /// </summary>
    Task<UserProfileDto> GetUserProfileAsync(long userId);

    /// <summary>
    /// Đổi mật khẩu tài khoản
    /// </summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(long userId, ChangePasswordRequest request);

    /// <summary>
    /// Cập nhật thông tin liên hệ và các tùy chọn giao diện / thông báo
    /// </summary>
    Task<bool> UpdateProfileAsync(long userId, UpdateProfileRequest request);

    /// <summary>
    /// Cập nhật đường dẫn ảnh đại diện
    /// </summary>
    Task<bool> UpdateAvatarAsync(long userId, string avatarUrl);
}
