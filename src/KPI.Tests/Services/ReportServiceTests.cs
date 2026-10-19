using FluentAssertions;
using KPI.Application.DTOs;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using EvaluationEntity = KPI.Domain.Entities.Evaluation.Evaluation;

namespace KPI.Tests.Services;

public class ReportServiceTests
{
    private KpiDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new KpiDbContext(options);
    }

    private async Task SeedStandardTestData(KpiDbContext context)
    {
        var period = new EvaluationPeriod
        {
            Id = 1,
            Code = "Q1_2026",
            Name = "Quý 1/2026",
            Year = 2026,
            PeriodType = PeriodType.Quarterly,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31),
            Status = PeriodStatus.Open
        };
        context.EvaluationPeriods.Add(period);

        var dept1 = new Department { Id = 1, Code = "CNTT", Name = "Trung tâm CNTT", Level = 1 };
        var dept2 = new Department { Id = 2, Code = "KHTC", Name = "Phòng Kế hoạch - Tài chính", Level = 1 };
        context.Departments.AddRange(dept1, dept2);

        var pos1 = new Position { Id = 1, Code = "CHUYEN_VIEN", Name = "Chuyên viên chính" };
        var pos2 = new Position { Id = 2, Code = "TRUONG_PHONG", Name = "Trưởng phòng" };
        context.Positions.AddRange(pos1, pos2);

        // Seed 4 nhân viên với 4 mức điểm khác nhau: 95 (Xuất sắc), 85 (Tốt), 65 (Đạt), 45 (Không đạt)
        var users = new[]
        {
            new User { Id = 1, Username = "user1", Email = "u1@napa.vn" },
            new User { Id = 2, Username = "user2", Email = "u2@napa.vn" },
            new User { Id = 3, Username = "user3", Email = "u3@napa.vn" },
            new User { Id = 4, Username = "user4", Email = "u4@napa.vn" }
        };
        context.Users.AddRange(users);

        var employees = new[]
        {
            new EmployeeProfile { Id = 1, UserId = 1, StaffCode = "CB-0001", FullName = "Nguyễn Văn Nhất", DepartmentId = 1, PositionId = 2 },
            new EmployeeProfile { Id = 2, UserId = 2, StaffCode = "CB-0002", FullName = "Trần Thị Nhì", DepartmentId = 1, PositionId = 1 },
            new EmployeeProfile { Id = 3, UserId = 3, StaffCode = "CB-0003", FullName = "Lê Văn Ba", DepartmentId = 2, PositionId = 1 },
            new EmployeeProfile { Id = 4, UserId = 4, StaffCode = "CB-0004", FullName = "Phạm Văn Bốn", DepartmentId = 2, PositionId = 1 }
        };
        context.EmployeeProfiles.AddRange(employees);

        var evals = new[]
        {
            new EvaluationEntity { Id = 1, PeriodId = 1, EmployeeId = 1, SelfTotalScore = 96, ManagerTotalScore = 94, FinalScore = 95, Status = EvaluationStatus.Final_Approved, FinalClassification = "Hoàn thành xuất sắc nhiệm vụ" },
            new EvaluationEntity { Id = 2, PeriodId = 1, EmployeeId = 2, SelfTotalScore = 90, ManagerTotalScore = 80, FinalScore = 85, Status = EvaluationStatus.Final_Approved, FinalClassification = "Hoàn thành tốt nhiệm vụ" },
            new EvaluationEntity { Id = 3, PeriodId = 1, EmployeeId = 3, SelfTotalScore = 60, ManagerTotalScore = 70, FinalScore = 65, Status = EvaluationStatus.Final_Approved, FinalClassification = "Hoàn thành nhiệm vụ" },
            new EvaluationEntity { Id = 4, PeriodId = 1, EmployeeId = 4, SelfTotalScore = 65, ManagerTotalScore = 45, FinalScore = 45, Status = EvaluationStatus.Final_Approved, FinalClassification = "Không hoàn thành nhiệm vụ" } // Outlier: Self 65 vs Manager 45 (gap = 20)
        };
        context.Evaluations.AddRange(evals);

        await context.SaveChangesAsync();
    }

    #region 1. Executive Summary Tests

    [Fact]
    public async Task GetExecutiveSummaryAsync_ShouldCalculateMetricsCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        await SeedStandardTestData(context);
        var service = new ReportService(context);

        var req = new ReportFilterRequest { PeriodId = 1 };

        // ACT
        var summary = await service.GetExecutiveSummaryAsync(req);

        // ASSERT
        summary.Should().NotBeNull();
        summary.TotalEmployees.Should().Be(4);
        summary.EvaluatedEmployees.Should().Be(4);
        summary.CompletionRate.Should().Be(100.0);

        // Avg = (95 + 85 + 65 + 45) / 4 = 72.5
        summary.AverageScore.Should().Be(72.5m);
        summary.HighestScore.Should().Be(95.0m);
        summary.HighestEmployeeName.Should().Be("Nguyễn Văn Nhất");
        summary.LowestScore.Should().Be(45.0m);
        summary.LowestEmployeeName.Should().Be("Phạm Văn Bốn");
        summary.StandardDeviation.Should().BeGreaterThan(0);
    }

    #endregion

    #region 2. Leaderboard Tests

    [Fact]
    public async Task GetLeaderboardAsync_ShouldRankAndAssignTop3Badges()
    {
        using var context = CreateInMemoryDbContext();
        await SeedStandardTestData(context);
        var service = new ReportService(context);

        var req = new ReportFilterRequest { PeriodId = 1, SortBy = "ScoreDesc" };

        // ACT
        var leaderboard = await service.GetLeaderboardAsync(req);

        // ASSERT
        leaderboard.Should().HaveCount(4);

        leaderboard[0].Rank.Should().Be(1);
        leaderboard[0].FullName.Should().Be("Nguyễn Văn Nhất");
        leaderboard[0].FinalScore.Should().Be(95.0m);
        leaderboard[0].BadgeType.Should().Be("Gold");
        leaderboard[0].IsTop3.Should().BeTrue();

        leaderboard[1].Rank.Should().Be(2);
        leaderboard[1].FullName.Should().Be("Trần Thị Nhì");
        leaderboard[1].BadgeType.Should().Be("Silver");
        leaderboard[1].IsTop3.Should().BeTrue();

        leaderboard[2].Rank.Should().Be(3);
        leaderboard[2].FullName.Should().Be("Lê Văn Ba");
        leaderboard[2].BadgeType.Should().Be("Bronze");
        leaderboard[2].IsTop3.Should().BeTrue();

        leaderboard[3].Rank.Should().Be(4);
        leaderboard[3].BadgeType.Should().BeEmpty();
        leaderboard[3].IsTop3.Should().BeFalse();
    }

    #endregion

    #region 3. Gauss Distribution Tests

    [Fact]
    public async Task GetGaussDistributionAsync_ShouldDistributeIntoFourBands()
    {
        using var context = CreateInMemoryDbContext();
        await SeedStandardTestData(context);
        var service = new ReportService(context);

        var req = new ReportFilterRequest { PeriodId = 1 };

        // ACT
        var gauss = await service.GetGaussDistributionAsync(req);

        // ASSERT
        gauss.Should().NotBeNull();
        gauss.Bands.Should().HaveCount(4);

        // Band 0: Xuất sắc (95 >= 90) -> count = 1 (25%)
        var excellent = gauss.Bands.First(b => b.BandCode == "EXCELLENT");
        excellent.ActualCount.Should().Be(1);
        excellent.ActualPercentage.Should().Be(25.0);
        excellent.IdealPercentage.Should().Be(15.0);

        // Band 1: Tốt (85 in [70, 89.9]) -> count = 1 (25%)
        var good = gauss.Bands.First(b => b.BandCode == "GOOD");
        good.ActualCount.Should().Be(1);

        // Band 2: Đạt (65 in [50, 69.9]) -> count = 1 (25%)
        var sat = gauss.Bands.First(b => b.BandCode == "SATISFACTORY");
        sat.ActualCount.Should().Be(1);

        // Band 3: Không đạt (45 < 50) -> count = 1 (25%)
        var unsat = gauss.Bands.First(b => b.BandCode == "UNSATISFACTORY");
        unsat.ActualCount.Should().Be(1);

        // Curve Points
        gauss.IdealCurvePoints.Should().NotBeEmpty();
    }

    #endregion

    #region 4. DataViz Extras Tests (Scatter & Funnel)

    [Fact]
    public async Task GetDataVizExtrasAsync_ShouldDetectOutliersAndStages()
    {
        using var context = CreateInMemoryDbContext();
        await SeedStandardTestData(context);
        var service = new ReportService(context);

        var req = new ReportFilterRequest { PeriodId = 1 };

        // ACT
        var extras = await service.GetDataVizExtrasAsync(req);

        // ASSERT
        extras.Should().NotBeNull();
        extras.ScatterPoints.Should().HaveCount(4);

        // Cán bộ 4: Self 65 vs Manager 45 (gap = 20 >= 15) -> Outlier
        var outlier = extras.ScatterPoints.FirstOrDefault(p => p.FullName == "Phạm Văn Bốn");
        outlier.Should().NotBeNull();
        outlier!.IsOutlier.Should().BeTrue();
        outlier.Gap.Should().Be(20.0m);

        // Funnel
        extras.FunnelStages.Should().HaveCount(5);
        extras.FunnelStages[0].Count.Should().Be(4);
    }

    #endregion

    #region 5. Export Engine Tests (ClosedXML Excel & QuestPDF & CSV)

    [Fact]
    public async Task ExportExcelAsync_ShouldGenerateValidXlsxBytes()
    {
        using var context = CreateInMemoryDbContext();
        await SeedStandardTestData(context);
        var service = new ReportService(context);

        var req = new ReportFilterRequest { PeriodId = 1 };

        // ACT
        var bytes = await service.ExportExcelAsync(req);

        // ASSERT
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(500);

        // Zip header PK\x03\x04
        bytes[0].Should().Be((byte)'P');
        bytes[1].Should().Be((byte)'K');
    }

    [Fact]
    public async Task ExportPdfAsync_ShouldGenerateValidPdfBytes()
    {
        using var context = CreateInMemoryDbContext();
        await SeedStandardTestData(context);
        var service = new ReportService(context);

        var req = new ReportFilterRequest { PeriodId = 1 };

        // ACT
        var bytes = await service.ExportPdfAsync(req);

        // ASSERT
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(500);

        // PDF header %PDF-
        var header = System.Text.Encoding.ASCII.GetString(bytes.Take(4).ToArray());
        header.Should().Be("%PDF");
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldContainHeadersAndRows()
    {
        using var context = CreateInMemoryDbContext();
        await SeedStandardTestData(context);
        var service = new ReportService(context);

        var req = new ReportFilterRequest { PeriodId = 1 };

        // ACT
        var bytes = await service.ExportCsvAsync(req);
        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        // ASSERT
        csv.Should().Contain("Hạng,Mã cán bộ,Họ và tên");
        csv.Should().Contain("Nguyễn Văn Nhất");
        csv.Should().Contain("95.0");
    }

    #endregion

    #region 6. Scheduled Report Tests

    [Fact]
    public async Task ScheduleReportAsync_ShouldCreateReportAndEmailQueue()
    {
        using var context = CreateInMemoryDbContext();
        await SeedStandardTestData(context);
        var service = new ReportService(context);

        var req = new ScheduledReportRequest
        {
            PeriodId = 1,
            Frequency = "End_Of_Period",
            Format = "Excel",
            RecipientEmails = "director@napa.edu.vn, hr@napa.edu.vn"
        };

        // ACT
        var success = await service.ScheduleReportAsync(req, 1);

        // ASSERT
        success.Should().BeTrue();

        var reports = await context.Reports.ToListAsync();
        reports.Should().HaveCount(1);
        reports[0].ReportType.Should().Be("PERFORMANCE_SUMMARY");

        var emails = await context.EmailQueues.ToListAsync();
        emails.Should().HaveCount(2);
        emails.Select(e => e.ToEmail).Should().Contain("director@napa.edu.vn");
        emails.Select(e => e.ToEmail).Should().Contain("hr@napa.edu.vn");
    }

    #endregion
}
