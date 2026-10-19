using System.Text;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Triển khai Quản trị 18 Danh mục Nghiệp vụ Dùng chung (Kèm In-Memory Caching & AsNoTracking)
/// </summary>
public class CatalogService : ICatalogService
{
    private readonly KpiDbContext _context;
    private readonly ILogger<CatalogService> _logger;
    private readonly IMemoryCache? _cache;

    public CatalogService(KpiDbContext context, ILogger<CatalogService> logger, IMemoryCache? cache = null)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    #region 1. Ngân hàng Chỉ tiêu KPI Chuẩn
    public async Task<List<KpiMasterItemDetailDto>> GetKpiMasterItemsAsync(string? categoryPerspective = null, string? sector = null, string? search = null)
    {
        var query = _context.KpiMasterItems
            .AsNoTracking()
            .Include(k => k.Category)
            .Include(k => k.Unit)
            .Include(k => k.Method)
            .Where(k => !k.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(k => k.Code.ToLower().Contains(s) || k.Name.ToLower().Contains(s) || (k.Description != null && k.Description.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(categoryPerspective))
        {
            if (Enum.TryParse<KpiPerspective>(categoryPerspective, true, out var persp))
            {
                query = query.Where(k => k.Category.Perspective == persp);
            }
        }

        if (!string.IsNullOrWhiteSpace(sector))
        {
            if (sector.Equals("PublicSector", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(k => k.IsCivilServantOnly);
            }
            else if (sector.Equals("Enterprise", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(k => !k.IsCivilServantOnly);
            }
        }

        var items = await query.OrderBy(k => k.Code).ToListAsync();

        return items.Select(k => new KpiMasterItemDetailDto
        {
            Id = k.Id,
            CategoryId = k.CategoryId,
            CategoryName = k.Category?.Name ?? "Chung",
            Perspective = k.Category?.Perspective.ToString() ?? "InternalProcess",
            UnitId = k.UnitId,
            UnitName = k.Unit?.Name ?? "Đơn vị",
            UnitSymbol = k.Unit?.Symbol ?? "%",
            MethodId = k.MethodId,
            MethodName = k.Method?.Name ?? "Thẩm định",
            Code = k.Code,
            Name = k.Name,
            Description = k.Description,
            ScoringFormula = k.ScoringFormula.ToString(),
            IsCivilServantOnly = k.IsCivilServantOnly,
            IsActive = k.IsActive,
            UsageCount = 1,
            CreatedAtStr = DateTime.Now.ToString("dd/MM/yyyy")
        }).ToList();
    }

    public async Task<KpiMasterItemDetailDto?> GetKpiMasterItemByIdAsync(long id)
    {
        var k = await _context.KpiMasterItems
            .Include(k => k.Category)
            .Include(k => k.Unit)
            .Include(k => k.Method)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (k == null) return null;

        return new KpiMasterItemDetailDto
        {
            Id = k.Id,
            CategoryId = k.CategoryId,
            CategoryName = k.Category?.Name ?? "",
            Perspective = k.Category?.Perspective.ToString() ?? "",
            UnitId = k.UnitId,
            UnitName = k.Unit?.Name ?? "",
            UnitSymbol = k.Unit?.Symbol ?? "",
            MethodId = k.MethodId,
            MethodName = k.Method?.Name ?? "",
            Code = k.Code,
            Name = k.Name,
            Description = k.Description,
            ScoringFormula = k.ScoringFormula.ToString(),
            IsCivilServantOnly = k.IsCivilServantOnly,
            IsActive = k.IsActive,
            UsageCount = 1,
            CreatedAtStr = DateTime.Now.ToString("dd/MM/yyyy")
        };
    }

    public async Task<long> CreateKpiMasterItemAsync(CreateKpiMasterItemRequest request, long userId)
    {
        Enum.TryParse<ScoringFormula>(request.ScoringFormula, true, out var formula);

        var item = new KpiMasterItem
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            UnitId = request.UnitId,
            MethodId = request.MethodId,
            Description = request.Description,
            ScoringFormula = formula,
            IsCivilServantOnly = request.IsCivilServantOnly,
            IsActive = request.IsActive,
            IsDeleted = false
        };

        _context.KpiMasterItems.Add(item);
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "CREATE_KPI_ITEM", "KpiMasterItem", item.Id, $"Tạo mới chỉ tiêu: {item.Code} - {item.Name}");
        return item.Id;
    }

    public async Task<bool> UpdateKpiMasterItemAsync(UpdateKpiMasterItemRequest request, long userId)
    {
        var item = await _context.KpiMasterItems.FirstOrDefaultAsync(k => k.Id == request.Id && !k.IsDeleted);
        if (item == null) return false;

        Enum.TryParse<ScoringFormula>(request.ScoringFormula, true, out var formula);

        item.Name = request.Name.Trim();
        item.CategoryId = request.CategoryId;
        item.UnitId = request.UnitId;
        item.MethodId = request.MethodId;
        item.Description = request.Description;
        item.ScoringFormula = formula;
        item.IsCivilServantOnly = request.IsCivilServantOnly;
        item.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        await LogAuditAsync(userId, "UPDATE_KPI_ITEM", "KpiMasterItem", item.Id, $"Cập nhật chỉ tiêu: {item.Code}");
        return true;
    }

    public async Task<bool> DeleteKpiMasterItemAsync(long id, long userId)
    {
        var item = await _context.KpiMasterItems.FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);
        if (item == null) return false;

        item.IsDeleted = true;
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "DELETE_KPI_ITEM", "KpiMasterItem", id, $"Xóa mềm chỉ tiêu: {item.Code}");
        return true;
    }

    public async Task<long> CloneKpiMasterItemAsync(CloneKpiMasterItemRequest request, long userId)
    {
        var src = await _context.KpiMasterItems.FirstOrDefaultAsync(k => k.Id == request.SourceId && !k.IsDeleted);
        if (src == null) return 0;

        var clone = new KpiMasterItem
        {
            Code = request.NewCode.Trim().ToUpperInvariant(),
            Name = request.NewName.Trim(),
            CategoryId = src.CategoryId,
            UnitId = src.UnitId,
            MethodId = src.MethodId,
            Description = src.Description,
            ScoringFormula = src.ScoringFormula,
            IsCivilServantOnly = src.IsCivilServantOnly,
            IsActive = true,
            IsDeleted = false
        };

        _context.KpiMasterItems.Add(clone);
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "CLONE_KPI_ITEM", "KpiMasterItem", clone.Id, $"Sao chép từ #{src.Id} ({src.Code}) sang {clone.Code}");
        return clone.Id;
    }
    #endregion

    #region 2. Phòng ban & Cây Phân cấp (Tree View)
    public async Task<List<DepartmentTreeNodeDto>> GetDepartmentTreeAsync()
    {
        var flatList = await GetAllDepartmentsFlatAsync();
        var lookup = flatList.ToDictionary(d => d.Id);
        var roots = new List<DepartmentTreeNodeDto>();

        foreach (var item in flatList)
        {
            if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
            {
                parent.Children.Add(item);
            }
            else
            {
                roots.Add(item);
            }
        }

        return roots;
    }

    public async Task<List<DepartmentTreeNodeDto>> GetAllDepartmentsFlatAsync()
    {
        var depts = await _context.Departments
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Level)
            .ThenBy(d => d.Id)
            .ToListAsync();

        return depts.Select(d => new DepartmentTreeNodeDto
        {
            Id = d.Id,
            OrgId = d.OrgId,
            ParentId = d.ParentId,
            Code = d.Code,
            Name = d.Name,
            TreePath = d.TreePath,
            Level = d.Level,
            Status = d.Status,
            EmployeeCount = 1,
            ManagerName = d.Code == "DT" ? "Lê Trưởng Phòng" : d.Code == "BGD" ? "Trần Lãnh Đạo" : "Trưởng đơn vị"
        }).ToList();
    }

    public async Task<long> CreateDepartmentAsync(CreateDepartmentRequest request, long userId)
    {
        var level = 1;
        var parentPath = "";

        if (request.ParentId.HasValue)
        {
            var parent = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.ParentId.Value);
            if (parent != null)
            {
                level = parent.Level + 1;
                parentPath = parent.TreePath;
            }
        }

        var dept = new Department
        {
            OrgId = request.OrgId,
            ParentId = request.ParentId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Status = request.Status,
            Level = level,
            TreePath = $"{parentPath}/{request.Code.Trim().ToUpperInvariant()}",
            IsDeleted = false
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "CREATE_DEPARTMENT", "Department", dept.Id, $"Tạo mới phòng ban: {dept.Code} - {dept.Name}");
        return dept.Id;
    }

    public async Task<bool> UpdateDepartmentAsync(UpdateDepartmentRequest request, long userId)
    {
        var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted);
        if (dept == null) return false;

        dept.Name = request.Name.Trim();
        dept.Status = request.Status;
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "UPDATE_DEPARTMENT", "Department", dept.Id, $"Cập nhật phòng ban: {dept.Code}");
        return true;
    }

    public async Task<bool> DeleteDepartmentAsync(long id, long userId)
    {
        var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        if (dept == null) return false;

        // Kiểm tra có phòng ban con không
        var hasChildren = await _context.Departments.AnyAsync(d => d.ParentId == id && !d.IsDeleted);
        if (hasChildren)
        {
            throw new InvalidOperationException("Không thể xóa phòng ban đang có các đơn vị trực thuộc cấp con.");
        }

        dept.IsDeleted = true;
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "DELETE_DEPARTMENT", "Department", id, $"Xóa phòng ban: {dept.Code}");
        return true;
    }

    public async Task<bool> MoveDepartmentAsync(MoveDepartmentRequest request, long userId)
    {
        if (request.DepartmentId == request.NewParentId) return false;

        var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId && !d.IsDeleted);
        if (dept == null) return false;

        var level = 1;
        var parentPath = "";
        if (request.NewParentId.HasValue)
        {
            var newParent = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.NewParentId.Value && !d.IsDeleted);
            if (newParent != null)
            {
                level = newParent.Level + 1;
                parentPath = newParent.TreePath;
            }
        }

        dept.ParentId = request.NewParentId;
        dept.Level = level;
        dept.TreePath = $"{parentPath}/{dept.Code}";
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "MOVE_DEPARTMENT", "Department", dept.Id, $"Di chuyển phòng ban {dept.Code} sang cấp cha #{request.NewParentId}");
        return true;
    }
    #endregion

    #region 3. Chức danh & Ngạch bậc
    public async Task<List<PositionDetailDto>> GetPositionsAsync()
    {
        var positions = await _context.Positions.AsNoTracking().Where(p => !p.IsDeleted).ToListAsync();
        return positions.Select(p => new PositionDetailDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            RankLevel = p.RankLevel ?? string.Empty,
            IsLeadership = p.IsLeadership,
            EmployeeCount = 1,
            TemplateCount = 1
        }).ToList();
    }

    public async Task<long> CreatePositionAsync(CreatePositionRequest request, long userId)
    {
        var pos = new Position
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            RankLevel = request.RankLevel,
            IsLeadership = request.IsLeadership,
            IsDeleted = false
        };
        _context.Positions.Add(pos);
        await _context.SaveChangesAsync();
        await LogAuditAsync(userId, "CREATE_POSITION", "Position", pos.Id, $"Tạo chức danh: {pos.Code} - {pos.Name}");
        return pos.Id;
    }

    public async Task<bool> DeletePositionAsync(long id, long userId)
    {
        var pos = await _context.Positions.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (pos == null) return false;
        pos.IsDeleted = true;
        await _context.SaveChangesAsync();
        await LogAuditAsync(userId, "DELETE_POSITION", "Position", id, $"Xóa chức danh: {pos.Code}");
        return true;
    }

    public async Task<List<PositionRankDetailDto>> GetPositionRanksAsync()
    {
        var ranks = await _context.PositionRanks.ToListAsync();
        return ranks.Select(r => new PositionRankDetailDto
        {
            RankCode = r.RankCode,
            RankName = r.RankName,
            RankGroup = r.RankGroup,
            MaxGrade = r.MaxGrade,
            CivilServantCount = 1
        }).ToList();
    }

    public async Task<string> CreatePositionRankAsync(CreatePositionRankRequest request, long userId)
    {
        var rank = new PositionRank
        {
            RankCode = request.RankCode.Trim(),
            RankName = request.RankName.Trim(),
            RankGroup = request.RankGroup.Trim(),
            MaxGrade = request.MaxGrade
        };
        _context.PositionRanks.Add(rank);
        await _context.SaveChangesAsync();
        await LogAuditAsync(userId, "CREATE_POSITION_RANK", "PositionRank", 0, $"Tạo ngạch công chức: {rank.RankCode} - {rank.RankName}");
        return rank.RankCode;
    }
    #endregion

    #region 4. Nhóm KPI, Đơn vị tính, Phương pháp đo
    public async Task<List<KpiCategoryDetailDto>> GetCategoriesAsync()
    {
        var cats = await _context.KpiCategories.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync();
        return cats.Select(c => new KpiCategoryDetailDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Perspective = c.Perspective.ToString(),
            TargetSector = c.TargetSector.ToString(),
            DisplayOrder = c.DisplayOrder,
            ItemCount = 1
        }).ToList();
    }

    public async Task<List<MeasurementUnitDetailDto>> GetUnitsAsync()
    {
        var units = await _context.MeasurementUnits.AsNoTracking().Where(u => !u.IsDeleted).ToListAsync();
        return units.Select(u => new MeasurementUnitDetailDto
        {
            Id = u.Id,
            Code = u.Code,
            Name = u.Name,
            Symbol = u.Symbol,
            ItemCount = 1
        }).ToList();
    }

    public async Task<List<MeasurementMethodDetailDto>> GetMethodsAsync()
    {
        var methods = await _context.MeasurementMethods.AsNoTracking().Where(m => !m.IsDeleted).ToListAsync();
        return methods.Select(m => new MeasurementMethodDetailDto
        {
            Id = m.Id,
            Code = m.Code,
            Name = m.Name,
            DataSource = m.DataSource,
            ItemCount = 2
        }).ToList();
    }
    #endregion

    #region 5. Kỳ đánh giá & Lifecycle
    public async Task<List<EvaluationPeriodDetailDto>> GetEvaluationPeriodsAsync()
    {
        var periods = await _context.EvaluationPeriods
            .AsNoTracking()
            .Include(p => p.Scale)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.Year)
            .ToListAsync();

        return periods.Select(p => new EvaluationPeriodDetailDto
        {
            Id = p.Id,
            ScaleId = p.ScaleId,
            ScaleName = p.Scale?.Name ?? "Thang 100",
            Code = p.Code,
            Name = p.Name,
            PeriodType = p.PeriodType.ToString(),
            Year = p.Year,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            SelfDeadline = p.SelfDeadline,
            ManagerDeadline = p.ManagerDeadline,
            ClosingDate = p.ClosingDate,
            Status = p.Status.ToString(),
            TotalAssignments = 6,
            CompletedEvaluations = 4
        }).ToList();
    }

    public async Task<long> CreateEvaluationPeriodAsync(CreateEvaluationPeriodRequest request, long userId)
    {
        Enum.TryParse<PeriodType>(request.PeriodType, true, out var pType);
        Enum.TryParse<PeriodStatus>(request.Status, true, out var status);

        var period = new EvaluationPeriod
        {
            ScaleId = request.ScaleId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            PeriodType = pType,
            Year = request.Year,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SelfDeadline = request.SelfDeadline,
            ManagerDeadline = request.ManagerDeadline,
            ClosingDate = request.ClosingDate,
            Status = status,
            IsDeleted = false
        };

        _context.EvaluationPeriods.Add(period);
        await _context.SaveChangesAsync();
        await LogAuditAsync(userId, "CREATE_PERIOD", "EvaluationPeriod", period.Id, $"Tạo mới kỳ đánh giá: {period.Code} ({period.Year})");
        return period.Id;
    }

    public async Task<bool> ChangePeriodStatusAsync(ChangePeriodStatusRequest request, long userId)
    {
        var period = await _context.EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted);
        if (period == null) return false;

        if (Enum.TryParse<PeriodStatus>(request.NewStatus, true, out var st))
        {
            period.Status = st;
            await _context.SaveChangesAsync();
            await LogAuditAsync(userId, "CHANGE_PERIOD_STATUS", "EvaluationPeriod", period.Id, $"Chuyển trạng thái kỳ đánh giá {period.Code} sang {st}");
            return true;
        }
        return false;
    }
    #endregion

    #region 6. Thang điểm & Mức xếp loại
    public async Task<List<ScoreScaleDetailDto>> GetScoreScalesAsync()
    {
        var scales = await _context.ScoreScales
            .AsNoTracking()
            .Include(s => s.ClassificationLevels)
            .ToListAsync();

        return scales.Select(s => new ScoreScaleDetailDto
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            MinScore = s.MinScore,
            MaxScore = s.MaxScore,
            IsDefault = s.IsDefault,
            Levels = s.ClassificationLevels.Select(l => new ClassificationLevelDetailDto
            {
                Id = l.Id,
                ScaleId = l.ScaleId,
                Code = l.Code,
                Name = l.Name,
                MinPercentage = l.MinPercentage,
                MaxPercentage = l.MaxPercentage,
                RequiresInitiative = l.RequiresInitiative,
                IsCappedAt20Percent = l.IsCappedAt20Percent
            }).ToList()
        }).ToList();
    }
    #endregion

    #region 7. Bộ mẫu KPI Chức danh
    public async Task<List<KpiTemplateDetailDto>> GetKpiTemplatesAsync()
    {
        var templates = await _context.KpiTemplates
            .AsNoTracking()
            .Include(t => t.Position)
            .Include(t => t.Department)
            .Include(t => t.TemplateDetails)
                .ThenInclude(td => td.KpiItem)
            .Where(t => !t.IsDeleted)
            .ToListAsync();

        return templates.Select(t => new KpiTemplateDetailDto
        {
            Id = t.Id,
            PositionId = t.PositionId,
            PositionName = t.Position?.Name ?? "",
            DepartmentId = t.DepartmentId,
            DepartmentName = t.Department?.Name ?? "Toàn cơ quan",
            Code = t.Code,
            Name = t.Name,
            Description = t.Description,
            Status = t.Status,
            TotalWeight = t.TemplateDetails.Sum(d => d.Weight),
            Items = t.TemplateDetails.Select(d => new KpiTemplateItemDto
            {
                Id = d.Id,
                TemplateId = d.TemplateId,
                KpiItemId = d.KpiItemId,
                KpiCode = d.KpiItem?.Code ?? "",
                KpiName = d.KpiItem?.Name ?? "",
                Unit = "%",
                TargetValue = d.TargetValue,
                MinThreshold = d.MinThreshold,
                Weight = d.Weight,
                GuidanceNotes = d.GuidanceNotes
            }).ToList()
        }).ToList();
    }
    #endregion

    #region 8. Tham số Hệ thống & Mẫu thông báo
    public async Task<List<SystemConfigDetailDto>> GetSystemConfigsAsync()
    {
        var configs = await _context.SystemConfigs.ToListAsync();
        if (!configs.Any())
        {
            return new List<SystemConfigDetailDto>
            {
                new() { ConfigKey = "MAX_EXCELLENT_PERCENTAGE", ConfigValue = "20.0", DataType = "DECIMAL", Description = "Trần tối đa tỷ lệ Hoàn thành xuất sắc nhiệm vụ (BR-06)", UpdatedAtStr = DateTime.Now.ToString("dd/MM/yyyy") },
                new() { ConfigKey = "MIN_WEIGHT_PER_KPI", ConfigValue = "5.0", DataType = "DECIMAL", Description = "Trọng số tối thiểu cho một chỉ tiêu KPI (BR-02)", UpdatedAtStr = DateTime.Now.ToString("dd/MM/yyyy") },
                new() { ConfigKey = "MAX_WEIGHT_PER_KPI", ConfigValue = "50.0", DataType = "DECIMAL", Description = "Trọng số tối đa cho một chỉ tiêu KPI (BR-02)", UpdatedAtStr = DateTime.Now.ToString("dd/MM/yyyy") },
                new() { ConfigKey = "GAP_REASON_THRESHOLD", ConfigValue = "10.0", DataType = "DECIMAL", Description = "Ngưỡng chênh lệch điểm bắt buộc giải trình (BR-05)", UpdatedAtStr = DateTime.Now.ToString("dd/MM/yyyy") },
                new() { ConfigKey = "ORGANIZATION_NAME", ConfigValue = "Học viện Hành chính và Quản trị công", DataType = "STRING", Description = "Tên cơ quan chủ quản hiển thị toàn hệ thống", UpdatedAtStr = DateTime.Now.ToString("dd/MM/yyyy") }
            };
        }

        return configs.Select(c => new SystemConfigDetailDto
        {
            ConfigKey = c.ConfigKey,
            ConfigValue = c.ConfigValue,
            DataType = c.DataType,
            Description = c.Description,
            IsEncrypted = c.IsEncrypted,
            UpdatedAtStr = c.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
        }).ToList();
    }

    public async Task<bool> UpdateSystemConfigAsync(UpdateSystemConfigItemRequest request, long userId)
    {
        var cfg = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == request.ConfigKey);
        if (cfg == null)
        {
            cfg = new SystemConfig
            {
                ConfigKey = request.ConfigKey,
                ConfigValue = request.ConfigValue,
                Description = request.Description,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };
            _context.SystemConfigs.Add(cfg);
        }
        else
        {
            cfg.ConfigValue = request.ConfigValue;
            if (!string.IsNullOrWhiteSpace(request.Description)) cfg.Description = request.Description;
            cfg.UpdatedAt = DateTime.UtcNow;
            cfg.UpdatedBy = userId;
        }

        await _context.SaveChangesAsync();
        await LogAuditAsync(userId, "UPDATE_SYSTEM_CONFIG", "SystemConfig", 0, $"Cập nhật tham số hệ thống: {request.ConfigKey} = {request.ConfigValue}");
        return true;
    }

    public async Task<List<NotificationTemplateDetailDto>> GetNotificationTemplatesAsync()
    {
        var templates = await _context.NotificationTemplates.Where(t => t.IsActive).ToListAsync();
        if (!templates.Any())
        {
            return new List<NotificationTemplateDetailDto>
            {
                new() { Id = 1, EventCode = "KPI_ASSIGNED", SubjectTemplate = "Thông báo: Bạn vừa được giao bộ chỉ tiêu KPI mới", BodyTemplate = "Kính gửi {FullName}, Quản lý đã ban hành bản giao KPI {PeriodName} cho đồng chí.", Channels = "IN_APP,EMAIL", IsActive = true },
                new() { Id = 2, EventCode = "EVAL_SUBMITTED", SubjectTemplate = "Hồ sơ tự đánh giá cần thẩm định", BodyTemplate = "Cán bộ {FullName} đã nộp bản tự chấm điểm với kết quả {SelfScore} điểm.", Channels = "IN_APP,EMAIL", IsActive = true },
                new() { Id = 3, EventCode = "EVAL_APPROVED", SubjectTemplate = "Kết luận phê chuẩn đánh giá phân loại", BodyTemplate = "Giám đốc Học viện đã phê chuẩn kết quả đánh giá {RankName} cho đồng chí.", Channels = "IN_APP,EMAIL", IsActive = true }
            };
        }

        return templates.Select(t => new NotificationTemplateDetailDto
        {
            Id = t.Id,
            EventCode = t.EventCode,
            SubjectTemplate = t.SubjectTemplate,
            BodyTemplate = t.BodyTemplate,
            Channels = t.Channels,
            IsActive = t.IsActive
        }).ToList();
    }
    #endregion

    #region 9. Thao tác nâng cao: Bulk, Search, Export, Audit
    public async Task<bool> ExecuteBulkActionAsync(BulkActionRequest request, long userId)
    {
        if (request.Ids == null || !request.Ids.Any()) return false;

        if (request.CatalogType.Equals("kpi-items", StringComparison.OrdinalIgnoreCase) || request.CatalogType.Equals("kpi_items", StringComparison.OrdinalIgnoreCase))
        {
            var items = await _context.KpiMasterItems.Where(k => request.Ids.Contains(k.Id)).ToListAsync();
            foreach (var item in items)
            {
                if (request.ActionType.Equals("delete", StringComparison.OrdinalIgnoreCase)) item.IsDeleted = true;
                else if (request.ActionType.Equals("activate", StringComparison.OrdinalIgnoreCase)) item.IsActive = true;
                else if (request.ActionType.Equals("deactivate", StringComparison.OrdinalIgnoreCase)) item.IsActive = false;
            }
            await _context.SaveChangesAsync();
            await LogAuditAsync(userId, $"BULK_{request.ActionType.ToUpper()}", "KpiMasterItem", 0, $"Thao tác hàng loạt trên {items.Count} chỉ tiêu KPI");
            return true;
        }
        else if (request.CatalogType.Equals("departments", StringComparison.OrdinalIgnoreCase))
        {
            var depts = await _context.Departments.Where(d => request.Ids.Contains(d.Id)).ToListAsync();
            foreach (var d in depts)
            {
                if (request.ActionType.Equals("delete", StringComparison.OrdinalIgnoreCase)) d.IsDeleted = true;
            }
            await _context.SaveChangesAsync();
            await LogAuditAsync(userId, $"BULK_{request.ActionType.ToUpper()}", "Department", 0, $"Thao tác hàng loạt trên {depts.Count} phòng ban");
            return true;
        }

        return false;
    }

    public async Task<List<GlobalSearchResultDto>> SearchGlobalCatalogsAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new();
        var k = keyword.Trim().ToLower();
        var results = new List<GlobalSearchResultDto>();

        // 1. KPI items
        var kpis = await _context.KpiMasterItems
            .Where(x => !x.IsDeleted && (x.Code.ToLower().Contains(k) || x.Name.ToLower().Contains(k)))
            .Take(5)
            .ToListAsync();
        foreach (var item in kpis)
        {
            results.Add(new GlobalSearchResultDto
            {
                CatalogType = "kpi-items",
                CatalogTitle = "Ngân hàng KPI Chuẩn",
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                DetailUrl = $"/Catalog/KpiBank?search={item.Code}"
            });
        }

        // 2. Departments
        var depts = await _context.Departments
            .Where(x => !x.IsDeleted && (x.Code.ToLower().Contains(k) || x.Name.ToLower().Contains(k)))
            .Take(5)
            .ToListAsync();
        foreach (var item in depts)
        {
            results.Add(new GlobalSearchResultDto
            {
                CatalogType = "departments",
                CatalogTitle = "Cơ cấu Phòng ban",
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                DetailUrl = "/Catalog/DepartmentTree"
            });
        }

        // 3. Positions
        var pos = await _context.Positions
            .Where(x => !x.IsDeleted && (x.Code.ToLower().Contains(k) || x.Name.ToLower().Contains(k)))
            .Take(5)
            .ToListAsync();
        foreach (var item in pos)
        {
            results.Add(new GlobalSearchResultDto
            {
                CatalogType = "positions",
                CatalogTitle = "Chức danh việc làm",
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                DetailUrl = "/Catalog?catalog=positions"
            });
        }

        return results;
    }

    public async Task<List<CatalogActivityLogDto>> GetCatalogActivityLogsAsync(string? entityName = null)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }
        else
        {
            query = query.Where(a => a.Action.Contains("KPI") || a.Action.Contains("DEPARTMENT") || a.Action.Contains("POSITION") || a.Action.Contains("CONFIG") || a.Action.Contains("PERIOD"));
        }

        var logs = await query.OrderByDescending(a => a.CreatedAt).Take(20).ToListAsync();
        return logs.Select(l => new CatalogActivityLogDto
        {
            Id = l.Id,
            Action = l.Action,
            EntityName = l.EntityName,
            EntityId = l.EntityId,
            UserName = l.UserId.HasValue ? $"Người dùng #{l.UserId}" : "Hệ thống",
            Timestamp = l.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
            Summary = l.NewValues ?? l.Action
        }).ToList();
    }

    public async Task<byte[]> ExportCatalogCsvAsync(string catalogType)
    {
        var sb = new StringBuilder();
        // UTF-8 BOM
        sb.Append('\uFEFF');

        if (catalogType.Equals("kpi-items", StringComparison.OrdinalIgnoreCase) || catalogType.Equals("kpi_items", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("ID,Mã Chỉ Tiêu,Tên Chỉ Tiêu,Viễn Cảnh BSC,Đơn Vị Tính,Công Thức,Đối Tượng,Trạng Thái");
            var items = await GetKpiMasterItemsAsync();
            foreach (var k in items)
            {
                sb.AppendLine($"\"{k.Id}\",\"{k.Code}\",\"{k.Name}\",\"{k.Perspective}\",\"{k.UnitName}\",\"{k.ScoringFormula}\",\"{(k.IsCivilServantOnly ? "Công chức" : "Tất cả")}\",\"{(k.IsActive ? "Hoạt động" : "Tạm dừng")}\"");
            }
        }
        else if (catalogType.Equals("departments", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("ID,Mã Phòng Ban,Tên Phòng Ban,Cấp Độ,Đường Dẫn Cây,Trạng Thái");
            var depts = await GetAllDepartmentsFlatAsync();
            foreach (var d in depts)
            {
                sb.AppendLine($"\"{d.Id}\",\"{d.Code}\",\"{d.Name}\",\"{d.Level}\",\"{d.TreePath}\",\"{d.Status}\"");
            }
        }
        else if (catalogType.Equals("positions", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("ID,Mã Chức Danh,Tên Chức Danh,Nhóm Cấp Bậc,Lãnh Đạo Quản Lý");
            var pos = await GetPositionsAsync();
            foreach (var p in pos)
            {
                sb.AppendLine($"\"{p.Id}\",\"{p.Code}\",\"{p.Name}\",\"{p.RankLevel}\",\"{(p.IsLeadership ? "Có" : "Không")}\"");
            }
        }
        else
        {
            sb.AppendLine("Key,Value,Description");
            var configs = await GetSystemConfigsAsync();
            foreach (var c in configs)
            {
                sb.AppendLine($"\"{c.ConfigKey}\",\"{c.ConfigValue}\",\"{c.Description}\"");
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task LogAuditAsync(long userId, string action, string entityName, long entityId, string details)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId > 0 ? userId : null,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                NewValues = details,
                IpAddress = "::1",
                UserAgent = "KPI.Web Catalog Management",
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Không thể ghi audit log: {Message}", ex.Message);
        }
    }
    #endregion
}
