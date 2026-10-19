namespace KPI.Application.DTOs;

/// <summary>
/// DTO Bảng Chấm công và Kỷ luật lao động (20% điểm KPI)
/// </summary>
public class AttendanceDto
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;

    public int Year { get; set; }
    public int Month { get; set; }

    // Ngày công
    public int StandardWorkDays { get; set; } = 22;
    public int ActualWorkDays { get; set; } = 22;
    public int PaidLeave { get; set; } = 0;
    public int UnpaidLeave { get; set; } = 0;
    public int SickLeave { get; set; } = 0;

    // Vi phạm
    public int LateCount { get; set; } = 0;
    public int EarlyLeaveCount { get; set; } = 0;
    public int UnauthorizedAbsence { get; set; } = 0;
    public int MissedMeetings { get; set; } = 0;
    public bool DisciplineViolation { get; set; } = false;

    // Điểm số & Tỷ lệ
    public decimal AttendanceScore { get; set; } = 20.0m;
    public decimal WorkDayRatio { get; set; } = 1.0m;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO Nhập / Cập nhật dữ liệu Chấm công
/// </summary>
public class AttendanceInputDto
{
    public long EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public int StandardWorkDays { get; set; } = 22;
    public int ActualWorkDays { get; set; } = 22;
    public int PaidLeave { get; set; } = 0;
    public int UnpaidLeave { get; set; } = 0;
    public int SickLeave { get; set; } = 0;

    public int LateCount { get; set; } = 0;
    public int EarlyLeaveCount { get; set; } = 0;
    public int UnauthorizedAbsence { get; set; } = 0;
    public int MissedMeetings { get; set; } = 0;
    public bool DisciplineViolation { get; set; } = false;

    public string? Notes { get; set; }
}
