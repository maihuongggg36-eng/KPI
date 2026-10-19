namespace KPI.Application.DTOs;

/// <summary>
/// Phần tử kết quả tìm kiếm đa chiều trong Command Palette
/// </summary>
public class SearchItemDto
{
    public string Category { get; set; } = string.Empty; // "Nhân viên", "Chỉ tiêu KPI", "Danh mục", "Trang hệ thống", "Hành động nhanh"
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-arrow-right-circle";
    public string? BadgeText { get; set; }
    public string? BadgeClass { get; set; }
}

/// <summary>
/// Toàn bộ kết quả tìm kiếm Command Palette
/// </summary>
public class SearchResultDto
{
    public string Query { get; set; } = string.Empty;
    public int TotalCount => Items.Count;
    public List<SearchItemDto> Items { get; set; } = new();
}
