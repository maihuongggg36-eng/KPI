using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Giao diện dịch vụ Báo cáo & Phân tích Dữ liệu Hiệu suất (FR-REP-01 đến FR-REP-05)
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Lấy thẻ tóm tắt báo cáo tổng quan (Summary cards, tỷ lệ hoàn thành, chuẩn Gauss)
    /// </summary>
    Task<ExecutiveReportSummaryDto> GetExecutiveSummaryAsync(ReportFilterRequest request);

    /// <summary>
    /// Lấy bảng xếp hạng hiệu suất (Leaderboard) hỗ trợ sắp xếp và top 3
    /// </summary>
    Task<List<LeaderboardItemDto>> GetLeaderboardAsync(ReportFilterRequest request);

    /// <summary>
    /// Lấy dữ liệu phân tích chuông Gauss (Bell curve overlay thực tế vs chuẩn lý tưởng)
    /// </summary>
    Task<GaussDistributionDto> GetGaussDistributionAsync(ReportFilterRequest request);

    /// <summary>
    /// Lấy danh sách cán bộ drill-down theo từng band điểm của chuông Gauss
    /// </summary>
    Task<List<LeaderboardItemDto>> GetDrillDownEmployeesAsync(long periodId, string bandCode);

    /// <summary>
    /// Phân tích xu hướng (Trend qua các kỳ, so sánh phòng ban, ma trận Heatmap)
    /// </summary>
    Task<TrendAnalysisDto> GetTrendAnalysisAsync(long currentPeriodId);

    /// <summary>
    /// Trực quan hóa mở rộng (Scatter plot Outliers, Box & Whisker, Funnel chart)
    /// </summary>
    Task<DataVizExtrasDto> GetDataVizExtrasAsync(ReportFilterRequest request);

    /// <summary>
    /// Xuất báo cáo Excel định dạng chuyên nghiệp với ClosedXML
    /// </summary>
    Task<byte[]> ExportExcelAsync(ReportFilterRequest request);

    /// <summary>
    /// Xuất báo cáo PDF chuẩn in ấn A4 với QuestPDF
    /// </summary>
    Task<byte[]> ExportPdfAsync(ReportFilterRequest request);

    /// <summary>
    /// Xuất dữ liệu thô dạng CSV UTF-8
    /// </summary>
    Task<byte[]> ExportCsvAsync(ReportFilterRequest request);

    /// <summary>
    /// Đặt lịch tự động gửi email báo cáo định kỳ
    /// </summary>
    Task<bool> ScheduleReportAsync(ScheduledReportRequest request, long userId);

    /// <summary>
    /// Lấy danh sách bộ lọc yêu thích đã lưu
    /// </summary>
    Task<List<SavedFilterDto>> GetSavedFiltersAsync(long userId);

    /// <summary>
    /// Lưu bộ lọc yêu thích của người dùng
    /// </summary>
    Task<SavedFilterDto> SaveFilterAsync(SavedFilterDto filter, long userId);
}
