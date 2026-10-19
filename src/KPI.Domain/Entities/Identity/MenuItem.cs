using KPI.Domain.Common;

namespace KPI.Domain.Entities.Identity;

/// <summary>
/// Quản lý Menu / Đầu mục điều hướng hệ thống do ADMIN toàn quyền quản lý (menu_items)
/// </summary>
public class MenuItem : BaseEntity, ISoftDeletable
{
    public long? ParentId { get; set; }
    public virtual MenuItem? Parent { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public string? PermissionCode { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public string? SectionTitle { get; set; } // Phân nhóm menu (ví dụ: "Tổng quan", "Kế hoạch & Giao KPI", "Quản trị Hệ thống")

    // Navigation properties
    public virtual ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
}
