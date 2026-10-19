using System.Security.Claims;
using KPI.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace KPI.Infrastructure.Security;

/// <summary>
/// Yêu cầu kiểm soát phạm vi dữ liệu (Data Scope Requirement)
/// </summary>
public class DataScopeRequirement : IAuthorizationRequirement
{
    public DataScope RequiredScope { get; }

    public DataScopeRequirement(DataScope requiredScope)
    {
        RequiredScope = requiredScope;
    }
}

/// <summary>
/// Trình thẩm định quyền truy cập theo phạm vi dữ liệu
/// </summary>
public class DataScopeHandler : AuthorizationHandler<DataScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DataScopeRequirement requirement)
    {
        var userScopeClaim = context.User.FindFirst("DataScope")?.Value;
        if (string.IsNullOrEmpty(userScopeClaim))
        {
            return Task.CompletedTask;
        }

        if (Enum.TryParse<DataScope>(userScopeClaim, out var userScope))
        {
            // Nếu phạm vi của user bao trùm phạm vi yêu cầu (Organization >= Department >= Self)
            if (userScope >= requirement.RequiredScope)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
