using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Thực thi ISearchService: Tìm kiếm đa chiều phục vụ Command Palette (Ctrl+K)
/// </summary>
public class SearchService : ISearchService
{
    private readonly KpiDbContext _context;

    public SearchService(KpiDbContext context)
    {
        _context = context;
    }

    public async Task<SearchResultDto> QuickSearchAsync(string query, long currentUserId, string role)
    {
        var result = new SearchResultDto { Query = query };
        var q = (query ?? string.Empty).Trim().ToLower();

        // 1. Nhóm Trang hệ thống & Hành động nhanh (luôn gợi ý nếu query rỗng hoặc khớp)
        var systemPages = new List<SearchItemDto>
        {
            new() { Category = "Trang hệ thống", Title = "Bảng điều khiển Giám đốc (Dashboard)", Subtitle = "Tổng quan hiệu suất, biểu đồ radar & donut", Url = "/Dashboard", Icon = "bi-speedometer2", BadgeText = "Trang chủ", BadgeClass = "bg-primary" },
            new() { Category = "Trang hệ thống", Title = "Giao chỉ tiêu KPI & Ký cam kết", Subtitle = "Phân bổ trọng số 100%, chọn từ ngân hàng chỉ tiêu", Url = "/KpiPlanning/Index", Icon = "bi-journal-plus", BadgeText = "Giao KPI", BadgeClass = "bg-info" },
            new() { Category = "Trang hệ thống", Title = "Phiếu Tự đánh giá KPI (Self-Assessment)", Subtitle = "Chấm điểm hoàn thành nhiệm vụ, đính kèm minh chứng", Url = "/Evaluation/SelfAssess", Icon = "bi-pencil-square", BadgeText = "Tự chấm", BadgeClass = "bg-success" },
            new() { Category = "Trang hệ thống", Title = "Quản lý Thẩm định Điểm KPI", Subtitle = "Duyệt điểm cấp phòng ban, giải trình chênh lệch", Url = "/Evaluation/ManagerList", Icon = "bi-clipboard-check", BadgeText = "Thẩm định", BadgeClass = "bg-warning" },
            new() { Category = "Trang hệ thống", Title = "Xếp loại Cán bộ Công chức (NĐ 90/48)", Subtitle = "Theo dõi tỷ lệ trần 20%, xuất biểu mẫu Mẫu 01/02 A4", Url = "/CivilServant/Index", Icon = "bi-patch-check", BadgeText = "Nghị định 90", BadgeClass = "bg-danger" },
            new() { Category = "Trang hệ thống", Title = "Tính lương KPI (P3) & Chi tiền mặt", Subtitle = "Bảng tính lương 3P, phiếu chi Mẫu 02-TT, ký nhận 05a", Url = "/Payroll/Index", Icon = "bi-cash-stack", BadgeText = "Lương 3P", BadgeClass = "bg-success" },
            new() { Category = "Trang hệ thống", Title = "Báo cáo Tổng hợp & Phân tích Đa chiều", Subtitle = "Biểu đồ chuông Gauss, phân tích xu hướng, xuất Excel/PDF", Url = "/Report/Index", Icon = "bi-graph-up-arrow", BadgeText = "Báo cáo", BadgeClass = "bg-primary" },
            new() { Category = "Trang hệ thống", Title = "Bảng Xếp Hạng Hiệu Suất (Leaderboard)", Subtitle = "Vinh danh Top 3 cán bộ, bảng vàng điểm số toàn viện", Url = "/Report/Leaderboard", Icon = "bi-trophy", BadgeText = "BXH", BadgeClass = "bg-warning" },
            new() { Category = "Trang hệ thống", Title = "Phân Tích Chuông Gauss Chuyên Sâu", Subtitle = "Kiểm soát độ lệch chuẩn và trần 20% Xuất sắc", Url = "/Report/Gauss", Icon = "bi-bell", BadgeText = "Gauss", BadgeClass = "bg-info" },
            new() { Category = "Trang hệ thống", Title = "Quản trị Danh mục Hệ thống", Subtitle = "18 danh mục dùng chung, ngân hàng KPI, phòng ban", Url = "/Catalog/Index", Icon = "bi-folder2-open", BadgeText = "Danh mục", BadgeClass = "bg-secondary" },
            new() { Category = "Trang hệ thống", Title = "Thông tin cá nhân & Cài đặt giao diện", Subtitle = "Đổi mật khẩu, avatar, theme Dark/Light, thông báo", Url = "/Profile", Icon = "bi-person-circle", BadgeText = "Cá nhân", BadgeClass = "bg-dark" }
        };

        var quickActions = new List<SearchItemDto>
        {
            new() { Category = "Hành động nhanh", Title = "Giao KPI cho nhân viên mới", Subtitle = "Mở form giao chỉ tiêu và phân bổ trọng số", Url = "/KpiPlanning/Assign", Icon = "bi-plus-circle", BadgeText = "Tạo mới", BadgeClass = "bg-success" },
            new() { Category = "Hành động nhanh", Title = "Xuất file Báo cáo Tổng hợp Excel (.xlsx)", Subtitle = "Tải bảng tổng hợp hiệu suất định dạng ClosedXML", Url = "/Report/ExportExcel?periodId=1", Icon = "bi-file-earmark-excel", BadgeText = "Tải về", BadgeClass = "bg-success" },
            new() { Category = "Hành động nhanh", Title = "Xuất Báo cáo Kết quả PDF (.pdf)", Subtitle = "Tải báo cáo in A4 định dạng QuestPDF", Url = "/Report/ExportPdf?periodId=1", Icon = "bi-file-earmark-pdf", BadgeText = "Tải về", BadgeClass = "bg-danger" },
            new() { Category = "Hành động nhanh", Title = "Xem Phiếu lương cá nhân (Payslip)", Subtitle = "Tra cứu bảng kê chi tiết thu nhập P1, P2, P3", Url = "/Payroll/MyPayslip", Icon = "bi-wallet2", BadgeText = "Tra cứu", BadgeClass = "bg-info" }
        };

        if (string.IsNullOrWhiteSpace(q))
        {
            // Nếu chưa gõ từ khóa: gợi ý các hành động nhanh và trang chính
            result.Items.AddRange(quickActions);
            result.Items.AddRange(systemPages.Take(6));
            return result;
        }

        // Lọc Trang & Hành động nhanh theo từ khóa
        var matchedPages = systemPages
            .Where(p => p.Title.ToLower().Contains(q) || p.Subtitle.ToLower().Contains(q) || (p.BadgeText != null && p.BadgeText.ToLower().Contains(q)))
            .ToList();
        result.Items.AddRange(matchedPages);

        var matchedActions = quickActions
            .Where(a => a.Title.ToLower().Contains(q) || a.Subtitle.ToLower().Contains(q))
            .ToList();
        result.Items.AddRange(matchedActions);

        // 2. Tìm kiếm Cán bộ / Giảng viên (EmployeeProfiles)
        var matchedEmployees = await _context.EmployeeProfiles
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => !e.IsDeleted && (
                e.FullName.ToLower().Contains(q) || 
                e.StaffCode.ToLower().Contains(q) || 
                (e.Phone != null && e.Phone.Contains(q))))
            .Take(5)
            .Select(e => new SearchItemDto
            {
                Category = "Cán bộ / Giảng viên",
                Title = e.FullName,
                Subtitle = $"{e.StaffCode} — {e.Position.Name} | {e.Department.Name}",
                Url = $"/Report/Leaderboard?searchTerm={Uri.EscapeDataString(e.FullName)}",
                Icon = "bi-person-badge",
                BadgeText = e.Department.Name,
                BadgeClass = "bg-primary-subtle text-primary"
            })
            .ToListAsync();
        result.Items.AddRange(matchedEmployees);

        // 3. Tìm kiếm Chỉ tiêu KPI (KpiMasterItems)
        var matchedKpis = await _context.KpiMasterItems
            .AsNoTracking()
            .Where(k => !k.IsDeleted && (k.Name.ToLower().Contains(q) || k.Code.ToLower().Contains(q)))
            .Take(5)
            .Select(k => new SearchItemDto
            {
                Category = "Chỉ tiêu KPI",
                Title = k.Name,
                Subtitle = $"Mã chỉ tiêu: {k.Code}",
                Url = $"/Catalog/Index?activeTab=kpiMaster&search={Uri.EscapeDataString(k.Code)}",
                Icon = "bi-bullseye",
                BadgeText = k.Code,
                BadgeClass = "bg-success-subtle text-success"
            })
            .ToListAsync();
        result.Items.AddRange(matchedKpis);

        // 4. Tìm kiếm Danh mục Phòng ban & Kỳ đánh giá
        var matchedDepts = await _context.Departments
            .AsNoTracking()
            .Where(d => !d.IsDeleted && (d.Name.ToLower().Contains(q) || d.Code.ToLower().Contains(q)))
            .Take(3)
            .Select(d => new SearchItemDto
            {
                Category = "Phòng ban / Đơn vị",
                Title = d.Name,
                Subtitle = $"Mã đơn vị: {d.Code}",
                Url = $"/Report/Index?departmentIds={d.Id}",
                Icon = "bi-building",
                BadgeText = "Đơn vị",
                BadgeClass = "bg-secondary-subtle text-secondary"
            })
            .ToListAsync();
        result.Items.AddRange(matchedDepts);

        var matchedPeriods = await _context.EvaluationPeriods
            .AsNoTracking()
            .Where(p => !p.IsDeleted && (p.Name.ToLower().Contains(q) || p.Code.ToLower().Contains(q)))
            .Take(2)
            .Select(p => new SearchItemDto
            {
                Category = "Kỳ đánh giá",
                Title = p.Name,
                Subtitle = $"Mã kỳ: {p.Code} ({p.Year})",
                Url = $"/Report/Index?periodId={p.Id}",
                Icon = "bi-calendar3",
                BadgeText = "Kỳ KPI",
                BadgeClass = "bg-info-subtle text-info"
            })
            .ToListAsync();
        result.Items.AddRange(matchedPeriods);

        return result;
    }
}
