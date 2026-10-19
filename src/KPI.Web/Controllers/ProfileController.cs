using System.Security.Claims;
using KPI.Application.DTOs;
using KPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

/// <summary>
/// Controller Quản lý Hồ sơ cá nhân, Đổi mật khẩu, Tùy chọn giao diện & Thông báo
/// </summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        IWebHostEnvironment env,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _env = env;
        _logger = logger;
    }

    private long GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : 1;
    }

    /// <summary>
    /// Giao diện Hồ sơ & Cài đặt cá nhân
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var profile = await _profileService.GetUserProfileAsync(userId);
        return View(profile);
    }

    /// <summary>
    /// Cập nhật thông tin liên hệ và tùy chọn hệ thống
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ok = await _profileService.UpdateProfileAsync(userId, request);
            if (ok)
            {
                TempData["SuccessMessage"] = "Đã cập nhật thông tin và cài đặt cá nhân thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi cập nhật hồ sơ");
            TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Đổi mật khẩu tài khoản
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (success, message) = await _profileService.ChangePasswordAsync(userId, request);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi đổi mật khẩu");
            TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Upload và cập nhật ảnh đại diện
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? avatarFile)
    {
        try
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tập tin ảnh đại diện!";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra kích thước tối đa 5MB
            if (avatarFile.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Dung lượng ảnh tối đa là 5MB!";
                return RedirectToAction(nameof(Index));
            }

            var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowedExts.Contains(ext))
            {
                TempData["ErrorMessage"] = "Định dạng ảnh không hợp lệ (hỗ trợ JPG, PNG, WEBP)!";
                return RedirectToAction(nameof(Index));
            }

            var userId = GetCurrentUserId();
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var fileName = $"avatar_user_{userId}_{DateTime.UtcNow.Ticks}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            var avatarUrl = $"/uploads/avatars/{fileName}";
            await _profileService.UpdateAvatarAsync(userId, avatarUrl);

            TempData["SuccessMessage"] = "Tải lên ảnh đại diện thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tải lên avatar");
            TempData["ErrorMessage"] = "Lỗi tải lên ảnh: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Chọn mẫu Avatar có sẵn
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPresetAvatar([FromForm] string avatarPreset)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _profileService.UpdateAvatarAsync(userId, avatarPreset);
            TempData["SuccessMessage"] = "Đã áp dụng ảnh đại diện mới!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
