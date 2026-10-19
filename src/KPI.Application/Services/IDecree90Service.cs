using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Giao diện dịch vụ phân loại cán bộ công chức theo Nghị định 90/2020 & Nghị định 48/2023
/// Kiểm soát các quy tắc nghiệp vụ: BR-06 (Khống chế 20%), BR-07 (Kỷ luật), BR-08 (Sáng kiến)
/// </summary>
public interface IDecree90Service
{
    /// <summary>
    /// BR-06: Tính hạn mức trần số lượng Xuất sắc = floor(0.20 * N_Tot_Tro_Len)
    /// </summary>
    int CalculateMaxExcellenceQuota(int totalGoodAndAbove);

    /// <summary>
    /// Kiểm tra và ném ngoại lệ nếu vi phạm trần 20%
    /// </summary>
    void ValidateExcellenceCap(int totalGoodAndAbove, int totalExcellence);

    /// <summary>
    /// Xác định mức xếp loại chất lượng theo điểm số KPI và điều kiện pháp lý (BR-07, BR-08)
    /// </summary>
    string DetermineCivilServantRank(decimal kpiScore, bool hasDisciplinaryAction, bool hasApprovedInitiative);

    /// <summary>
    /// Kiểm tra trạng thái hạn mức 20% hiện tại của kỳ đánh giá
    /// </summary>
    Task<CivilServantQuotaStatusDto> CheckExcellenceCapAsync(long periodId);

    /// <summary>
    /// Lấy danh sách bảng theo dõi cán bộ công chức kèm xếp loại và bộ lọc
    /// </summary>
    Task<List<CivilServantOverviewDto>> GetCivilServantOverviewListAsync(long? periodId = null, long? departmentId = null, string? rankCode = null, string? classification = null);

    /// <summary>
    /// Lấy dữ liệu Biểu mẫu Quốc gia Mẫu 01 (Lãnh đạo) hoặc Mẫu 02 (Chuyên viên)
    /// </summary>
    Task<Form01_02Dto?> GetForm0102Async(long employeeId, long periodId);

    /// <summary>
    /// Lưu kết quả đánh giá phân loại Mẫu 01 / Mẫu 02
    /// </summary>
    Task<bool> SaveForm0102Async(SaveForm01_02Request request, long currentUserId);

    /// <summary>
    /// Lấy dữ liệu thống kê Dashboard Công chức & Phân bổ Gauss
    /// </summary>
    Task<CivilServantDashboardDto> GetCivilServantDashboardAsync(long periodId);
}
