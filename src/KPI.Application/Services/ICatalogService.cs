using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Giao diện dịch vụ Phân hệ Quản trị 18 Danh mục Nghiệp vụ Dùng chung
/// </summary>
public interface ICatalogService
{
    #region 1. Ngân hàng Chỉ tiêu KPI Chuẩn
    Task<List<KpiMasterItemDetailDto>> GetKpiMasterItemsAsync(string? categoryPerspective = null, string? sector = null, string? search = null);
    Task<KpiMasterItemDetailDto?> GetKpiMasterItemByIdAsync(long id);
    Task<long> CreateKpiMasterItemAsync(CreateKpiMasterItemRequest request, long userId);
    Task<bool> UpdateKpiMasterItemAsync(UpdateKpiMasterItemRequest request, long userId);
    Task<bool> DeleteKpiMasterItemAsync(long id, long userId);
    Task<long> CloneKpiMasterItemAsync(CloneKpiMasterItemRequest request, long userId);
    #endregion

    #region 2. Phòng ban & Cây phân cấp (Tree View)
    Task<List<DepartmentTreeNodeDto>> GetDepartmentTreeAsync();
    Task<List<DepartmentTreeNodeDto>> GetAllDepartmentsFlatAsync();
    Task<long> CreateDepartmentAsync(CreateDepartmentRequest request, long userId);
    Task<bool> UpdateDepartmentAsync(UpdateDepartmentRequest request, long userId);
    Task<bool> DeleteDepartmentAsync(long id, long userId);
    Task<bool> MoveDepartmentAsync(MoveDepartmentRequest request, long userId);
    #endregion

    #region 3. Chức danh & Ngạch bậc
    Task<List<PositionDetailDto>> GetPositionsAsync();
    Task<long> CreatePositionAsync(CreatePositionRequest request, long userId);
    Task<bool> DeletePositionAsync(long id, long userId);
    Task<List<PositionRankDetailDto>> GetPositionRanksAsync();
    Task<string> CreatePositionRankAsync(CreatePositionRankRequest request, long userId);
    #endregion

    #region 4. Nhóm KPI, Đơn vị tính, Phương pháp đo
    Task<List<KpiCategoryDetailDto>> GetCategoriesAsync();
    Task<List<MeasurementUnitDetailDto>> GetUnitsAsync();
    Task<List<MeasurementMethodDetailDto>> GetMethodsAsync();
    #endregion

    #region 5. Kỳ đánh giá (Evaluation Periods) & Lifecycle
    Task<List<EvaluationPeriodDetailDto>> GetEvaluationPeriodsAsync();
    Task<long> CreateEvaluationPeriodAsync(CreateEvaluationPeriodRequest request, long userId);
    Task<bool> ChangePeriodStatusAsync(ChangePeriodStatusRequest request, long userId);
    #endregion

    #region 6. Thang điểm & Mức xếp loại
    Task<List<ScoreScaleDetailDto>> GetScoreScalesAsync();
    #endregion

    #region 7. Bộ mẫu KPI Chức danh
    Task<List<KpiTemplateDetailDto>> GetKpiTemplatesAsync();
    #endregion

    #region 8. Tham số Hệ thống & Mẫu thông báo
    Task<List<SystemConfigDetailDto>> GetSystemConfigsAsync();
    Task<bool> UpdateSystemConfigAsync(UpdateSystemConfigItemRequest request, long userId);
    Task<List<NotificationTemplateDetailDto>> GetNotificationTemplatesAsync();
    #endregion

    #region 9. Thao tác nâng cao: Bulk actions, Global Search, Export CSV, Audit Log
    Task<bool> ExecuteBulkActionAsync(BulkActionRequest request, long userId);
    Task<List<GlobalSearchResultDto>> SearchGlobalCatalogsAsync(string keyword);
    Task<List<CatalogActivityLogDto>> GetCatalogActivityLogsAsync(string? entityName = null);
    Task<byte[]> ExportCatalogCsvAsync(string catalogType);
    #endregion
}
