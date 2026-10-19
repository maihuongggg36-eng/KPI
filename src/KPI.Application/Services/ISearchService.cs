using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ Tìm kiếm Toàn diện Đa chiều (Command Palette Ctrl+K)
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Tìm kiếm nhanh theo từ khóa qua Nhân sự, Chỉ tiêu KPI, Danh mục và Trang chức năng
    /// </summary>
    Task<SearchResultDto> QuickSearchAsync(string query, long currentUserId, string role);
}
