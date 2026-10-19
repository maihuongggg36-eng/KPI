namespace KPI.Application.DTOs;

/// <summary>
/// DTO Mục tiêu Ban Giám đốc đưa ra (Director Targets)
/// </summary>
public class DirectorTargetDto
{
    public long Id { get; set; }
    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "Toàn Học viện";

    public string TargetName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal ChallengeValue { get; set; }
    public string TargetUnit { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO Tạo mới hoặc cập nhật Mục tiêu Ban Giám đốc
/// </summary>
public class CreateDirectorTargetRequest
{
    public long? Id { get; set; }
    public long PeriodId { get; set; }
    public long? DepartmentId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal ChallengeValue { get; set; }
    public string TargetUnit { get; set; } = string.Empty;
    public string? Description { get; set; }
}
