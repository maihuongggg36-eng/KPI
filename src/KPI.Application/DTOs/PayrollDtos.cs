namespace KPI.Application.DTOs;

/// <summary>
/// DTO đại diện cho 1 dòng bảng lương nhân viên gắn với KPI (P1-P2-P3)
/// </summary>
public class PayrollItemDto
{
    public long Id { get; set; }
    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int PeriodYear { get; set; }

    public long EmployeeId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = "/images/default-avatar.png";

    public long DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;

    // P1: Lương cơ bản & Hệ số (Nghị định 204/2004 + Nghị định 73/2024: Mức cơ sở 2.340.000đ)
    public decimal BaseSalaryUnit { get; set; } = 2340000m;
    public decimal BaseSalary { get; set; }
    public decimal SalaryCoefficient { get; set; }

    // Chấm công & Kỷ luật
    public int StandardWorkDays { get; set; } = 22;
    public int ActualWorkDays { get; set; } = 22;
    public decimal WorkDayRatio { get; set; } = 1.0m;
    public decimal AttendanceScore { get; set; } = 20.0m;

    // P2: Phụ cấp chi tiết
    public decimal PositionAllowance { get; set; } = 0.0m;
    public decimal SeniorityAllowance { get; set; } = 0.0m;
    public decimal ProfessionAllowance { get; set; } = 0.0m;
    public decimal OtherAllowance { get; set; } = 0.0m;
    public decimal Allowances { get; set; }

    // P3: Performance Pay (KPI gắn với Vượt mục tiêu BGĐ)
    public decimal KpiScore { get; set; }
    public string KpiClassification { get; set; } = string.Empty;
    public decimal KpiMultiplier { get; set; } // 1.3 (Xuất sắc), 1.15 (Tốt), 1.0 (Đạt), 0.7 (Không đạt)
    public decimal P3Salary { get; set; }      // BaseSalary * SalaryCoefficient * KpiMultiplier

    // Khấu trừ Bảo hiểm & Thuế TNCN
    public decimal BonusAmount { get; set; }
    public decimal BhxhDeduction { get; set; }
    public decimal BhytDeduction { get; set; }
    public decimal BhtnDeduction { get; set; }
    public decimal UnionFee { get; set; }
    public decimal Deductions { get; set; }
    public decimal PersonalIncomeTax { get; set; }

    // Tổng thu nhập & Thực lĩnh
    public decimal GrossIncome { get; set; }
    public decimal NetSalary { get; set; }

    // Chi trả tiền mặt
    public string VoucherCode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public string PaymentStatus { get; set; } = "Chưa thanh toán"; // "Đã thanh toán", "Chưa thanh toán"
    public DateTime? PaidAt { get; set; }
    public string? CashierName { get; set; }
    public string? RecipientSignature { get; set; }
    public string? Notes { get; set; }
    public string? VerificationHash { get; set; }

    public bool IsPaid => PaymentStatus == "Đã thanh toán";
    public string RowStatusClass => IsPaid ? "table-success-subtle" : "table-warning-subtle";
}

/// <summary>
/// DTO Thống kê tổng hợp Quỹ lương & Trạng thái giải ngân tiền mặt
/// </summary>
public class PayrollStatsDto
{
    public int TotalEmployees { get; set; }
    public decimal TotalBaseSalary { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TotalP3Salary { get; set; }
    public decimal TotalBonus { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetSalary { get; set; }
    public decimal TotalCashPaid { get; set; }
    public decimal TotalCashPending { get; set; }
    public int CountPaid { get; set; }
    public int CountPending { get; set; }

    public double PaymentRate => TotalNetSalary > 0 
        ? Math.Round((double)(TotalCashPaid / TotalNetSalary) * 100, 1) 
        : 0;
}

/// <summary>
/// DTO Phiếu Chi Tiền Mặt Mẫu 02-TT (Theo Thông tư 133/2016/TT-BTC & 200/2014/TT-BTC)
/// </summary>
public class CashVoucherDto
{
    public long PayrollId { get; set; }
    public string VoucherCode { get; set; } = string.Empty; // KPI-PC-YYYY-NNNN
    public string DateFormatted { get; set; } = string.Empty;
    public int Day { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Đơn vị
    public string OrganizationName { get; set; } = "HỌC VIỆN HÀNH CHÍNH QUỐC GIA";
    public string DepartmentName { get; set; } = string.Empty;

    // Định khoản kế toán
    public string DebitAccount { get; set; } = "TK 3341, TK 642";
    public string CreditAccount { get; set; } = "TK 1111 (Tiền mặt)";

    // Người nhận
    public string FullName { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string Address { get; set; } = "Học viện Hành chính Quốc gia";

    // Lý do chi & Số tiền
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountInWords { get; set; } = string.Empty; // Chữ tiếng Việt chuẩn kế toán
    public string Attachments { get; set; } = "01 Bảng đánh giá KPI và Phiếu lương chi tiết";

    // Ký tên 4 bên
    public string DirectorName { get; set; } = "PGS.TS Nguyễn Văn Giám Đốc";
    public string ChiefAccountantName { get; set; } = "ThS. Trần Thị Kế Toán Trưởng";
    public string CashierName { get; set; } = "Lê Thị Thủ Quỹ";
    public string PreparerName { get; set; } = "Nguyễn Văn Kế Toán Viên";
}

/// <summary>
/// DTO Bảng Ký Nhận Tiền Mặt Mẫu 05a
/// </summary>
public class CashSheetItemDto
{
    public int Index { get; set; }
    public long PayrollId { get; set; }
    public long EmployeeId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal P3Salary { get; set; }
    public decimal Allowances { get; set; }
    public decimal NetSalary { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? RecipientSignature { get; set; }
}

public class CashSheetDto
{
    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int Year { get; set; }
    public long? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "Toàn cơ quan";

    public List<CashSheetItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string TotalAmountInWords { get; set; } = string.Empty;
    public int TotalEmployees => Items.Count;
    public int TotalSignedCount => Items.Count(i => i.IsPaid);
}

/// <summary>
/// DTO Phiếu Lương Cá Nhân (Payslip)
/// </summary>
public class PayslipDto
{
    public long PayrollId { get; set; }
    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int Year { get; set; }

    public long EmployeeId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string RankName { get; set; } = "Chuyên viên";

    // Chi tiết P1: Lương ngạch bậc (NĐ 204/2004 + NĐ 73/2024: Mức cơ sở 2.340.000đ)
    public decimal BaseSalaryUnit { get; set; } = 2340000m;
    public decimal BaseSalary { get; set; }         // P1
    public decimal SalaryCoefficient { get; set; }  // P1

    // Chấm công & Kỷ luật
    public int StandardWorkDays { get; set; } = 22;
    public int ActualWorkDays { get; set; } = 22;
    public decimal AttendanceScore { get; set; } = 20.0m;

    // Chi tiết P2: Phụ cấp
    public decimal PositionAllowance { get; set; } = 0.0m;
    public decimal SeniorityAllowance { get; set; } = 0.0m;
    public decimal ProfessionAllowance { get; set; } = 0.0m;
    public decimal OtherAllowance { get; set; } = 0.0m;
    public decimal Allowances { get; set; }         // P2

    // Chi tiết P3: Performance Pay
    public decimal KpiScore { get; set; }           // P3
    public string KpiClassification { get; set; } = string.Empty; // P3
    public decimal KpiMultiplier { get; set; }      // P3
    public decimal P3Salary { get; set; }           // P3

    // Thưởng / Giảm trừ / Thuế
    public decimal BonusAmount { get; set; }
    public decimal SocialInsurance { get; set; }     // BHXH 8%
    public decimal HealthInsurance { get; set; }     // BHYT 1.5%
    public decimal UnemploymentInsurance { get; set; } // BHTN 1%
    public decimal UnionFee { get; set; }            // Công đoàn 1%
    public decimal TotalDeductions { get; set; }
    public decimal PersonalIncomeTax { get; set; }

    // Thực lĩnh
    public decimal GrossIncome { get; set; }
    public decimal NetSalary { get; set; }
    public string AmountInWords { get; set; } = string.Empty;

    // Xác thực
    public string VoucherCode { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string QrCodeContent { get; set; } = string.Empty;
    public string VerificationHash { get; set; } = string.Empty;
}

/// <summary>
/// DTO Báo cáo Phân tích Lương & Quỹ thưởng
/// </summary>
public class PayrollReportDto
{
    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public PayrollStatsDto Stats { get; set; } = new();

    // Biểu đồ: Chi lương theo phòng ban (Stacked Bar: P1, P2, P3)
    public List<DepartmentPayrollChartDto> DepartmentCharts { get; set; } = new();

    // Biểu đồ: Phân bổ lương theo band (Histogram)
    public List<SalaryBandDto> SalaryBands { get; set; } = new();
}

public class DepartmentPayrollChartDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalP1 { get; set; }
    public decimal TotalP2 { get; set; }
    public decimal TotalP3 { get; set; }
    public decimal TotalNet { get; set; }
}

public class SalaryBandDto
{
    public string BandLabel { get; set; } = string.Empty; // Dưới 10tr, 10-15tr, 15-20tr, Trên 20tr
    public int EmployeeCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Request update lương 1 dòng
/// </summary>
public class UpdatePayrollItemRequest
{
    public long PayrollId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal Deductions { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request xác nhận ký nhận tiền mặt 1 người
/// </summary>
public class ConfirmCashPaymentRequest
{
    public long PayrollId { get; set; }
    public string? Signature { get; set; }
}
