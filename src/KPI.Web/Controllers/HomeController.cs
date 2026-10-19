using System.Diagnostics;
using KPI.Infrastructure.Data;
using KPI.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KPI.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly KpiDbContext _dbContext;

    public HomeController(ILogger<HomeController> logger, KpiDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Trang Chủ - Hệ Thống Quản Lý & Đánh Giá KPI";
        ViewData["MetaDescription"] = "Cổng thông tin Đánh giá KPI và Phân loại Cán bộ, Giảng viên Học viện Hành chính và Quản trị công theo Nghị định 90/2020/NĐ-CP.";
        
        var dbCanConnect = false;
        var userCount = 0;
        var roleCount = 0;
        var deptCount = 0;
        var kpiItemCount = 0;

        try
        {
            dbCanConnect = await _dbContext.Database.CanConnectAsync();
            if (dbCanConnect)
            {
                userCount = await _dbContext.Users.AsNoTracking().CountAsync();
                roleCount = await _dbContext.Roles.AsNoTracking().CountAsync();
                deptCount = await _dbContext.Departments.AsNoTracking().CountAsync();
                kpiItemCount = await _dbContext.KpiMasterItems.AsNoTracking().CountAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Không thể kết nối trực tiếp CSDL: {Message}", ex.Message);
        }

        ViewBag.DbCanConnect = dbCanConnect;
        ViewBag.UserCount = userCount;
        ViewBag.RoleCount = roleCount;
        ViewBag.DeptCount = deptCount;
        ViewBag.KpiItemCount = kpiItemCount;

        return View();
    }

    [HttpGet]
    public IActionResult About()
    {
        ViewData["Title"] = "Giới Thiệu Hệ Thống KPI";
        ViewData["MetaDescription"] = "Giới thiệu Hệ thống Quản trị & Đánh giá Hiệu quả Công việc Cán bộ, Giảng viên Học viện Hành chính và Quản trị công.";
        return View();
    }

    [HttpGet]
    public IActionResult UserGuide()
    {
        ViewData["Title"] = "Hướng Dẫn Sử Dụng & Quy Trình KPI";
        ViewData["MetaDescription"] = "Cẩm nang hướng dẫn sử dụng phần mềm KPI cho Giảng viên, Lãnh đạo Khoa, Phòng và Ban Giám Đốc.";
        return View();
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        ViewData["Title"] = "Chính Sách Bảo Mật & Quyền Riêng Tư";
        return View();
    }

    [Route("/Home/Error/{statusCode?}")]
    [HttpGet]
    [HttpPost]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var code = statusCode ?? Response.StatusCode;
        if (code == 200 && !statusCode.HasValue) code = 500;

        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        };

        return View(model);
    }
}
