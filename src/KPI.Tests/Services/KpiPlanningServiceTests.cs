using FluentAssertions;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KPI.Tests.Services;

public class KpiPlanningServiceTests
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
        var period = new EvaluationPeriod
        {
            Id = 1,
            ScaleId = 1,
            Code = "2026_Q1",
            Name = "Kỳ đánh giá Quý 1/2026",
            Year = 2026,
            Status = PeriodStatus.Planning,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31)
        };

        var dept = new Department
        {
            Id = 1,
            OrgId = 1,
            Code = "PKHTC",
            Name = "Phòng Kế hoạch Tài chính"
        };

        var pos = new Position
        {
            Id = 1,
            Code = "CVC_TC",
            Name = "Chuyên viên chính Quản lý Ngân sách"
        };

        var employee = new EmployeeProfile
        {
            Id = 1,
            UserId = 10,
            DepartmentId = 1,
            PositionId = 1,
            StaffCode = "CB-001",
            FullName = "Nguyễn Văn A"
        };

        var category = new KpiCategory
        {
            Id = 1,
            Code = "BSC_FIN",
            Name = "Hiệu quả Ngân sách",
            Perspective = KpiPerspective.Financial
        };

        var unit = new MeasurementUnit
        {
            Id = 1,
            Code = "PCT",
            Name = "Phần trăm",
            Symbol = "%"
        };

        var method = new MeasurementMethod
        {
            Id = 1,
            Code = "AUTO",
            Name = "Hệ thống Tự động"
        };

        var kpi1 = new KpiMasterItem
        {
            Id = 1,
            CategoryId = 1,
            UnitId = 1,
            MethodId = 1,
            Code = "KPI_NS_01",
            Name = "Tỷ lệ giải ngân vốn ngân sách đúng tiến độ",
            IsActive = true
        };

        var kpi2 = new KpiMasterItem
        {
            Id = 2,
            CategoryId = 1,
            UnitId = 1,
            MethodId = 1,
            Code = "KPI_NS_02",
            Name = "Tỷ lệ quyết toán chi thường xuyên đúng quy định",
            IsActive = true
        };

        var template = new KpiTemplate
        {
            Id = 1,
            PositionId = 1,
            DepartmentId = 1,
            Code = "TPL_CVC_TC",
            Name = "Khung KPI chuẩn Chuyên viên chính Tài chính"
        };

        template.TemplateDetails.Add(new KpiTemplateDetail
        {
            Id = 1,
            TemplateId = 1,
            KpiItemId = 1,
            TargetValue = 95m,
            Weight = 40m
        });

        template.TemplateDetails.Add(new KpiTemplateDetail
        {
            Id = 2,
            TemplateId = 1,
            KpiItemId = 2,
            TargetValue = 100m,
            Weight = 60m
        });

        context.EvaluationPeriods.Add(period);
        context.Departments.Add(dept);
        context.Positions.Add(pos);
        context.EmployeeProfiles.Add(employee);
        context.KpiCategories.Add(category);
        context.MeasurementUnits.Add(unit);
        context.MeasurementMethods.Add(method);
        context.KpiMasterItems.AddRange(kpi1, kpi2);
        context.KpiTemplates.Add(template);
        context.SaveChanges();
    }

    [Fact]
    public async Task SaveOrSubmitAssignmentAsync_WhenTotalWeightNot100_ThrowsBR01Exception()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new KpiPlanningService(context, NullLogger<KpiPlanningService>.Instance);
        var request = new CreateAssignmentRequest
        {
            PeriodId = 1,
            EmployeeId = 1,
            Title = "Kế hoạch KPI 2026",
            IsDraft = false,
            Items = new List<AssignmentItemRequest>
            {
                new() { KpiItemId = 1, KpiName = "KPI 1", Weight = 40m, TargetValue = 95m, Unit = "%" },
                new() { KpiItemId = 2, KpiName = "KPI 2", Weight = 50m, TargetValue = 100m, Unit = "%" } // Tổng = 90% != 100%
            }
        };

        // Act
        Func<Task> act = async () => await service.SaveOrSubmitAssignmentAsync(request, currentUserId: 99);

        // Assert: Bắt buộc ném ngoại lệ BR-01
        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-01");
    }

    [Fact]
    public async Task SaveOrSubmitAssignmentAsync_WhenWeightBelow5Percent_ThrowsBR02Exception()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new KpiPlanningService(context, NullLogger<KpiPlanningService>.Instance);
        var request = new CreateAssignmentRequest
        {
            PeriodId = 1,
            EmployeeId = 1,
            Title = "Kế hoạch KPI 2026",
            IsDraft = false,
            Items = new List<AssignmentItemRequest>
            {
                new() { KpiItemId = 1, KpiName = "KPI 1", Weight = 96m, TargetValue = 95m, Unit = "%" },
                new() { KpiItemId = 2, KpiName = "KPI 2", Weight = 4m, TargetValue = 100m, Unit = "%" } // 4% < 5% (Vi phạm BR-02)
            }
        };

        // Act
        Func<Task> act = async () => await service.SaveOrSubmitAssignmentAsync(request, currentUserId: 99);

        // Assert: Bắt buộc ném ngoại lệ BR-02
        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-02");
    }

    [Fact]
    public async Task SaveOrSubmitAssignmentAsync_WhenWeightAbove50Percent_ThrowsBR02Exception()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new KpiPlanningService(context, NullLogger<KpiPlanningService>.Instance);
        var request = new CreateAssignmentRequest
        {
            PeriodId = 1,
            EmployeeId = 1,
            Title = "Kế hoạch KPI 2026",
            IsDraft = false,
            Items = new List<AssignmentItemRequest>
            {
                new() { KpiItemId = 1, KpiName = "KPI 1", Weight = 60m, TargetValue = 95m, Unit = "%" }, // 60% > 50% (Vi phạm BR-02)
                new() { KpiItemId = 2, KpiName = "KPI 2", Weight = 40m, TargetValue = 100m, Unit = "%" }
            }
        };

        // Act
        Func<Task> act = async () => await service.SaveOrSubmitAssignmentAsync(request, currentUserId: 99);

        // Assert: Bắt buộc ném ngoại lệ BR-02
        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-02");
    }

    [Fact]
    public async Task SaveOrSubmitAssignmentAsync_WhenValidWeights_CreatesAssignmentWithPendingStaffConfirm()
    {
        // Arrange: Hợp lệ BR-01 (50% + 50% = 100%) và BR-02 (5% <= weight <= 50%)
        using var context = CreateInMemoryDbContext();
        var service = new KpiPlanningService(context, NullLogger<KpiPlanningService>.Instance);
        var request = new CreateAssignmentRequest
        {
            PeriodId = 1,
            EmployeeId = 1,
            Title = "Kế hoạch KPI chuẩn",
            IsDraft = false,
            Items = new List<AssignmentItemRequest>
            {
                new() { KpiItemId = 1, KpiName = "KPI 1", Weight = 50m, TargetValue = 95m, Unit = "%" },
                new() { KpiItemId = 2, KpiName = "KPI 2", Weight = 50m, TargetValue = 100m, Unit = "%" }
            }
        };

        // Act
        var assignmentId = await service.SaveOrSubmitAssignmentAsync(request, currentUserId: 99);

        // Assert
        assignmentId.Should().BeGreaterThan(0);
        var assignment = await context.KpiAssignments.Include(a => a.AssignmentDetails).FirstOrDefaultAsync(a => a.Id == assignmentId);
        assignment.Should().NotBeNull();
        assignment!.TotalWeight.Should().Be(100.00m);
        assignment.Status.Should().Be(AssignmentStatus.Pending_Staff_Confirm);
        assignment.AssignmentDetails.Should().HaveCount(2);
    }

    [Fact]
    public async Task StaffConfirmAssignmentAsync_ShouldRecordCommittedTimestampAndTransitionToPendingApproval()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new KpiPlanningService(context, NullLogger<KpiPlanningService>.Instance);

        var assignment = new KpiAssignment
        {
            Id = 1,
            PeriodId = 1,
            EmployeeId = 1,
            ApproverId = 99,
            Title = "Bản giao mẫu",
            TotalWeight = 100m,
            Status = AssignmentStatus.Pending_Staff_Confirm
        };
        context.KpiAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var confirmRequest = new StaffConfirmRequest
        {
            AssignmentId = 1,
            Agree = true,
            ElectronicSignatureNote = "Cán bộ Nguyễn Văn A đã ký cam kết điện tử"
        };

        // Act: employeeUserId = 10 (UserId của EmployeeId 1)
        var result = await service.StaffConfirmAssignmentAsync(confirmRequest, employeeUserId: 10);

        // Assert
        result.Should().BeTrue();
        var updated = await context.KpiAssignments.FindAsync(1L);
        updated!.Status.Should().Be(AssignmentStatus.Pending_Approval);
        updated.CommittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DirectorApproveAsync_ShouldLockAssignment_BR03()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new KpiPlanningService(context, NullLogger<KpiPlanningService>.Instance);

        var assignment = new KpiAssignment
        {
            Id = 1,
            PeriodId = 1,
            EmployeeId = 1,
            ApproverId = 99,
            Title = "Bản giao chờ duyệt",
            TotalWeight = 100m,
            Status = AssignmentStatus.Pending_Approval
        };
        assignment.AssignmentDetails.Add(new KpiAssignmentDetail { KpiItemId = 1, KpiName = "K1", Weight = 50m, Unit = "%" });
        assignment.AssignmentDetails.Add(new KpiAssignmentDetail { KpiItemId = 2, KpiName = "K2", Weight = 50m, Unit = "%" });
        context.KpiAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var approveRequest = new ApproveAssignmentRequest
        {
            AssignmentId = 1,
            Comments = "Đồng ý phê duyệt và kích hoạt kế hoạch"
        };

        // Act
        var result = await service.DirectorApproveAsync(approveRequest, directorUserId: 88);

        // Assert: BR-03 Đã khóa bản giao
        result.Should().BeTrue();
        var updated = await context.KpiAssignments.FindAsync(1L);
        updated!.Status.Should().Be(AssignmentStatus.Approved_Locked);
        updated.ApprovedAt.Should().NotBeNull();

        // Thử sửa bản giao sau khi khóa -> Phải bị chặn theo quy tắc BR-03
        var editRequest = new CreateAssignmentRequest
        {
            Id = 1,
            PeriodId = 1,
            EmployeeId = 1,
            IsDraft = false,
            Items = new List<AssignmentItemRequest>
            {
                new() { KpiItemId = 1, KpiName = "K1", Weight = 50m, TargetValue = 90m, Unit = "%" },
                new() { KpiItemId = 2, KpiName = "K2", Weight = 50m, TargetValue = 100m, Unit = "%" }
            }
        };

        Func<Task> actEdit = async () => await service.SaveOrSubmitAssignmentAsync(editRequest, currentUserId: 99);
        await actEdit.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-03");
    }

    [Fact]
    public async Task CreateRevisionAndGetDiffAsync_ShouldTrackDifferencesAccurately()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new KpiPlanningService(context, NullLogger<KpiPlanningService>.Instance);

        var assignment = new KpiAssignment
        {
            Id = 1,
            PeriodId = 1,
            EmployeeId = 1,
            ApproverId = 99,
            Title = "Bản giao v1",
            TotalWeight = 100m,
            Status = AssignmentStatus.Approved_Locked
        };
        assignment.AssignmentDetails.Add(new KpiAssignmentDetail { KpiItemId = 1, KpiName = "KPI 1", Weight = 50m, TargetValue = 90m, Unit = "%" });
        assignment.AssignmentDetails.Add(new KpiAssignmentDetail { KpiItemId = 2, KpiName = "KPI 2", Weight = 50m, TargetValue = 100m, Unit = "%" });
        context.KpiAssignments.Add(assignment);
        await context.SaveChangesAsync();

        // Điều chỉnh giữa kỳ: KPI 1 tăng lên 95m mục tiêu, weight giảm về 40%. KPI 2 tăng weight lên 60%
        var revision = new RevisionRequest
        {
            AssignmentId = 1,
            Reason = "Điều chỉnh tỷ lệ vốn ngân sách tháng 2/2026",
            RevisedItems = new List<AssignmentItemRequest>
            {
                new() { KpiItemId = 1, KpiName = "KPI 1", Weight = 40m, TargetValue = 95m, Unit = "%" },
                new() { KpiItemId = 2, KpiName = "KPI 2", Weight = 60m, TargetValue = 100m, Unit = "%" } // In revision test: 40% + 60% = 100%
            }
        };

        // Note: For BR-02 test in revision, let's use 50% / 50% with changed target to keep within [5, 50]
        revision.RevisedItems[0].Weight = 50m;
        revision.RevisedItems[1].Weight = 50m;

        // Act
        var revResult = await service.CreateRevisionAsync(revision, currentUserId: 99);
        revResult.Should().BeTrue();

        var diff = await service.GetRevisionDiffAsync(1);

        // Assert
        diff.Should().NotBeNull();
        diff.Differences.Should().HaveCount(2);
        var diffKpi1 = diff.Differences.FirstOrDefault(d => d.KpiItemId == 1);
        diffKpi1.Should().NotBeNull();
        diffKpi1!.ChangeType.Should().Be("MODIFIED");
        diffKpi1.OldTarget.Should().Be(90m);
        diffKpi1.NewTarget.Should().Be(95m);
    }
}
