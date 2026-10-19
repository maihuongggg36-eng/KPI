using FluentAssertions;
using KPI.Application.Common.Exceptions;
using KPI.Application.Services;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Services;
using KPI.Web.Controllers;
using KPI.Web.Health;
using KPI.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KPI.Tests.Integration;

public class HealthAndSeoEndpointsTests
{
    private KpiDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new KpiDbContext(options);
    }

    #region 1. SEO Endpoints Tests (sitemap.xml & robots.txt)

    [Fact]
    public void SeoController_Sitemap_ShouldReturnValidXmlContent()
    {
        // Arrange
        var controller = new SeoController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Request.Scheme = "https";
        controller.HttpContext.Request.Host = new HostString("kpi.napa.vn");

        // Act
        var result = controller.Sitemap() as ContentResult;

        // Assert
        result.Should().NotBeNull();
        result!.ContentType.Should().Contain("application/xml");
        result.Content.Should().Contain("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        result.Content.Should().Contain("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        result.Content.Should().Contain("https://kpi.napa.vn/");
        result.Content.Should().Contain("https://kpi.napa.vn/Auth/Login");
        result.Content.Should().Contain("https://kpi.napa.vn/Health");
    }

    [Fact]
    public void SeoController_Robots_ShouldReturnCorrectDirectives()
    {
        // Arrange
        var controller = new SeoController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Request.Scheme = "https";
        controller.HttpContext.Request.Host = new HostString("kpi.napa.vn");

        // Act
        var result = controller.Robots() as ContentResult;

        // Assert
        result.Should().NotBeNull();
        result!.ContentType.Should().Contain("text/plain");
        result.Content.Should().Contain("User-agent: *");
        result.Content.Should().Contain("Allow: /");
        result.Content.Should().Contain("Disallow: /api/");
        result.Content.Should().Contain("Disallow: /hubs/");
        result.Content.Should().Contain("Sitemap: https://kpi.napa.vn/sitemap.xml");
    }

    #endregion

    #region 2. Health Check Dashboard & JSON Endpoint Tests

    [Fact]
    public async Task HealthController_GetStatus_ShouldReturnDetailedJson()
    {
        // Arrange: Setup Service Provider with Logging and HealthChecks
        var services = new ServiceCollection();
        using var dbContext = CreateInMemoryDbContext();

        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("Database")
            .AddCheck<MemoryHealthCheck>("Memory")
            .AddCheck<DiskSpaceHealthCheck>("DiskSpace");

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        var controller = new HealthController(healthCheckService, NullLogger<HealthController>.Instance);

        // Act
        var result = await controller.GetStatus() as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        // Verify JSON properties via reflection
        var val = result.Value;
        val.Should().NotBeNull();
        var statusProp = val!.GetType().GetProperty("status")?.GetValue(val);
        statusProp.Should().NotBeNull();
        statusProp!.ToString().Should().BeOneOf("Healthy", "Degraded");
    }

    [Fact]
    public async Task HealthController_Index_ShouldReturnViewResultWithHealthReport()
    {
        // Arrange
        var services = new ServiceCollection();
        using var dbContext = CreateInMemoryDbContext();

        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddHealthChecks()
            .AddCheck<MemoryHealthCheck>("Memory");

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        var controller = new HealthController(healthCheckService, NullLogger<HealthController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.Index() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().BeOfType<HealthReport>();
        var report = (HealthReport)result.Model!;
        report.Entries.Should().ContainKey("Memory");
    }

    #endregion

    #region 3. Custom Error Pages & Public Views (HomeController)

    [Theory]
    [InlineData(404, "404 - Không Tìm Thấy Trang")]
    [InlineData(403, "403 - Quyền Truy Cập Bị Từ Chối")]
    [InlineData(500, "500 - Lỗi Máy Chủ Nội Bộ")]
    public void HomeController_Error_ShouldReturnExpectedErrorViewModel(int statusCode, string expectedTitle)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var controller = new HomeController(NullLogger<HomeController>.Instance, dbContext);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = controller.Error(statusCode) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result!.Model.Should().BeOfType<ErrorViewModel>();
        var model = (ErrorViewModel)result.Model!;
        model.StatusCode.Should().Be(statusCode);
        model.ErrorTitle.Should().Be(expectedTitle);
    }

    [Fact]
    public void HomeController_About_ShouldReturnViewWithTitle()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var controller = new HomeController(NullLogger<HomeController>.Instance, dbContext);

        // Act
        var result = controller.About() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        controller.ViewData["Title"].Should().NotBeNull();
        controller.ViewData["Title"]!.ToString().Should().Contain("Giới Thiệu");
    }

    [Fact]
    public void HomeController_UserGuide_ShouldReturnViewWithTitle()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var controller = new HomeController(NullLogger<HomeController>.Instance, dbContext);

        // Act
        var result = controller.UserGuide() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        controller.ViewData["Title"].Should().NotBeNull();
        controller.ViewData["Title"]!.ToString().Should().Contain("Hướng Dẫn");
    }

    #endregion

    #region 4. Bổ Sung Unit Tests KpiCalculationService & PayrollService

    [Fact]
    public void KpiCalculationService_CalculateCompletionRate_ZeroTarget_ReturnsZero()
    {
        var service = new KpiCalculationService();
        var rate = service.CalculateCompletionRate(50m, 0m, 150m);
        rate.Should().Be(0m);
    }

    [Fact]
    public void KpiCalculationService_ValidateWeights_EmptyList_ThrowsBR01()
    {
        var service = new KpiCalculationService();
        Action act = () => service.ValidateWeights(new List<decimal>());
        act.Should().Throw<BusinessRuleException>().Where(e => e.RuleCode == "BR-01");
    }

    [Theory]
    [InlineData(1000000000, "Một tỷ đồng chẵn.")]
    [InlineData(1250000000, "Một tỷ hai trăm năm mươi triệu đồng chẵn.")]
    [InlineData(500000, "Năm trăm nghìn đồng chẵn.")]
    public void PayrollService_ConvertNumberToVietnameseWords_LargeValues_ReturnsCorrect(decimal amount, string expected)
    {
        using var context = CreateInMemoryDbContext();
        var service = new PayrollService(context);

        var words = service.ConvertNumberToVietnameseWords(amount);
        words.Should().Be(expected);
    }

    #endregion
}
