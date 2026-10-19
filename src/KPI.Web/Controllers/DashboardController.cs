using System.Security.Claims;
using KPI.Application.Services;
using KPI.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? role = null)
    {
        var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "DIRECTOR";
        var userFullName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Đồng chí";
        var username = User.Identity?.Name ?? "director";

        // Tự động chọn view vai trò tương ứng hoặc nhận từ query string
        var activeRole = "Director";
        if (!string.IsNullOrWhiteSpace(role))
        {
            activeRole = role.Trim();
        }
        else
        {
            activeRole = userRoleClaim.ToUpperInvariant() switch
            {
                "DIRECTOR" => "Director",
                "ADMIN" => "Director",
                "MANAGER" => "Manager",
                "OFFICER" => "Employee",
                "ACCOUNTANT" => "Accountant",
                "HR_ADMIN" => "Hr",
                _ => "Director"
            };
        }

        var currentUserId = GetCurrentUserId();

        // Nạp trước dữ liệu đầy đủ cho cả 5 vai trò để switch tab tức thì
        var directorData = await _dashboardService.GetDirectorDashboardDataAsync();
        var managerData = await _dashboardService.GetManagerDashboardDataAsync();
        var employeeData = await _dashboardService.GetEmployeeDashboardDataAsync(currentUserId);
        var accountantData = await _dashboardService.GetAccountantDashboardDataAsync();
        var hrData = await _dashboardService.GetHrDashboardDataAsync();

        var model = new DashboardIndexViewModel
        {
            ActiveRole = activeRole,
            UserFullName = userFullName,
            CurrentUsername = username,
            PrimaryRole = userRoleClaim,
            CanSwitchRole = true,
            DirectorData = directorData,
            ManagerData = managerData,
            EmployeeData = employeeData,
            AccountantData = accountantData,
            HrData = hrData
        };

        return View(model);
    }

    #region AJAX Data Endpoints (Hỗ trợ Auto-Refresh 60s & Partial Fetching)
    [HttpGet]
    public async Task<IActionResult> GetDirectorData()
    {
        var data = await _dashboardService.GetDirectorDashboardDataAsync();
        return Json(new { success = true, data, timestamp = DateTime.Now.ToString("HH:mm:ss") });
    }

    [HttpGet]
    public async Task<IActionResult> GetManagerData()
    {
        var data = await _dashboardService.GetManagerDashboardDataAsync();
        return Json(new { success = true, data, timestamp = DateTime.Now.ToString("HH:mm:ss") });
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployeeData()
    {
        var userId = GetCurrentUserId();
        var data = await _dashboardService.GetEmployeeDashboardDataAsync(userId);
        return Json(new { success = true, data, timestamp = DateTime.Now.ToString("HH:mm:ss") });
    }

    [HttpGet]
    public async Task<IActionResult> GetAccountantData()
    {
        var data = await _dashboardService.GetAccountantDashboardDataAsync();
        return Json(new { success = true, data, timestamp = DateTime.Now.ToString("HH:mm:ss") });
    }

    [HttpGet]
    public async Task<IActionResult> GetHrData()
    {
        var data = await _dashboardService.GetHrDashboardDataAsync();
        return Json(new { success = true, data, timestamp = DateTime.Now.ToString("HH:mm:ss") });
    }
    #endregion

    #region Quick Actions
    [HttpPost]
    public async Task<IActionResult> QuickApprove([FromBody] QuickApproveRequest? request)
    {
        if (request == null)
        {
            return Json(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        var userId = GetCurrentUserId();
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? "Lãnh đạo đã phê chuẩn kết quả đánh giá" : request.Comment;
        var result = await _dashboardService.QuickApproveEvaluationAsync(request.EvaluationId, userId, comment);

        if (result)
        {
            return Json(new { success = true, message = $"Đã phê chuẩn thành công hồ sơ đánh giá #{request.EvaluationId}!" });
        }
        return Json(new { success = false, message = "Không thể phê chuẩn hồ sơ. Vui lòng thử lại." });
    }

    [HttpPost]
    public async Task<IActionResult> QuickDisburse([FromBody] QuickDisburseRequest? request)
    {
        if (request == null)
        {
            return Json(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        var userId = GetCurrentUserId();
        var result = await _dashboardService.QuickDisburseCashVoucherAsync(request.VoucherId, userId);

        if (result)
        {
            return Json(new { success = true, message = $"Đã xác nhận chi tiền mặt tại quỹ cho phiếu chi #{request.VoucherId}!" });
        }
        return Json(new { success = false, message = "Không thể chi trả phiếu chi. Vui lòng thử lại." });
    }
    #endregion

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdClaim, out var id))
        {
            return id;
        }
        return 1; // Default Admin/Director
    }
}
