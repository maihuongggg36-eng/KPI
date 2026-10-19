namespace KPI.Domain.Common;

/// <summary>
/// Thực thể cơ sở định danh duy nhất bằng ID kiểu long
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
}

/// <summary>
/// Giao diện xóa mềm (Soft Delete)
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

/// <summary>
/// Giao diện kiểm toán dữ liệu (Audit Trail)
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    long? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    long? UpdatedBy { get; set; }
}

/// <summary>
/// Thực thể cơ sở hỗ trợ kiểm toán tự động và xóa mềm
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity, IAuditableEntity, ISoftDeletable
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}
