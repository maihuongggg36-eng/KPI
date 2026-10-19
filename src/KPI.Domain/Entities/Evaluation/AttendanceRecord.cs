using KPI.Domain.Common;
using KPI.Domain.Entities.Organization;

namespace KPI.Domain.Entities.Evaluation;

/// <summary>
/// Bảng chấm công và kỷ luật lao động hàng tháng của cán bộ, nhân viên (attendance_records)
/// Điểm chấm công chiếm 20% trong tổng điểm KPI (max 20 điểm)
/// </summary>
public class AttendanceRecord : BaseEntity, ISoftDeletable
{
    public long EmployeeId { get; set; }
    public virtual EmployeeProfile Employee { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }

    // Ngày công chuẩn và thực tế
    public int StandardWorkDays { get; set; } = 22;      // Ngày công chuẩn tháng (mặc định 22)
    public int ActualWorkDays { get; set; } = 22;        // Ngày công thực tế đi làm
    public int PaidLeave { get; set; } = 0;              // Nghỉ phép có lương / nghỉ lễ
    public int UnpaidLeave { get; set; } = 0;            // Nghỉ không lương
    public int SickLeave { get; set; } = 0;              // Nghỉ ốm (có chứng nhận y tế)

    // Vi phạm kỷ luật công vụ
    public int LateCount { get; set; } = 0;              // Số lần đi muộn (trừ 0.5 đ/lần)
    public int EarlyLeaveCount { get; set; } = 0;        // Số lần về sớm (trừ 0.5 đ/lần)
    public int UnauthorizedAbsence { get; set; } = 0;    // Nghỉ không phép (trừ 1 đ/ngày)
    public int MissedMeetings { get; set; } = 0;         // Vắng họp / sinh hoạt chuyên môn (trừ 0.5 đ/lần)
    public bool DisciplineViolation { get; set; } = false; // Vi phạm quy chế nội quy cơ quan (trừ 2 đ)

    // Điểm số và tỷ lệ tính lương
    public decimal AttendanceScore { get; set; } = 20.0m; // Điểm chấm công (tối đa 20 điểm)
    public decimal WorkDayRatio { get; set; } = 1.0m;     // Tỷ lệ hoàn thành ngày công = ActualWorkDays / StandardWorkDays

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Hàm tự động tính điểm Chấm công & Kỷ luật theo thang 20 điểm
    /// </summary>
    public void CalculateScore()
    {
        // 1. Đi làm đủ ngày công chuẩn (8 điểm tối đa)
        decimal workDays = Math.Min(ActualWorkDays + PaidLeave, StandardWorkDays);
        decimal dayScore = StandardWorkDays > 0 ? (8.0m * workDays / StandardWorkDays) : 8.0m;

        // 2. Không đi muộn / về sớm (4 điểm tối đa, trừ 0.5đ/lần)
        decimal punctualityScore = Math.Max(0m, 4.0m - ((LateCount + EarlyLeaveCount) * 0.5m));

        // 3. Không nghỉ không phép (4 điểm tối đa, trừ 1.0đ/lần)
        decimal unauthorizedScore = Math.Max(0m, 4.0m - (UnauthorizedAbsence * 1.0m));

        // 4. Chấp hành nội quy cơ quan (2 điểm tối đa)
        decimal disciplineScore = DisciplineViolation ? 0m : 2.0m;

        // 5. Tham gia họp / sinh hoạt đầy đủ (2 điểm tối đa, trừ 0.5đ/lần)
        decimal meetingScore = Math.Max(0m, 2.0m - (MissedMeetings * 0.5m));

        // Tổng điểm chấm công (thang 20 điểm)
        AttendanceScore = Math.Clamp(Math.Round(dayScore + punctualityScore + unauthorizedScore + disciplineScore + meetingScore, 2), 0m, 20.0m);

        // Tỷ lệ ngày công tính lương
        WorkDayRatio = StandardWorkDays > 0 ? Math.Clamp(Math.Round((decimal)ActualWorkDays / StandardWorkDays, 4), 0m, 1.5m) : 1.0m;
    }
}
