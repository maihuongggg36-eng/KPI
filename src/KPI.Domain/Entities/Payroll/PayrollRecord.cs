using KPI.Domain.Common;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using EvaluationEntity = KPI.Domain.Entities.Evaluation.Evaluation;

namespace KPI.Domain.Entities.Payroll;

/// <summary>
/// Thực thể Bảng Lương & Chi trả Tiền mặt theo chuẩn Học viện Hành chính và Quản trị công
/// Áp dụng Nghị định 204/2004/NĐ-CP (Ngạch bậc) + Nghị định 73/2024/NĐ-CP (Mức lương cơ sở 2.340.000đ)
/// Kết hợp Chấm công (20%) + KPI Vượt mục tiêu BGĐ (70%) + Đồng nghiệp (10%)
/// </summary>
public class PayrollRecord : BaseAuditableEntity, ISoftDeletable
{
    public long PeriodId { get; set; }
    public virtual EvaluationPeriod Period { get; set; } = null!;

    public long EmployeeId { get; set; }
    public virtual EmployeeProfile Employee { get; set; } = null!;

    public long? EvaluationId { get; set; }
    public virtual EvaluationEntity? Evaluation { get; set; }

    // 1. Chấm công & Ngày làm việc
    public int StandardWorkDays { get; set; } = 22;      // Ngày công chuẩn tháng
    public int ActualWorkDays { get; set; } = 22;        // Ngày công thực tế đi làm
    public decimal WorkDayRatio { get; set; } = 1.0m;    // ActualWorkDays / StandardWorkDays
    public decimal AttendanceScore { get; set; } = 20.0m;// Điểm chấm công (max 20)

    // 2. P1: Lương ngạch bậc viên chức/công chức
    public decimal BaseSalaryUnit { get; set; } = 2340000m; // Mức lương cơ sở: 2.340.000đ (NĐ 73/2024)
    public decimal SalaryCoefficient { get; set; } = 2.34m; // Hệ số lương (GV: 2.34 -> 4.98; GVC: 4.40 -> 6.78)
    public decimal BaseSalary { get; set; } = 0.0m;         // Lương ngạch bậc thực nhận = BaseSalaryUnit * SalaryCoefficient * WorkDayRatio

    // 3. P2: Phụ cấp chi tiết theo quy định Học viện
    public decimal PositionAllowance { get; set; } = 0.0m;   // Phụ cấp chức vụ (HS chức vụ * 2.340.000đ)
    public decimal SeniorityAllowance { get; set; } = 0.0m;  // Phụ cấp thâm niên nghề / vượt khung
    public decimal ProfessionAllowance { get; set; } = 0.0m; // Phụ cấp ưu đãi nghề (25% - 45% lương + PC chức vụ)
    public decimal OtherAllowance { get; set; } = 0.0m;      // Phụ cấp trách nhiệm, độc hại, v.v.
    public decimal Allowances { get; set; } = 0.0m;          // Tổng các khoản phụ cấp = Position + Seniority + Profession + Other

    // 4. P3: Thu nhập tăng thêm theo hiệu quả công việc & KPI
    public decimal KpiScore { get; set; } = 0.0m;
    public string KpiClassification { get; set; } = string.Empty;
    public decimal KpiMultiplier { get; set; } = 1.0m;       // 1.3 (Xuất sắc - Vượt mục tiêu BGĐ), 1.15 (Tốt), 1.0 (Hoàn thành), 0.7 (Không HT)
    public decimal P3Salary { get; set; } = 0.0m;           // Thu nhập KPI = BaseSalary * KpiMultiplier * WorkDayRatio
    public decimal BonusAmount { get; set; } = 0.0m;        // Thưởng thành tích đột xuất / sáng kiến

    // 5. Khấu trừ bảo hiểm bắt buộc theo luật (BHXH 8%, BHYT 1.5%, BHTN 1%, Công đoàn 1%)
    public decimal InsuranceSalary { get; set; } = 0.0m;     // Tiền lương đóng BH = Lương ngạch bậc + PC chức vụ + PC thâm niên
    public decimal BhxhDeduction { get; set; } = 0.0m;       // BHXH 8%
    public decimal BhytDeduction { get; set; } = 0.0m;       // BHYT 1.5%
    public decimal BhtnDeduction { get; set; } = 0.0m;       // BHTN 1.0%
    public decimal UnionFee { get; set; } = 0.0m;            // Kinh phí công đoàn 1.0%
    public decimal Deductions { get; set; } = 0.0m;          // Tổng trích nộp = BHXH + BHYT + BHTN + Công đoàn

    // 6. Thuế thu nhập cá nhân (TNCN)
    public decimal TaxableIncome { get; set; } = 0.0m;       // Thu nhập chịu thuế
    public decimal PersonalIncomeTax { get; set; } = 0.0m;   // Thuế TNCN phải nộp

    // 7. Tổng thu nhập & Thực lĩnh
    public decimal GrossIncome { get; set; } = 0.0m;         // Tổng thu nhập trước thuế = BaseSalary + Allowances + P3Salary + BonusAmount
    public decimal NetSalary { get; set; } = 0.0m;           // Thực lĩnh = GrossIncome - Deductions - PersonalIncomeTax

    // 8. Quản lý chi tiền mặt & Chứng từ (Mẫu 02-TT / 05a)
    public string VoucherCode { get; set; } = string.Empty;  // KPI-PC-YYYY-NNNN
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public string PaymentStatus { get; set; } = "Chưa thanh toán"; // "Chưa thanh toán", "Đã thanh toán"
    public DateTime? PaidAt { get; set; }
    public long? CashierId { get; set; }
    public virtual User? Cashier { get; set; }
    public string? RecipientSignature { get; set; }
    public string? Notes { get; set; }
    public string? VerificationHash { get; set; }

    /// <summary>
    /// Tính toán toàn diện lương viên chức/giảng viên theo quy chuẩn NĐ 204/2004 + NĐ 73/2024
    /// </summary>
    public void CalculateSalary()
    {
        WorkDayRatio = StandardWorkDays > 0 ? Math.Clamp(Math.Round((decimal)ActualWorkDays / StandardWorkDays, 4), 0m, 1.5m) : 1.0m;

        // P1: Lương ngạch bậc
        BaseSalary = Math.Round(BaseSalaryUnit * SalaryCoefficient * WorkDayRatio, 0);

        // P2: Phụ cấp
        Allowances = Math.Round(PositionAllowance + SeniorityAllowance + ProfessionAllowance + OtherAllowance, 0);

        // P3: Performance Pay (KPI)
        P3Salary = Math.Round(BaseSalary * KpiMultiplier * WorkDayRatio, 0);

        // Gross
        GrossIncome = BaseSalary + Allowances + P3Salary + BonusAmount;

        // Bảo hiểm: Tính trên mức đóng bảo hiểm
        InsuranceSalary = BaseSalary + PositionAllowance + SeniorityAllowance;
        BhxhDeduction = Math.Round(InsuranceSalary * 0.08m, 0);
        BhytDeduction = Math.Round(InsuranceSalary * 0.015m, 0);
        BhtnDeduction = Math.Round(InsuranceSalary * 0.01m, 0);
        UnionFee = Math.Round(InsuranceSalary * 0.01m, 0);
        Deductions = BhxhDeduction + BhytDeduction + BhtnDeduction + UnionFee;

        // Thuế TNCN (Giảm trừ bản thân 11.000.000đ)
        decimal personalDeduction = 11000000m;
        decimal netBeforeTax = GrossIncome - Deductions;
        TaxableIncome = Math.Max(0m, netBeforeTax - personalDeduction);

        // Biểu thuế lũy tiến từng phần đơn giản hóa bậc 1 (5% dưới 5 triệu), bậc 2 (10% từ 5-10 triệu)
        if (TaxableIncome <= 0)
        {
            PersonalIncomeTax = 0m;
        }
        else if (TaxableIncome <= 5000000m)
        {
            PersonalIncomeTax = Math.Round(TaxableIncome * 0.05m, 0);
        }
        else if (TaxableIncome <= 10000000m)
        {
            PersonalIncomeTax = Math.Round(250000m + ((TaxableIncome - 5000000m) * 0.10m), 0);
        }
        else
        {
            PersonalIncomeTax = Math.Round(750000m + ((TaxableIncome - 10000000m) * 0.15m), 0);
        }

        // Thực lĩnh
        NetSalary = GrossIncome - Deductions - PersonalIncomeTax;
    }
}
