using System.Security.Claims;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Web.ViewModels.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly ICatalogService _catalogService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportService reportService,
        ICatalogService catalogService,
        ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _catalogService = catalogService;
        _logger = logger;
    }

    #region 1. Trang Báo Cáo Tổng Hợp & Phân Tích Đa Chiều

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ReportFilterRequest? filter, [FromQuery] string? tab = "overview")
    {
        filter ??= new ReportFilterRequest();

        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        if (filter.PeriodId <= 0)
        {
            filter.PeriodId = periods.FirstOrDefault()?.Id ?? 1;
        }

        // Xử lý Quick Presets
        ApplyPreset(filter);

        var departments = await _catalogService.GetAllDepartmentsFlatAsync();
        var currentUserId = GetCurrentUserId();

        // Tải toàn bộ dữ liệu song song / tuần tự
        var summary = await _reportService.GetExecutiveSummaryAsync(filter);
        var leaderboard = await _reportService.GetLeaderboardAsync(filter);
        var gauss = await _reportService.GetGaussDistributionAsync(filter);
        var trends = await _reportService.GetTrendAnalysisAsync(filter.PeriodId);
        var dataViz = await _reportService.GetDataVizExtrasAsync(filter);
        var savedFilters = await _reportService.GetSavedFiltersAsync(currentUserId);

        var viewModel = new ReportIndexViewModel
        {
            Summary = summary,
            Leaderboard = leaderboard,
            Gauss = gauss,
            Trends = trends,
            DataViz = dataViz,
            Periods = periods,
            Departments = departments,
            Filter = filter,
            SavedFilters = savedFilters,
            CurrentTab = tab ?? "overview"
        };

        return View(viewModel);
    }

    #endregion

    #region 2. Bảng Xếp Hạng Hiệu Suất (Leaderboard Dedicated)

    [HttpGet]
    public async Task<IActionResult> Leaderboard([FromQuery] ReportFilterRequest? filter)
    {
        filter ??= new ReportFilterRequest();
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        if (filter.PeriodId <= 0) filter.PeriodId = periods.FirstOrDefault()?.Id ?? 1;

        var departments = await _catalogService.GetAllDepartmentsFlatAsync();
        var items = await _reportService.GetLeaderboardAsync(filter);

        var viewModel = new LeaderboardViewModel
        {
            Items = items,
            Periods = periods,
            Departments = departments,
            Filter = filter
        };

        return View(viewModel);
    }

    #endregion

    #region 3. Chuông Gauss & Kiểm soát Phân bổ (Gauss Dedicated)

    [HttpGet]
    public async Task<IActionResult> Gauss([FromQuery] ReportFilterRequest? filter)
    {
        filter ??= new ReportFilterRequest();
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        if (filter.PeriodId <= 0) filter.PeriodId = periods.FirstOrDefault()?.Id ?? 1;

        var departments = await _catalogService.GetAllDepartmentsFlatAsync();
        var gauss = await _reportService.GetGaussDistributionAsync(filter);
        var summary = await _reportService.GetExecutiveSummaryAsync(filter);

        var viewModel = new GaussAnalysisViewModel
        {
            Gauss = gauss,
            Summary = summary,
            Periods = periods,
            Departments = departments,
            Filter = filter
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> DrillDownBand(long periodId, string bandCode)
    {
        var employees = await _reportService.GetDrillDownEmployeesAsync(periodId, bandCode);
        return Json(new { success = true, data = employees });
    }

    #endregion

    #region 4. Export Engine (Excel ClosedXML, PDF QuestPDF, CSV)

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] ReportFilterRequest filter)
    {
        try
        {
            var bytes = await _reportService.ExportExcelAsync(filter);
            var fileName = $"BaoCao_HieuSuat_KPI_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất báo cáo Excel");
            return StatusCode(500, "Lỗi khi xuất báo cáo Excel: " + ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] ReportFilterRequest filter)
    {
        try
        {
            var bytes = await _reportService.ExportPdfAsync(filter);
            var fileName = $"BaoCao_HieuSuat_KPI_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất báo cáo PDF");
            return StatusCode(500, "Lỗi khi xuất báo cáo PDF: " + ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv([FromQuery] ReportFilterRequest filter)
    {
        try
        {
            var bytes = await _reportService.ExportCsvAsync(filter);
            var fileName = $"BaoCao_HieuSuat_KPI_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất CSV");
            return StatusCode(500, "Lỗi khi xuất CSV: " + ex.Message);
        }
    }

    #endregion

    #region 5. Lập Lịch Báo Cáo & Bộ Lọc Đã Lưu

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleReport([FromBody] ScheduledReportRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _reportService.ScheduleReportAsync(request, currentUserId);
            return Json(new 
            { 
                success = true, 
                message = $"Đã lập lịch gửi báo cáo tự động thành công tới {request.RecipientEmails} theo tần suất '{request.Frequency}'!" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đặt lịch gửi email báo cáo");
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFilter([FromBody] SavedFilterDto filter)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _reportService.SaveFilterAsync(filter, currentUserId);
            return Json(new { success = true, data = result, message = "Đã lưu bộ lọc thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Helper Methods

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && long.TryParse(claim.Value, out var id) ? id : 1;
    }

    private void ApplyPreset(ReportFilterRequest filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Preset)) return;

        switch (filter.Preset.ToLower())
        {
            case "this_quarter":
                filter.PeriodId = 1;
                filter.DepartmentIds = null;
                break;
            case "this_year":
                filter.PeriodId = 1;
                filter.DepartmentIds = null;
                break;
            case "my_dept":
                var deptClaim = User.FindFirst("DepartmentId")?.Value;
                if (long.TryParse(deptClaim, out var deptId) && deptId > 0)
                {
                    filter.DepartmentIds = new List<long> { deptId };
                }
                break;
        }
    }

    #endregion
}
