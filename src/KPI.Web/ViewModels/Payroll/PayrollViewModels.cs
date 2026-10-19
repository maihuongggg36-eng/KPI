using KPI.Application.DTOs;

namespace KPI.Web.ViewModels.Payroll;

/// <summary>
/// ViewModel cho màn hình Bảng Tính Lương Theo KPI (P3) & Giải ngân
/// </summary>
public class PayrollIndexViewModel
{
    public List<PayrollItemDto> Items { get; set; } = new();
    public PayrollStatsDto Stats { get; set; } = new();

    public long SelectedPeriodId { get; set; }
    public long? SelectedDepartmentId { get; set; }
    public string? SelectedPaymentStatus { get; set; }

    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public List<DepartmentTreeNodeDto> Departments { get; set; } = new();

    public string UserRole { get; set; } = string.Empty;
    public bool CanCalculateAndDisburse { get; set; } = false;
}

/// <summary>
/// ViewModel cho Phiếu Chi Tiền Mặt Mẫu 02-TT
/// </summary>
public class CashVoucherViewModel
{
    public CashVoucherDto Voucher { get; set; } = new();
}

/// <summary>
/// ViewModel cho Bảng Ký Nhận Tiền Mặt Mẫu 05a
/// </summary>
public class CashSheetViewModel
{
    public CashSheetDto Sheet { get; set; } = new();
    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public List<DepartmentTreeNodeDto> Departments { get; set; } = new();
    public long SelectedPeriodId { get; set; }
    public long? SelectedDepartmentId { get; set; }
    public bool CanDisburse { get; set; } = false;
}

/// <summary>
/// ViewModel cho Phiếu Lương Cá Nhân (Payslip)
/// </summary>
public class MyPayslipViewModel
{
    public PayslipDto? Payslip { get; set; }
    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public long SelectedPeriodId { get; set; }
}

/// <summary>
/// ViewModel cho Báo Cáo Phân Tích Lương & Quỹ Thưởng
/// </summary>
public class PayrollReportViewModel
{
    public PayrollReportDto Report { get; set; } = new();
    public List<EvaluationPeriodDetailDto> Periods { get; set; } = new();
    public long SelectedPeriodId { get; set; }
}
