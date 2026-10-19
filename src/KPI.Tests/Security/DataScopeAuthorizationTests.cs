using System.Security.Claims;
using FluentAssertions;
using KPI.Application.Common.Security;
using KPI.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace KPI.Tests.Security;

public class DataScopeAuthorizationTests
{
    private readonly DataScopeHandler _handler = new();

    [Theory]
    [InlineData("Organization", DataScope.Organization, true)]
    [InlineData("Organization", DataScope.Department, true)]
    [InlineData("Organization", DataScope.Self, true)]
    [InlineData("Department", DataScope.Department, true)]
    [InlineData("Department", DataScope.Self, true)]
    [InlineData("Department", DataScope.Organization, false)]
    [InlineData("Self", DataScope.Self, true)]
    [InlineData("Self", DataScope.Department, false)]
    [InlineData("Self", DataScope.Organization, false)]
    public async Task HandleRequirementAsync_EvaluatesScopeHierarchyCorrectly(
        string userScopeClaim, DataScope requiredScope, bool shouldSucceed)
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("DataScope", userScopeClaim)
        }));

        var requirement = new DataScopeRequirement(requiredScope);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().Be(shouldSucceed);
    }

    [Fact]
    public void GetDefaultDataScope_MapsRolesToAccurateScopes()
    {
        AppRoles.GetDefaultDataScope(AppRoles.Director).Should().Be(DataScope.Organization);
        AppRoles.GetDefaultDataScope(AppRoles.Admin).Should().Be(DataScope.Organization);
        AppRoles.GetDefaultDataScope(AppRoles.HrAdmin).Should().Be(DataScope.Organization);
        AppRoles.GetDefaultDataScope(AppRoles.Accountant).Should().Be(DataScope.Organization);
        AppRoles.GetDefaultDataScope(AppRoles.Manager).Should().Be(DataScope.Department);
        AppRoles.GetDefaultDataScope(AppRoles.Officer).Should().Be(DataScope.Self);
    }
}
