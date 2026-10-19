using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Evaluation;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Dịch vụ Quản lý Chấm công và Kỷ luật lao động (20% điểm KPI)
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly KpiDbContext _context;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(KpiDbContext context, ILogger<AttendanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AttendanceDto>> GetMonthlyAttendanceAsync(int year, int month, long? departmentId = null)
    {
        // Tự động sinh dữ liệu chấm công nếu chưa có
        await AutoGenerateMonthlyAttendanceAsync(year, month);

        var query = _context.AttendanceRecords
            .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
            .Include(a => a.Employee)
                .ThenInclude(e => e.Position)
            .Where(a => a.Year == year && a.Month == month)
            .AsNoTracking();

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(a => a.Employee.DepartmentId == departmentId.Value);
        }

        var list = await query.ToListAsync();

        return list.Select(a => new AttendanceDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            StaffCode = a.Employee?.StaffCode ?? "",
            FullName = a.Employee?.FullName ?? "",
            DepartmentName = a.Employee?.Department?.Name ?? "",
            PositionName = a.Employee?.Position?.Name ?? "",
            Year = a.Year,
            Month = a.Month,
            StandardWorkDays = a.StandardWorkDays,
            ActualWorkDays = a.ActualWorkDays,
            PaidLeave = a.PaidLeave,
            UnpaidLeave = a.UnpaidLeave,
            SickLeave = a.SickLeave,
            LateCount = a.LateCount,
            EarlyLeaveCount = a.EarlyLeaveCount,
            UnauthorizedAbsence = a.UnauthorizedAbsence,
            MissedMeetings = a.MissedMeetings,
            DisciplineViolation = a.DisciplineViolation,
            AttendanceScore = a.AttendanceScore,
            WorkDayRatio = a.WorkDayRatio,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt
        }).OrderBy(a => a.StaffCode).ToList();
    }

    public async Task<AttendanceDto?> GetEmployeeAttendanceAsync(long employeeId, int year, int month)
    {
        var a = await _context.AttendanceRecords
            .Include(x => x.Employee)
                .ThenInclude(e => e.Department)
            .Include(x => x.Employee)
                .ThenInclude(e => e.Position)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == year && x.Month == month);

        if (a == null) return null;

        return new AttendanceDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            StaffCode = a.Employee?.StaffCode ?? "",
            FullName = a.Employee?.FullName ?? "",
            DepartmentName = a.Employee?.Department?.Name ?? "",
            PositionName = a.Employee?.Position?.Name ?? "",
            Year = a.Year,
            Month = a.Month,
            StandardWorkDays = a.StandardWorkDays,
            ActualWorkDays = a.ActualWorkDays,
            PaidLeave = a.PaidLeave,
            UnpaidLeave = a.UnpaidLeave,
            SickLeave = a.SickLeave,
            LateCount = a.LateCount,
            EarlyLeaveCount = a.EarlyLeaveCount,
            UnauthorizedAbsence = a.UnauthorizedAbsence,
            MissedMeetings = a.MissedMeetings,
            DisciplineViolation = a.DisciplineViolation,
            AttendanceScore = a.AttendanceScore,
            WorkDayRatio = a.WorkDayRatio,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt
        };
    }

    public async Task<bool> SaveAttendanceRecordAsync(AttendanceInputDto input, long currentUserId)
    {
        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == input.EmployeeId && a.Year == input.Year && a.Month == input.Month);

        if (record == null)
        {
            record = new AttendanceRecord
            {
                EmployeeId = input.EmployeeId,
                Year = input.Year,
                Month = input.Month
            };
            _context.AttendanceRecords.Add(record);
        }

        record.StandardWorkDays = input.StandardWorkDays;
        record.ActualWorkDays = input.ActualWorkDays;
        record.PaidLeave = input.PaidLeave;
        record.UnpaidLeave = input.UnpaidLeave;
        record.SickLeave = input.SickLeave;
        record.LateCount = input.LateCount;
        record.EarlyLeaveCount = input.EarlyLeaveCount;
        record.UnauthorizedAbsence = input.UnauthorizedAbsence;
        record.MissedMeetings = input.MissedMeetings;
        record.DisciplineViolation = input.DisciplineViolation;
        record.Notes = input.Notes;
        record.UpdatedAt = DateTime.UtcNow;

        // Tính điểm tự động theo thang 20 điểm
        record.CalculateScore();

        await _context.SaveChangesAsync();
        _logger.LogInformation("Người dùng {UserId} đã cập nhật chấm công tháng {Month}/{Year} cho Nhân viên {EmpId}. Điểm: {Score}/20",
            currentUserId, input.Month, input.Year, input.EmployeeId, record.AttendanceScore);

        return true;
    }

    public async Task<int> AutoGenerateMonthlyAttendanceAsync(int year, int month)
    {
        var employees = await _context.EmployeeProfiles
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        var existingRecordEmployeeIds = (await _context.AttendanceRecords
            .Where(a => a.Year == year && a.Month == month)
            .Select(a => a.EmployeeId)
            .ToListAsync())
            .ToHashSet();

        int generatedCount = 0;
        foreach (var emp in employees)
        {
            if (!existingRecordEmployeeIds.Contains(emp.Id))
            {
                var newRecord = new AttendanceRecord
                {
                    EmployeeId = emp.Id,
                    Year = year,
                    Month = month,
                    StandardWorkDays = 22,
                    ActualWorkDays = 22,
                    PaidLeave = 0,
                    UnpaidLeave = 0,
                    SickLeave = 0,
                    LateCount = 0,
                    EarlyLeaveCount = 0,
                    UnauthorizedAbsence = 0,
                    MissedMeetings = 0,
                    DisciplineViolation = false,
                    Notes = "Khởi tạo dữ liệu công chuẩn ban đầu"
                };
                newRecord.CalculateScore();
                _context.AttendanceRecords.Add(newRecord);
                generatedCount++;
            }
        }

        if (generatedCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Đã tự động khởi tạo {Count} bản ghi chấm công tháng {Month}/{Year}", generatedCount, month, year);
        }

        return generatedCount;
    }
}
