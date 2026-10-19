using System.Security.Claims;
using KPI.Application.Common.Security;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Dịch vụ kết xuất Menu điều hướng động từ Database theo quyền thực tế của Người dùng
/// Nếu là ADMIN -> Thấy toàn bộ menu hệ thống và menu Quản trị
/// Nếu là User khác -> Chỉ thấy các menu mà User có Claim Permission tương ứng (hoặc menu công khai không yêu cầu quyền)
/// </summary>
public class MenuService : IMenuService
{
    private readonly KpiDbContext _context;

    public MenuService(KpiDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItemDto>> GetUserNavigationMenuAsync(ClaimsPrincipal user)
    {
        List<Domain.Entities.Identity.MenuItem> allMenus = new();
        try
        {
            allMenus = await _context.MenuItems
                .Where(m => !m.IsDeleted && m.IsActive)
                .OrderBy(m => m.SortOrder)
                .AsNoTracking()
                .ToListAsync();
        }
        catch
        {
            // Bỏ qua lỗi nếu bảng menu_items chưa sẵn sàng, dùng danh sách mặc định bên dưới
        }

        if (!allMenus.Any())
        {
            allMenus = GetDefaultStaticMenuItems();
        }

        var isAdmin = user.IsInRole(AppRoles.Admin);
        var userPermissions = user.FindAll("Permission").Select(c => c.Value).ToHashSet();

        // Lọc menu theo quyền
        var accessibleMenus = allMenus.Where(m =>
        {
            if (isAdmin) return true;
            if (string.IsNullOrEmpty(m.PermissionCode)) return true;
            return userPermissions.Contains(m.PermissionCode);
        }).ToList();

        // Xây dựng cây phân cấp (Hierarchical Tree)
        var menuDict = accessibleMenus.ToDictionary(m => m.Id, m => new MenuItemDto
        {
            Id = m.Id,
            ParentId = m.ParentId,
            Title = m.Title,
            Icon = m.Icon,
            Url = m.Url,
            PermissionCode = m.PermissionCode,
            SortOrder = m.SortOrder,
            IsActive = m.IsActive,
            SectionTitle = m.SectionTitle
        });

        var rootMenus = new List<MenuItemDto>();

        foreach (var item in menuDict.Values)
        {
            if (item.ParentId.HasValue && menuDict.TryGetValue(item.ParentId.Value, out var parentItem))
            {
                parentItem.Children.Add(item);
            }
            else
            {
                rootMenus.Add(item);
            }
        }

        return rootMenus.OrderBy(m => m.SortOrder).ToList();
    }

    private static List<Domain.Entities.Identity.MenuItem> GetDefaultStaticMenuItems()
    {
        return new List<Domain.Entities.Identity.MenuItem>
        {
            new() { Id = 1, Title = "Bàn làm việc (Dashboard)", Icon = "bi-grid-fill", Url = "/Dashboard/Index", PermissionCode = "Dashboard.View", SortOrder = 1, SectionTitle = "Tổng quan", IsActive = true },
            new() { Id = 2, Title = "Mục tiêu Ban Giám đốc", Icon = "bi-flag-fill", Url = "/KpiPlanning/DirectorTargets", PermissionCode = "DirectorTarget.View", SortOrder = 2, SectionTitle = "Kế hoạch & Giao KPI", IsActive = true },
            new() { Id = 3, Title = "KPI Của Tôi", Icon = "bi-person-workspace", Url = "/KpiPlanning/MyKpi", PermissionCode = "KpiPlanning.View", SortOrder = 3, SectionTitle = "Kế hoạch & Giao KPI", IsActive = true },
            new() { Id = 4, Title = "Giao Chỉ Tiêu (Wizard)", Icon = "bi-bullseye", Url = "/KpiPlanning/Assign", PermissionCode = "KpiPlanning.Assign", SortOrder = 4, SectionTitle = "Kế hoạch & Giao KPI", IsActive = true },
            new() { Id = 5, Title = "Phê Duyệt Kế Hoạch (BR-03)", Icon = "bi-patch-check-fill", Url = "/KpiPlanning/Approvals", PermissionCode = "KpiPlanning.Approve", SortOrder = 5, SectionTitle = "Kế hoạch & Giao KPI", IsActive = true },
            new() { Id = 6, Title = "Bảng Chấm Công (20% KPI)", Icon = "bi-calendar-check-fill", Url = "/Attendance/Index", PermissionCode = "Attendance.View", SortOrder = 6, SectionTitle = "Chấm công & Kỷ luật", IsActive = true },
            new() { Id = 7, Title = "Tự đánh giá (Self-Assess)", Icon = "bi-pencil-square", Url = "/Evaluation/SelfAssess", PermissionCode = "Evaluation.SelfAssess", SortOrder = 7, SectionTitle = "Đánh giá & Thẩm định", IsActive = true },
            new() { Id = 8, Title = "Quản lý Thẩm định điểm", Icon = "bi-clipboard-check-fill", Url = "/Evaluation/ManagerList", PermissionCode = "Evaluation.ManagerReview", SortOrder = 8, SectionTitle = "Đánh giá & Thẩm định", IsActive = true },
            new() { Id = 9, Title = "Lãnh đạo Phê duyệt", Icon = "bi-award-fill", Url = "/Evaluation/DirectorList", PermissionCode = "Evaluation.FinalApprove", SortOrder = 9, SectionTitle = "Đánh giá & Thẩm định", IsActive = true },
            new() { Id = 10, Title = "Xếp loại CBCC (NĐ 90)", Icon = "bi-patch-check-fill", Url = "/CivilServant/Index", PermissionCode = "Decree90.View", SortOrder = 10, SectionTitle = "Nghị định 90 & 48", IsActive = true },
            new() { Id = 11, Title = "Tính lương KPI (P3)", Icon = "bi-cash-stack", Url = "/Payroll/Index", PermissionCode = "Payroll.View", SortOrder = 11, SectionTitle = "Lương 3P & Chi trả", IsActive = true },
            new() { Id = 12, Title = "Ký nhận Tiền mặt (05a)", Icon = "bi-pen-fill", Url = "/Payroll/CashSheet", PermissionCode = "Payroll.PayoutCash", SortOrder = 12, SectionTitle = "Lương 3P & Chi trả", IsActive = true },
            new() { Id = 13, Title = "Phiếu lương Cá nhân", Icon = "bi-file-earmark-person-fill", Url = "/Payroll/MyPayslip", PermissionCode = "Payroll.ViewOwnPayslip", SortOrder = 13, SectionTitle = "Lương 3P & Chi trả", IsActive = true },
            new() { Id = 14, Title = "Báo cáo & Phân tích", Icon = "bi-graph-up-arrow", Url = "/Report/Index", PermissionCode = "Report.View", SortOrder = 14, SectionTitle = "Báo cáo & Phân tích", IsActive = true },
            new() { Id = 15, Title = "Quản lý Người dùng", Icon = "bi-people-fill", Url = "/Admin/Users", PermissionCode = "Admin.ManageUsers", SortOrder = 15, SectionTitle = "Quản trị Hệ thống (ADMIN)", IsActive = true },
            new() { Id = 16, Title = "Quản lý Menu Đầu mục", Icon = "bi-menu-button-wide-fill", Url = "/Admin/Menus", PermissionCode = "Admin.ManageMenus", SortOrder = 16, SectionTitle = "Quản trị Hệ thống (ADMIN)", IsActive = true },
            new() { Id = 17, Title = "Phân quyền Hệ thống", Icon = "bi-shield-lock-fill", Url = "/Admin/Permissions", PermissionCode = "Admin.ManagePermissions", SortOrder = 17, SectionTitle = "Quản trị Hệ thống (ADMIN)", IsActive = true },
            new() { Id = 18, Title = "Nhật ký Kiểm toán", Icon = "bi-clock-history", Url = "/Admin/AuditLog", PermissionCode = "Admin.ViewAuditLogs", SortOrder = 18, SectionTitle = "Quản trị Hệ thống (ADMIN)", IsActive = true }
        };
    }
}
