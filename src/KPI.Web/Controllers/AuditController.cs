using KPI.Domain.Entities.Identity;
using KPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KPI.Web.Controllers;

[Authorize(Roles = "ADMIN,DIRECTOR")]
public class AuditController : Controller
{
    private readonly KpiDbContext _context;

    public AuditController(KpiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? actionType, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        const int pageSize = 15;
        if (page < 1) page = 1;

        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        // Lọc theo từ khóa tìm kiếm
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(a => a.EntityName.Contains(s) || 
                                     a.IpAddress.Contains(s) || 
                                     (a.User != null && (a.User.Username.Contains(s) || a.User.Email.Contains(s))));
        }

        // Lọc theo loại hành động
        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(a => a.Action == actionType);
        }

        // Lọc theo ngày
        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= fromDate.Value.ToUniversalTime());
        }
        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(a => a.CreatedAt <= endOfDay);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var auditLogs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.ActionType = actionType;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = Math.Max(1, totalPages);
        ViewBag.TotalItems = totalItems;

        // Lấy danh sách các action duy nhất cho bộ lọc dropdown
        ViewBag.AvailableActions = await _context.AuditLogs
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return View(auditLogs);
    }
}
