namespace KPI.Application.DTOs;

/// <summary>
/// DTO Thông tin Hồ sơ cá nhân & Cài đặt
/// </summary>
public class UserProfileDto
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    
    // Cài đặt giao diện
    public string Theme { get; set; } = "dark"; // light | dark | auto
    public string Language { get; set; } = "vi"; // vi | en
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public bool SoundEnabled { get; set; } = true;
    
    // Cài đặt thông báo
    public NotificationPreferencesDto NotificationSettings { get; set; } = new();
    
    // Lịch sử đăng nhập
    public List<LoginHistoryDto> LoginHistory { get; set; } = new();
}

/// <summary>
/// Tùy chọn bật/tắt nhận thông báo cho từng sự kiện
/// </summary>
public class NotificationPreferencesDto
{
    public bool NotifyKpiAssigned { get; set; } = true;
    public bool NotifySelfAssessment { get; set; } = true;
    public bool NotifyManagerReview { get; set; } = true;
    public bool NotifyDirectorApprove { get; set; } = true;
    public bool NotifyDeadlines { get; set; } = true;
    public bool NotifyQuotaWarning { get; set; } = true;
    public bool EmailNotification { get; set; } = true;
}

/// <summary>
/// Lịch sử đăng nhập từ AuditLog
/// </summary>
public class LoginHistoryDto
{
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public string Status { get; set; } = "Thành công";
}

/// <summary>
/// Yêu cầu đổi mật khẩu
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Cập nhật thông tin và cài đặt cá nhân
/// </summary>
public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Theme { get; set; } = "dark";
    public string Language { get; set; } = "vi";
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public bool SoundEnabled { get; set; } = true;
    public NotificationPreferencesDto NotificationSettings { get; set; } = new();
}
