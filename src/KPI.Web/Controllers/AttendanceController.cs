using System.Security.Claims;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KPI.Web.Controllers;

/// <summary>
/// Controller Quản lý Chấm công và Kỷ luật lao động (20% điểm KPI)
/// </summary>
[Authorize]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly KpiDbContext _context;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, KpiDbContext context, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _context = context;
        _logger = logger;
    }

    private long GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(idStr, out var id) ? id : 1;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, long? departmentId)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;

        ViewBag.Year = targetYear;
        ViewBag.Month = targetMonth;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();

        var records = await _attendanceService.GetMonthlyAttendanceAsync(targetYear, targetMonth, departmentId);

        ViewBag.TotalEmployees = records.Count;
        ViewBag.AverageScore = records.Count > 0 ? Math.Round(records.Average(r => r.AttendanceScore), 1) : 20.0m;
        ViewBag.FullAttendanceCount = records.Count(r => r.ActualWorkDays >= r.StandardWorkDays && r.LateCount == 0 && r.UnauthorizedAbsence == 0);

        return View(records);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AttendanceInputDto model)
    {
        if (model.ActualWorkDays < 0 || model.ActualWorkDays > 31)
        {
            TempData["ErrorMessage"] = "Số ngày công thực tế không hợp lệ.";
            return RedirectToAction(nameof(Index), new { year = model.Year, month = model.Month });
        }

        var result = await _attendanceService.SaveAttendanceRecordAsync(model, GetCurrentUserId());
        if (result)
        {
            TempData["SuccessMessage"] = "Đã cập nhật dữ liệu chấm công và tự động tính điểm chuyên cần thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể lưu dữ liệu chấm công.";
        }

        return RedirectToAction(nameof(Index), new { year = model.Year, month = model.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoGenerate(int year, int month)
    {
        var count = await _attendanceService.AutoGenerateMonthlyAttendanceAsync(year, month);
        TempData["SuccessMessage"] = $"Đã tự động khởi tạo dữ liệu chuẩn cho {count} cán bộ, nhân viên!";
        return RedirectToAction(nameof(Index), new { year, month });
    }
}
