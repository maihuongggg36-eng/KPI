using System.Security.Claims;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Web.ViewModels.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[Authorize]
public class CatalogController : Controller
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ICatalogService catalogService, ILogger<CatalogController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    #region 1. Trang Tổng quan Quản trị 18 Danh mục
    [HttpGet]
    public async Task<IActionResult> Index(string tab = "kpi-items", string? search = null)
    {
        var model = new CatalogIndexViewModel
        {
            ActiveTab = tab.ToLowerInvariant(),
            SearchQuery = search ?? "",
            KpiItems = await _catalogService.GetKpiMasterItemsAsync(search: search),
            Departments = await _catalogService.GetAllDepartmentsFlatAsync(),
            Positions = await _catalogService.GetPositionsAsync(),
            PositionRanks = await _catalogService.GetPositionRanksAsync(),
            Categories = await _catalogService.GetCategoriesAsync(),
            Units = await _catalogService.GetUnitsAsync(),
            Methods = await _catalogService.GetMethodsAsync(),
            Periods = await _catalogService.GetEvaluationPeriodsAsync(),
            Scales = await _catalogService.GetScoreScalesAsync(),
            Templates = await _catalogService.GetKpiTemplatesAsync(),
            Configs = await _catalogService.GetSystemConfigsAsync(),
            Notifications = await _catalogService.GetNotificationTemplatesAsync(),
            ActivityLogs = await _catalogService.GetCatalogActivityLogsAsync()
        };

        return View(model);
    }
    #endregion

    #region 2. Trang Chuyên biệt: Ngân hàng KPI Chuẩn (kpi_master_items)
    [HttpGet]
    public async Task<IActionResult> KpiBank(string perspective = "All", string sector = "All", string? search = null, string view = "grid")
    {
        var p = perspective == "All" ? null : perspective;
        var s = sector == "All" ? null : sector;

        var items = await _catalogService.GetKpiMasterItemsAsync(categoryPerspective: p, sector: s, search: search);
        var categories = await _catalogService.GetCategoriesAsync();
        var units = await _catalogService.GetUnitsAsync();
        var methods = await _catalogService.GetMethodsAsync();

        var model = new KpiBankViewModel
        {
            Items = items,
            Categories = categories,
            Units = units,
            Methods = methods,
            ActivePerspective = perspective,
            ActiveSector = sector,
            SearchQuery = search ?? "",
            ViewMode = view
        };

        return View(model);
    }
    #endregion

    #region 3. Trang Chuyên biệt: Sơ đồ Cây Phân cấp Phòng ban (Tree View)
    [HttpGet]
    public async Task<IActionResult> DepartmentTree()
    {
        var tree = await _catalogService.GetDepartmentTreeAsync();
        var flat = await _catalogService.GetAllDepartmentsFlatAsync();

        var model = new DepartmentTreeViewModel
        {
            Tree = tree,
            FlatList = flat,
            TotalDepartments = flat.Count,
            TotalStaff = flat.Sum(d => d.EmployeeCount)
        };

        return View(model);
    }
    #endregion

    #region 4. AJAX CRUD Endpoints
    // --- KPI MASTER ITEM ---
    [HttpPost]
    public async Task<IActionResult> CreateKpiItem([FromBody] CreateKpiMasterItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Json(new { success = false, message = "Mã và Tên chỉ tiêu không được để trống." });
        }

        try
        {
            var id = await _catalogService.CreateKpiMasterItemAsync(request, GetCurrentUserId());
            return Json(new { success = true, id, message = $"Đã thêm mới chỉ tiêu KPI '{request.Code}' thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi lưu: " + ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateKpiItem([FromBody] UpdateKpiMasterItemRequest request)
    {
        var result = await _catalogService.UpdateKpiMasterItemAsync(request, GetCurrentUserId());
        return Json(new { success = result, message = result ? "Cập nhật chỉ tiêu thành công!" : "Không tìm thấy chỉ tiêu." });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteKpiItem(long id)
    {
        var result = await _catalogService.DeleteKpiMasterItemAsync(id, GetCurrentUserId());
        return Json(new { success = result, message = result ? "Đã xóa chỉ tiêu khỏi ngân hàng KPI!" : "Không tìm thấy chỉ tiêu." });
    }

    [HttpPost]
    public async Task<IActionResult> CloneKpiItem([FromBody] CloneKpiMasterItemRequest request)
    {
        var id = await _catalogService.CloneKpiMasterItemAsync(request, GetCurrentUserId());
        return Json(new { success = id > 0, id, message = id > 0 ? $"Đã nhân bản chỉ tiêu mới: '{request.NewCode}'!" : "Không thể nhân bản." });
    }

    // --- DEPARTMENT ---
    [HttpPost]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Json(new { success = false, message = "Mã và Tên phòng ban không được để trống." });
        }

        var id = await _catalogService.CreateDepartmentAsync(request, GetCurrentUserId());
        return Json(new { success = true, id, message = $"Đã tạo mới phòng ban '{request.Name}'!" });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateDepartment([FromBody] UpdateDepartmentRequest request)
    {
        var result = await _catalogService.UpdateDepartmentAsync(request, GetCurrentUserId());
        return Json(new { success = result, message = result ? "Cập nhật phòng ban thành công!" : "Lỗi cập nhật." });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteDepartment(long id)
    {
        try
        {
            var result = await _catalogService.DeleteDepartmentAsync(id, GetCurrentUserId());
            return Json(new { success = result, message = result ? "Đã xóa phòng ban thành công!" : "Không tìm thấy phòng ban." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MoveDepartment([FromBody] MoveDepartmentRequest request)
    {
        var result = await _catalogService.MoveDepartmentAsync(request, GetCurrentUserId());
        return Json(new { success = result, message = result ? "Đã di chuyển vị trí phòng ban trong cây cơ cấu!" : "Không thể di chuyển." });
    }

    // --- POSITION & RANK ---
    [HttpPost]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionRequest request)
    {
        var id = await _catalogService.CreatePositionAsync(request, GetCurrentUserId());
        return Json(new { success = true, id, message = $"Đã tạo chức danh '{request.Name}'!" });
    }

    [HttpDelete]
    public async Task<IActionResult> DeletePosition(long id)
    {
        var result = await _catalogService.DeletePositionAsync(id, GetCurrentUserId());
        return Json(new { success = result, message = result ? "Đã xóa chức danh thành công!" : "Không tìm thấy." });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePositionRank([FromBody] CreatePositionRankRequest request)
    {
        var code = await _catalogService.CreatePositionRankAsync(request, GetCurrentUserId());
        return Json(new { success = true, code, message = $"Đã thêm ngạch công chức '{request.RankCode}'!" });
    }

    // --- PERIOD LIFECYCLE ---
    [HttpPost]
    public async Task<IActionResult> CreatePeriod([FromBody] CreateEvaluationPeriodRequest request)
    {
        var id = await _catalogService.CreateEvaluationPeriodAsync(request, GetCurrentUserId());
        return Json(new { success = true, id, message = $"Đã khởi tạo kỳ đánh giá '{request.Code}'!" });
    }

    [HttpPost]
    public async Task<IActionResult> ChangePeriodStatus([FromBody] ChangePeriodStatusRequest request)
    {
        var result = await _catalogService.ChangePeriodStatusAsync(request, GetCurrentUserId());
        return Json(new { success = result, message = result ? $"Đã chuyển trạng thái kỳ đánh giá sang {request.NewStatus}!" : "Lỗi chuyển trạng thái." });
    }

    // --- SYSTEM CONFIG ---
    [HttpPost]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateSystemConfigItemRequest request)
    {
        var result = await _catalogService.UpdateSystemConfigAsync(request, GetCurrentUserId());
        return Json(new { success = result, message = result ? "Đã cập nhật tham số hệ thống!" : "Lỗi cập nhật." });
    }

    // --- BULK ACTION ---
    [HttpPost]
    public async Task<IActionResult> BulkAction([FromBody] BulkActionRequest request)
    {
        var result = await _catalogService.ExecuteBulkActionAsync(request, GetCurrentUserId());
        return Json(new { success = result, message = result ? "Đã thực thi thao tác hàng loạt thành công!" : "Thao tác thất bại." });
    }

    // --- GLOBAL SEARCH ---
    [HttpGet]
    public async Task<IActionResult> GlobalSearch(string? q = null, string? keyword = null)
    {
        var term = q ?? keyword ?? string.Empty;
        var results = await _catalogService.SearchGlobalCatalogsAsync(term);
        return Json(new { success = true, results });
    }

    // --- ACTIVITY LOGS ---
    [HttpGet]
    public async Task<IActionResult> ActivityLogs(string? entity = null)
    {
        var logs = await _catalogService.GetCatalogActivityLogsAsync(entity);
        return Json(logs);
    }

    // --- EXPORT CSV ---
    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? type = null, string? catalogType = null)
    {
        var targetType = type ?? catalogType ?? "kpi-items";
        var bytes = await _catalogService.ExportCatalogCsvAsync(targetType);
        return File(bytes, "text/csv; charset=utf-8", $"{targetType}_catalog_{DateTime.Now:yyyyMMdd}.csv");
    }
    #endregion

    private long GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : 1;
    }
}
