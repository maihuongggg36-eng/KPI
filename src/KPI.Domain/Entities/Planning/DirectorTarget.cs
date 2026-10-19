using KPI.Domain.Common;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;

namespace KPI.Domain.Entities.Planning;

/// <summary>
/// Mục tiêu chỉ đạo do Ban Giám đốc đặt ra cho toàn viện hoặc từng khoa/phòng (director_targets)
/// Nhân viên/Giảng viên phải VƯỢT qua mức mục tiêu này mới đủ điều kiện đạt xếp loại Tốt (K=1.15) hoặc Xuất sắc (K=1.3)
/// </summary>
public class DirectorTarget : BaseEntity, ISoftDeletable
{
    public long PeriodId { get; set; }
    public virtual EvaluationPeriod Period { get; set; } = null!;

    public long? DepartmentId { get; set; } // null = áp dụng toàn Học viện
    public virtual Department? Department { get; set; }

    public string TargetName { get; set; } = string.Empty; // "Số tiết giảng dạy định mức", "Số bài báo NCKH"
    public decimal TargetValue { get; set; }               // Chỉ tiêu tối thiểu chuẩn (ví dụ: 320 tiết)
    public decimal ChallengeValue { get; set; }            // Mức mục tiêu thách thức/vượt (ví dụ: 350 tiết)
    public string TargetUnit { get; set; } = string.Empty; // "tiết", "bài báo", "%"
    public string? Description { get; set; }

    public long CreatedBy { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
