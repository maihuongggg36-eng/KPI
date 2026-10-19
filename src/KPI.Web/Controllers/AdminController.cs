using System.Security.Claims;
using KPI.Application.Common.Security;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KPI.Web.Controllers;

/// <summary>
/// Controller Quản trị Hệ thống tập trung (CHỈ DÀNH CHO ADMIN)
/// - Quản lý Người dùng: Chỉnh sửa tên, tuổi, phòng ban, chức vụ, vai trò
/// - Cấp lại mật khẩu (Reset password) cho bất kỳ User nào
/// - Quản lý Menu động: Thêm, sửa, xóa, sắp xếp và bật/tắt các đầu mục menu
/// - Phân quyền hệ thống: Cấp và thu hồi quyền cho từng vai trò và user
/// - Xem Nhật ký kiểm toán toàn hệ thống
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly KpiDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, KpiDbContext context, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _context = context;
        _logger = logger;
    }

    private long GetCurrentAdminId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : 1;
    }

    #region 1. Quản lý Người dùng

    [HttpGet]
    public async Task<IActionResult> Users(string? searchTerm = null, long? departmentId = null)
    {
        ViewBag.Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        ViewBag.Positions = await _context.Positions.OrderBy(p => p.Name).ToListAsync();
        ViewBag.Roles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();
        ViewBag.SearchTerm = searchTerm;
        ViewBag.DepartmentId = departmentId;

        var users = await _adminService.GetUsersListAsync(searchTerm, departmentId);
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UpdateUserInfoDto model)
    {
        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Email))
        {
            TempData["ErrorMessage"] = "Họ tên và Email không được để trống.";
            return RedirectToAction(nameof(Users));
        }

        var result = await _adminService.UpdateUserAsync(model, GetCurrentAdminId());
        if (result)
        {
            TempData["SuccessMessage"] = $"Đã cập nhật thành công thông tin người dùng '{model.FullName}'!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng cần cập nhật.";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(long userId, string? newPassword)
    {
        var pwd = string.IsNullOrWhiteSpace(newPassword) ? "123456" : newPassword.Trim();
        var result = await _adminService.ResetUserPasswordAsync(userId, pwd, GetCurrentAdminId());

        if (result)
        {
            TempData["SuccessMessage"] = $"Đã cấp lại mật khẩu thành công cho tài khoản! Mật khẩu mới: {pwd}";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể cấp lại mật khẩu cho người dùng này.";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(long userId)
    {
        var result = await _adminService.ToggleUserLockAsync(userId, GetCurrentAdminId());
        if (result)
        {
            TempData["SuccessMessage"] = "Đã thay đổi trạng thái khóa tài khoản thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể thay đổi trạng thái tài khoản.";
        }

        return RedirectToAction(nameof(Users));
    }

    #endregion

    #region 2. Quản lý Menu động

    [HttpGet]
    public async Task<IActionResult> Menus()
    {
        var allMenus = await _adminService.GetAllMenuItemsAsync();
        ViewBag.Permissions = AppPermissions.GetAllPermissions();
        return View(allMenus);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMenu(MenuItemDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            TempData["ErrorMessage"] = "Tên hiển thị menu không được để trống.";
            return RedirectToAction(nameof(Menus));
        }

        var result = await _adminService.SaveMenuItemAsync(model, GetCurrentAdminId());
        if (result)
        {
            TempData["SuccessMessage"] = model.Id > 0 ? "Đã cập nhật menu thành công!" : "Đã tạo mới menu thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể lưu thông tin menu.";
        }

        return RedirectToAction(nameof(Menus));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMenu(long id)
    {
        var result = await _adminService.DeleteMenuItemAsync(id, GetCurrentAdminId());
        if (result)
        {
            TempData["SuccessMessage"] = "Đã xóa menu thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể xóa menu này.";
        }

        return RedirectToAction(nameof(Menus));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleMenu(long id)
    {
        var result = await _adminService.ToggleMenuItemActiveAsync(id, GetCurrentAdminId());
        if (result)
        {
            TempData["SuccessMessage"] = "Đã thay đổi trạng thái kích hoạt menu thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể thay đổi trạng thái menu này.";
        }

        return RedirectToAction(nameof(Menus));
    }

    #endregion

    #region 3. Phân quyền Hệ thống

    [HttpGet]
    public async Task<IActionResult> Permissions()
    {
        var roles = await _adminService.GetRolePermissionsMatrixAsync();
        var allPermissions = AppPermissions.GetAllPermissions();
        ViewBag.AllPermissions = allPermissions;
        return View(roles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRolePermissions(long roleId, List<string>? permissionCodes)
    {
        var codes = permissionCodes ?? new List<string>();
        var result = await _adminService.UpdateRolePermissionsAsync(roleId, codes, GetCurrentAdminId());

        if (result)
        {
            TempData["SuccessMessage"] = "Đã lưu và cập nhật quyền hạn cho vai trò thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể cập nhật quyền hạn cho vai trò.";
        }

        return RedirectToAction(nameof(Permissions));
    }

    #endregion

    #region 4. Nhật ký Kiểm toán

    [HttpGet]
    public async Task<IActionResult> AuditLog(int page = 1)
    {
        int pageSize = 50;
        var query = _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.TotalLogs = total;

        return View(logs);
    }

    #endregion
}
