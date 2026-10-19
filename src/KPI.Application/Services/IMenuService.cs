using System.Security.Claims;
using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Dịch vụ kết xuất Menu điều hướng động từ Database theo quyền thực tế của Người dùng
/// </summary>
public interface IMenuService
{
    Task<List<MenuItemDto>> GetUserNavigationMenuAsync(ClaimsPrincipal user);
}
