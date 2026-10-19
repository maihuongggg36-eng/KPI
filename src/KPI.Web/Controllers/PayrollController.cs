using System.Security.Claims;
using System.Text;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Web.ViewModels.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[Authorize]
public class PayrollController : Controller
{
    private readonly IPayrollService _payrollService;
    private readonly ICatalogService _catalogService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        IPayrollService payrollService,
        ICatalogService catalogService,
        ILogger<PayrollController> logger)
    {
        _payrollService = payrollService;
        _catalogService = catalogService;
        _logger = logger;
    }

    #region 1. Bảng Tính Lương Theo KPI (P3) & Quản lý Giải Ngân Tiền Mặt

    [HttpGet]
    public async Task<IActionResult> Index(long? periodId, long? departmentId, string? paymentStatus)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Officer";

        // Cán bộ / Chuyên viên thông thường chỉ truy cập Phiếu lương cá nhân
        if (role.Equals("Officer", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(MyPayslip), new { periodId });
        }

        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var targetPeriodId = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var items = await _payrollService.GetPayrollListAsync(targetPeriodId, departmentId, paymentStatus);
        var stats = await _payrollService.GetPayrollStatsAsync(targetPeriodId);
        var departments = await _catalogService.GetAllDepartmentsFlatAsync();

        bool canManage = role.Equals("Accountant", StringComparison.OrdinalIgnoreCase) ||
                         role.Equals("Director", StringComparison.OrdinalIgnoreCase) ||
                         role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        var viewModel = new PayrollIndexViewModel
        {
            Items = items,
            Stats = stats,
            SelectedPeriodId = targetPeriodId,
            SelectedDepartmentId = departmentId,
            SelectedPaymentStatus = paymentStatus,
            Periods = periods,
            Departments = departments,
            UserRole = role,
            CanCalculateAndDisburse = canManage
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate([FromBody] CalculatePayrollBatchRequest request)
    {
        try
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (!IsAccountantOrDirector(role))
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện tính toán quỹ lương." });
            }

            var currentUserId = GetCurrentUserId();
            var count = await _payrollService.CalculateAllPayrollAsync(request.PeriodId, currentUserId);

            return Json(new 
            { 
                success = true, 
                count, 
                message = $"Đã tính toán thành công lương 3P (P1-P2-P3) cho {count} cán bộ, nhân viên dựa trên kết quả KPI!" 
            });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, code = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tính toán bảng lương");
            return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem([FromBody] UpdatePayrollItemRequest request)
    {
        try
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (!IsAccountantOrDirector(role))
            {
                return Json(new { success = false, message = "Bạn không có quyền điều chỉnh phiếu lương." });
            }

            var currentUserId = GetCurrentUserId();
            var success = await _payrollService.UpdatePayrollItemAsync(request, currentUserId);
            return Json(new { success, message = success ? "Cập nhật phiếu lương thành công!" : "Không tìm thấy phiếu lương." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật phiếu lương");
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayCash([FromBody] ConfirmCashPaymentRequest request)
    {
        try
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (!IsAccountantOrDirector(role))
            {
                return Json(new { success = false, message = "Bạn không có quyền xác nhận chi tiền mặt." });
            }

            var currentUserId = GetCurrentUserId();
            var success = await _payrollService.MarkCashPaidAsync(request.PayrollId, request.Signature ?? "Đã nhận tiền mặt tại quỹ", currentUserId);

            return Json(new { success, message = success ? "Đã xác nhận chi tiền mặt thành công!" : "Không tìm thấy phiếu lương." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xác nhận chi tiền mặt");
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayAllCash([FromBody] CalculatePayrollBatchRequest request)
    {
        try
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (!IsAccountantOrDirector(role))
            {
                return Json(new { success = false, message = "Bạn không có quyền giải ngân hàng loạt." });
            }

            var currentUserId = GetCurrentUserId();
            var count = await _payrollService.MarkAllCashPaidAsync(request.PeriodId, currentUserId);

            return Json(new 
            { 
                success = true, 
                count, 
                message = $"Đã hoàn tất xác nhận chi trả tiền mặt cho {count} cán bộ, nhân viên!" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi giải ngân hàng loạt");
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }

    #endregion

    #region 2. Phiếu Chi Tiền Mặt — Mẫu 02-TT

    [HttpGet]
    public async Task<IActionResult> Voucher(long id)
    {
        var voucher = await _payrollService.GetCashVoucherAsync(id);
        if (voucher == null) return NotFound();

        var viewModel = new CashVoucherViewModel { Voucher = voucher };
        return View(viewModel);
    }

    #endregion

    #region 3. Bảng Ký Nhận Tiền Mặt — Mẫu 05a

    [HttpGet]
    public async Task<IActionResult> CashSheet(long? periodId, long? departmentId)
    {
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var targetPeriodId = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var sheet = await _payrollService.GetCashSheetAsync(targetPeriodId, departmentId);
        var departments = await _catalogService.GetAllDepartmentsFlatAsync();
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        var viewModel = new CashSheetViewModel
        {
            Sheet = sheet,
            Periods = periods,
            Departments = departments,
            SelectedPeriodId = targetPeriodId,
            SelectedDepartmentId = departmentId,
            CanDisburse = IsAccountantOrDirector(role)
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCashSheetCsv(long periodId, long? departmentId)
    {
        var sheet = await _payrollService.GetCashSheetAsync(periodId, departmentId);
        var csv = new StringBuilder();

        // UTF-8 BOM
        csv.Append('\uFEFF');
        csv.AppendLine("STT,Mã CB,Họ và tên,Chức vụ,Phòng ban,Lương cơ bản (P1),Lương KPI (P3),Phụ cấp (P2),Thực lĩnh (VNĐ),Trạng thái,Ký nhận");

        foreach (var item in sheet.Items)
        {
            csv.AppendLine($"{item.Index},\"{item.StaffCode}\",\"{item.FullName}\",\"{item.PositionName}\",\"{item.DepartmentName}\",{item.BaseSalary},{item.P3Salary},{item.Allowances},{item.NetSalary},\"{(item.IsPaid ? "Đã nhận tiền" : "Chờ nhận")}\",\"{item.RecipientSignature ?? ""}\"");
        }

        csv.AppendLine($",,,,Tổng cộng,,,{sheet.TotalAmount},,");

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"Bang_Ky_Nhan_Tien_Mat_{sheet.PeriodName.Replace(" ", "_")}.csv";
        return File(bytes, "text/csv", fileName);
    }

    #endregion

    #region 4. Payslip Cá Nhân (Phiếu Lương)

    [HttpGet]
    public async Task<IActionResult> MyPayslip(long? periodId)
    {
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var targetPeriodId = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var currentUserId = GetCurrentUserId();
        var payslip = await _payrollService.GetMyPayslipAsync(targetPeriodId, currentUserId);

        var viewModel = new MyPayslipViewModel
        {
            Payslip = payslip,
            Periods = periods,
            SelectedPeriodId = targetPeriodId
        };

        return View(viewModel);
    }

    #endregion

    #region 5. Báo Cáo Phân Tích Lương & Quỹ Thưởng

    [HttpGet]
    public async Task<IActionResult> Reports(long? periodId)
    {
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var targetPeriodId = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var report = await _payrollService.GetPayrollReportAsync(targetPeriodId);

        var viewModel = new PayrollReportViewModel
        {
            Report = report,
            Periods = periods,
            SelectedPeriodId = targetPeriodId
        };

        return View(viewModel);
    }

    #endregion

    #region Helper Methods

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && long.TryParse(claim.Value, out var id) ? id : 1;
    }

    private static bool IsAccountantOrDirector(string role)
    {
        return role.Equals("Accountant", StringComparison.OrdinalIgnoreCase) ||
               role.Equals("Director", StringComparison.OrdinalIgnoreCase) ||
               role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

public class CalculatePayrollBatchRequest
{
    public long PeriodId { get; set; }
}
