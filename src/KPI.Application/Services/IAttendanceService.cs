using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ Quản lý Chấm công và Kỷ luật lao động (Thành phần 20% trong tổng điểm KPI)
/// </summary>
public interface IAttendanceService
{
    Task<List<AttendanceDto>> GetMonthlyAttendanceAsync(int year, int month, long? departmentId = null);
    Task<AttendanceDto?> GetEmployeeAttendanceAsync(long employeeId, int year, int month);
    Task<bool> SaveAttendanceRecordAsync(AttendanceInputDto input, long currentUserId);
    Task<int> AutoGenerateMonthlyAttendanceAsync(int year, int month);
}
