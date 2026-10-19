using FluentAssertions;
using KPI.Application.DTOs;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using Xunit;

namespace KPI.Tests.Services;

public class CatalogServiceTests
{
    private KpiDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new KpiDbContext(options);
        SeedSampleData(context);
        return context;
    }

    private void SeedSampleData(KpiDbContext context)
    {
        var catBsc = new KpiCategory
        {
            Id = 1,
            Code = "BSC_CUS",
            Name = "Sự hài lòng tổ chức & người dân",
            Perspective = KpiPerspective.Customer,
            TargetSector = TargetSector.PublicSector,
            DisplayOrder = 1
        };

        var unit = new MeasurementUnit
        {
            Id = 1,
            Code = "PERCENT",
            Name = "Phần trăm (%)",
            Symbol = "%"
        };

        var method = new MeasurementMethod
        {
            Id = 1,
            Code = "SURVEY",
            Name = "Khảo sát trực tuyến & phiếu đánh giá"
        };

        var org = new Organization
        {
            Id = 1,
            Code = "HVHC",
            Name = "Học viện Hành chính và Quản trị công"
        };

        var deptRoot = new Department
        {
            Id = 1,
            OrgId = 1,
            Code = "BGD",
            Name = "Ban Giám Đốc",
            Level = 1,
            TreePath = "/1/"
        };

        var deptChild = new Department
        {
            Id = 2,
            OrgId = 1,
            ParentId = 1,
            Code = "PKHTC",
            Name = "Phòng Kế hoạch Tài chính",
            Level = 2,
            TreePath = "/1/2/"
        };

        var kpi1 = new KpiMasterItem
        {
            Id = 1,
            CategoryId = 1,
            UnitId = 1,
            MethodId = 1,
            Code = "KPI_CS_01",
            Name = "Tỷ lệ hồ sơ TTHC giải quyết đúng và trước hạn",
            Description = "Đo lường tiến độ giải quyết dịch vụ hành chính công",
            ScoringFormula = ScoringFormula.HigherIsBetter,
            IsCivilServantOnly = true,
            IsActive = true,
            IsDeleted = false
        };

        var period = new EvaluationPeriod
        {
            Id = 1,
            ScaleId = 1,
            Code = "2026_Q1",
            Name = "Đánh giá KPI Quý 1/2026",
            Year = 2026,
            Status = PeriodStatus.Planning,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31),
            SelfDeadline = new DateTime(2026, 4, 5),
            ManagerDeadline = new DateTime(2026, 4, 15),
            ClosingDate = new DateTime(2026, 4, 25),
            IsDeleted = false
        };

        context.KpiCategories.Add(catBsc);
        context.MeasurementUnits.Add(unit);
        context.MeasurementMethods.Add(method);
        context.Organizations.Add(org);
        context.Departments.AddRange(deptRoot, deptChild);
        context.KpiMasterItems.Add(kpi1);
        context.EvaluationPeriods.Add(period);
        context.SaveChanges();
    }

    [Fact]
    public async Task GetKpiMasterItemsAsync_ShouldReturnActiveItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogService(context, NullLogger<CatalogService>.Instance);

        // Act
        var result = await service.GetKpiMasterItemsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Code.Should().Be("KPI_CS_01");
        result[0].CategoryName.Should().Be("Sự hài lòng tổ chức & người dân");
    }

    [Fact]
    public async Task CloneKpiMasterItemAsync_ShouldCreateNewKpiWithCopiedProperties()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogService(context, NullLogger<CatalogService>.Instance);
        var request = new CloneKpiMasterItemRequest
        {
            SourceId = 1,
            NewCode = "KPI_CS_01_CLONE",
            NewName = "Tỷ lệ hồ sơ TTHC giải quyết trước hạn (Bản sao)"
        };

        // Act
        var clonedId = await service.CloneKpiMasterItemAsync(request, userId: 100);

        // Assert
        clonedId.Should().BeGreaterThan(0);
        var clonedItem = await context.KpiMasterItems.FindAsync(clonedId);
        clonedItem.Should().NotBeNull();
        clonedItem!.Code.Should().Be("KPI_CS_01_CLONE");
        clonedItem.Name.Should().Be("Tỷ lệ hồ sơ TTHC giải quyết trước hạn (Bản sao)");
        clonedItem.CategoryId.Should().Be(1);
        clonedItem.UnitId.Should().Be(1);
        clonedItem.MethodId.Should().Be(1);
    }

    [Fact]
    public async Task DeleteKpiMasterItemAsync_ShouldSoftDelete_NotRemoveRecord()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogService(context, NullLogger<CatalogService>.Instance);

        // Act
        var success = await service.DeleteKpiMasterItemAsync(1, userId: 100);

        // Assert
        success.Should().BeTrue();
        var item = await context.KpiMasterItems.FindAsync(1L);
        item.Should().NotBeNull();
        item!.IsDeleted.Should().BeTrue();

        // Check it is excluded from master items view
        var activeItems = await service.GetKpiMasterItemsAsync();
        activeItems.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDepartmentTreeAsync_ShouldBuildHierarchyTree()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogService(context, NullLogger<CatalogService>.Instance);

        // Act
        var tree = await service.GetDepartmentTreeAsync();

        // Assert
        tree.Should().NotBeNull();
        tree.Should().HaveCount(1); // 1 root node
        tree[0].Code.Should().Be("BGD");
        tree[0].Children.Should().HaveCount(1);
        tree[0].Children[0].Code.Should().Be("PKHTC");
    }

    [Fact]
    public async Task ChangePeriodStatusAsync_ShouldUpdateEvaluationPeriodStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogService(context, NullLogger<CatalogService>.Instance);
        var request = new ChangePeriodStatusRequest
        {
            PeriodId = 1,
            NewStatus = "InEvaluation"
        };

        // Act
        var success = await service.ChangePeriodStatusAsync(request, userId: 100);

        // Assert
        success.Should().BeTrue();
        var period = await context.EvaluationPeriods.FindAsync(1L);
        period.Should().NotBeNull();
        period!.Status.Should().Be(PeriodStatus.InEvaluation);
    }

    [Fact]
    public async Task ExportCatalogCsvAsync_ShouldReturnUtf8BomEncodedBytes()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogService(context, NullLogger<CatalogService>.Instance);

        // Act
        var csvBytes = await service.ExportCatalogCsvAsync("kpi_items");

        // Assert
        csvBytes.Should().NotBeNull();
        csvBytes.Length.Should().BeGreaterThan(3);

        // Check UTF-8 BOM
        csvBytes[0].Should().Be(0xEF);
        csvBytes[1].Should().Be(0xBB);
        csvBytes[2].Should().Be(0xBF);

        var content = Encoding.UTF8.GetString(csvBytes);
        content.Should().Contain("KPI_CS_01");
        content.Should().Contain("Tỷ lệ hồ sơ TTHC");
    }

    [Fact]
    public async Task ExecuteBulkActionAsync_ShouldDeactivateSelectedItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new CatalogService(context, NullLogger<CatalogService>.Instance);
        var request = new BulkActionRequest
        {
            CatalogType = "kpi_items",
            ActionType = "deactivate",
            Ids = new List<long> { 1 }
        };

        // Act
        var success = await service.ExecuteBulkActionAsync(request, userId: 100);

        // Assert
        success.Should().BeTrue();
        var item = await context.KpiMasterItems.FindAsync(1L);
        item.Should().NotBeNull();
        item!.IsActive.Should().BeFalse();
    }
}
