using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Giao diện dịch vụ Quản lý Lương 3P (P1-P2-P3) & Chi trả Tiền mặt
/// </summary>
public interface IPayrollService
{
    /// <summary>
    /// Lấy danh sách bảng tính lương theo kỳ và bộ lọc phòng ban, trạng thái thanh toán
    /// </summary>
    Task<List<PayrollItemDto>> GetPayrollListAsync(long periodId, long? departmentId = null, string? paymentStatus = null);

    /// <summary>
    /// Thống kê tổng quan quỹ lương và tiến độ giải ngân
    /// </summary>
    Task<PayrollStatsDto> GetPayrollStatsAsync(long periodId);

    /// <summary>
    /// Tính toán bảng lương toàn bộ nhân sự theo kết quả đánh giá KPI trong kỳ
    /// Công thức: Lương_P3 = LươngCơBản * HệSốLương * HệSốKPI
    /// </summary>
    Task<int> CalculateAllPayrollAsync(long periodId, long currentUserId);

    /// <summary>
    /// Kế toán điều chỉnh một dòng phiếu lương
    /// </summary>
    Task<bool> UpdatePayrollItemAsync(UpdatePayrollItemRequest request, long currentUserId);

    /// <summary>
    /// Xác nhận ký nhận chi tiền mặt cho một nhân viên
    /// </summary>
    Task<bool> MarkCashPaidAsync(long payrollId, string signature, long currentUserId);

    /// <summary>
    /// Đánh dấu tất cả nhân sự đã nhận tiền mặt tại quỹ (Batch payout)
    /// </summary>
    Task<int> MarkAllCashPaidAsync(long periodId, long currentUserId);

    /// <summary>
    /// Lấy thông tin Phiếu Chi tiền mặt Mẫu 02-TT
    /// </summary>
    Task<CashVoucherDto?> GetCashVoucherAsync(long payrollId);

    /// <summary>
    /// Lấy dữ liệu Bảng Ký Nhận Tiền Mặt Mẫu 05a
    /// </summary>
    Task<CashSheetDto> GetCashSheetAsync(long periodId, long? departmentId = null);

    /// <summary>
    /// Lấy Phiếu lương chi tiết (Payslip) cá nhân cho nhân viên
    /// </summary>
    Task<PayslipDto?> GetMyPayslipAsync(long periodId, long employeeId);

    /// <summary>
    /// Lấy dữ liệu Báo cáo & Phân tích cơ cấu Lương theo phòng ban và theo band
    /// </summary>
    Task<PayrollReportDto> GetPayrollReportAsync(long periodId);

    /// <summary>
    /// Mapping hệ số KPI (Xuất sắc: 1.3, Tốt: 1.15, Đạt: 1.0, Không đạt: 0.7)
    /// </summary>
    decimal CalculateKpiMultiplier(string? classification, decimal kpiScore);

    /// <summary>
    /// Dịch số tiền thành chữ tiếng Việt chuẩn kế toán ("Một triệu năm trăm nghìn đồng chẵn.")
    /// </summary>
    string ConvertNumberToVietnameseWords(decimal number);
}
