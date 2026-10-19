using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Planning;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Dịch vụ Quản lý Mục tiêu do Ban Giám đốc đặt ra (Director Stretch Targets)
/// </summary>
public class DirectorTargetService : IDirectorTargetService
{
    private readonly KpiDbContext _context;
    private readonly ILogger<DirectorTargetService> _logger;

    public DirectorTargetService(KpiDbContext context, ILogger<DirectorTargetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<DirectorTargetDto>> GetTargetsAsync(long periodId, long? departmentId = null)
    {
        var query = _context.DirectorTargets
            .Include(t => t.Period)
            .Include(t => t.Department)
            .Include(t => t.CreatedByUser)
            .Where(t => t.PeriodId == periodId && t.IsActive)
            .AsNoTracking();

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(t => t.DepartmentId == null || t.DepartmentId == departmentId.Value);
        }

        var list = await query.ToListAsync();

        return list.Select(t => new DirectorTargetDto
        {
            Id = t.Id,
            PeriodId = t.PeriodId,
            PeriodName = t.Period?.Name ?? "",
            DepartmentId = t.DepartmentId,
            DepartmentName = t.Department?.Name ?? "Toàn Học viện",
            TargetName = t.TargetName,
            TargetValue = t.TargetValue,
            ChallengeValue = t.ChallengeValue,
            TargetUnit = t.TargetUnit,
            Description = t.Description,
            CreatedByName = t.CreatedByUser?.Username ?? "Ban Giám đốc",
            CreatedAt = t.CreatedAt,
            IsActive = t.IsActive
        }).OrderBy(t => t.DepartmentId).ThenBy(t => t.TargetName).ToList();
    }

    public async Task<DirectorTargetDto?> GetTargetByIdAsync(long targetId)
    {
        var t = await _context.DirectorTargets
            .Include(x => x.Period)
            .Include(x => x.Department)
            .Include(x => x.CreatedByUser)
            .FirstOrDefaultAsync(x => x.Id == targetId);

        if (t == null) return null;

        return new DirectorTargetDto
        {
            Id = t.Id,
            PeriodId = t.PeriodId,
            PeriodName = t.Period?.Name ?? "",
            DepartmentId = t.DepartmentId,
            DepartmentName = t.Department?.Name ?? "Toàn Học viện",
            TargetName = t.TargetName,
            TargetValue = t.TargetValue,
            ChallengeValue = t.ChallengeValue,
            TargetUnit = t.TargetUnit,
            Description = t.Description,
            CreatedByName = t.CreatedByUser?.Username ?? "Ban Giám đốc",
            CreatedAt = t.CreatedAt,
            IsActive = t.IsActive
        };
    }

    public async Task<bool> SaveTargetAsync(CreateDirectorTargetRequest request, long directorId)
    {
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            var target = await _context.DirectorTargets.FirstOrDefaultAsync(t => t.Id == request.Id.Value);
            if (target == null) return false;

            target.TargetName = request.TargetName.Trim();
            target.TargetValue = request.TargetValue;
            target.ChallengeValue = request.ChallengeValue;
            target.TargetUnit = request.TargetUnit.Trim();
            target.Description = request.Description?.Trim();
            target.DepartmentId = request.DepartmentId > 0 ? request.DepartmentId : null;
        }
        else
        {
            var target = new DirectorTarget
            {
                PeriodId = request.PeriodId,
                DepartmentId = request.DepartmentId > 0 ? request.DepartmentId : null,
                TargetName = request.TargetName.Trim(),
                TargetValue = request.TargetValue,
                ChallengeValue = request.ChallengeValue,
                TargetUnit = request.TargetUnit.Trim(),
                Description = request.Description?.Trim(),
                CreatedBy = directorId
            };
            _context.DirectorTargets.Add(target);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Lãnh đạo {DirectorId} đã lưu mục tiêu BGĐ: '{TargetName}' ({TargetValue} {TargetUnit})",
            directorId, request.TargetName, request.TargetValue, request.TargetUnit);

        return true;
    }

    public async Task<bool> DeleteTargetAsync(long targetId)
    {
        var target = await _context.DirectorTargets.FirstOrDefaultAsync(t => t.Id == targetId);
        if (target == null) return false;

        target.IsDeleted = true;
        target.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
