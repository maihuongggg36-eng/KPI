using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ Quản lý Mục tiêu do Ban Giám đốc đặt ra (Director Stretch Targets)
/// Nguyên tắc: Nhân viên phải VƯỢT qua mức mục tiêu BGĐ mới được xếp loại Tốt (K=1.15) hoặc Xuất sắc (K=1.3)
/// </summary>
public interface IDirectorTargetService
{
    Task<List<DirectorTargetDto>> GetTargetsAsync(long periodId, long? departmentId = null);
    Task<DirectorTargetDto?> GetTargetByIdAsync(long targetId);
    Task<bool> SaveTargetAsync(CreateDirectorTargetRequest request, long directorId);
    Task<bool> DeleteTargetAsync(long targetId);
}
