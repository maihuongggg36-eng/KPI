using System.Security.Claims;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Web.ViewModels.CivilServant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[Authorize]
public class CivilServantController : Controller
{
    private readonly IDecree90Service _decree90Service;
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CivilServantController> _logger;

    public CivilServantController(
        IDecree90Service decree90Service,
        ICatalogService catalogService,
        ILogger<CivilServantController> logger)
    {
        _decree90Service = decree90Service;
        _catalogService = catalogService;
        _logger = logger;
    }

    #region 1. Bảng Theo Dõi Cán Bộ Công Chức & Gauge Meter (BR-06)

    [HttpGet]
    public async Task<IActionResult> Index(
        long? periodId, 
        long? departmentId, 
        string? rankCode, 
        string? classification)
    {
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var targetPeriodId = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var civilServants = await _decree90Service.GetCivilServantOverviewListAsync(
            targetPeriodId, departmentId, rankCode, classification);

        var quotaStatus = await _decree90Service.CheckExcellenceCapAsync(targetPeriodId);
        var departments = await _catalogService.GetAllDepartmentsFlatAsync();
        var positionRanks = await _catalogService.GetPositionRanksAsync();

        var viewModel = new CivilServantIndexViewModel
        {
            CivilServants = civilServants,
            QuotaStatus = quotaStatus,
            SelectedPeriodId = targetPeriodId,
            SelectedDepartmentId = departmentId,
            SelectedRankCode = rankCode,
            SelectedClassification = classification,
            Periods = periods,
            Departments = departments,
            PositionRanks = positionRanks
        };

        return View(viewModel);
    }

    #endregion

    #region 2. Biểu Mẫu Quốc Gia Mẫu 01 / Mẫu 02 (A4 Print-Ready)

    [HttpGet]
    public async Task<IActionResult> Form01_02(long employeeId, long? periodId)
    {
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var targetPeriodId = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var formData = await _decree90Service.GetForm0102Async(employeeId, targetPeriodId);
        if (formData == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ cán bộ công chức cho kỳ đánh giá này.";
            return RedirectToAction(nameof(Index));
        }

        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Officer";
        bool canEdit = role.Equals("Director", StringComparison.OrdinalIgnoreCase) || 
                       role.Equals("Hr", StringComparison.OrdinalIgnoreCase) || 
                       role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        var viewModel = new Form01_02ViewModel
        {
            Form = formData,
            IsReadOnly = !canEdit,
            UserRole = role
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveForm01_02([FromBody] SaveForm01_02Request request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _decree90Service.SaveForm0102Async(request, currentUserId);
            return Json(new { success = true, message = "Đã lưu kết quả phân loại cán bộ công chức theo Nghị định 90 thành công!" });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, code = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu biểu mẫu Mẫu 01/02");
            return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PrintForm(long employeeId, long? periodId)
    {
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var targetPeriodId = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var formData = await _decree90Service.GetForm0102Async(employeeId, targetPeriodId);
        if (formData == null) return NotFound();

        return View("PrintA4", formData);
    }

    #endregion

    #region 3. Dashboard Công Chức & Phân Bổ Gauss

    [HttpGet]
    public async Task<IActionResult> Dashboard(long? periodId)
    {
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var targetPeriodId = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var dashboardData = await _decree90Service.GetCivilServantDashboardAsync(targetPeriodId);

        var viewModel = new CivilServantDashboardViewModel
        {
            Dashboard = dashboardData,
            SelectedPeriodId = targetPeriodId,
            Periods = periods
        };

        return View(viewModel);
    }

    #endregion

    #region Helpers

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(claim, out var id)) return id;
        return 1;
    }

    #endregion
}
