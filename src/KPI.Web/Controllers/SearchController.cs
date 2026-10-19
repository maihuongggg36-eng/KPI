using System.Security.Claims;
using KPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

/// <summary>
/// Controller xử lý tìm kiếm nhanh cho Command Palette (Ctrl+K)
/// </summary>
[Authorize]
public class SearchController : Controller
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> QuickSearch([FromQuery] string? q)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        long userId = long.TryParse(idStr, out var id) ? id : 1;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "GUEST";

        var result = await _searchService.QuickSearchAsync(q ?? string.Empty, userId, role);
        return Json(new { success = true, data = result });
    }
}
