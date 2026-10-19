using FluentAssertions;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Domain.Entities.Evaluation;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KPI.Tests.Services;

public class Decree90ServiceTests
{
    private readonly Decree90Service _service = new();

    private KpiDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new KpiDbContext(options);
    }

    #region BR-06: Khống chế trần 20% Xuất sắc

    [Theory]
    [InlineData(10, 2)]  // 10 * 0.20 = 2 -> Floor: 2
    [InlineData(14, 2)]  // 14 * 0.20 = 2.8 -> Floor: 2
    [InlineData(15, 3)]  // 15 * 0.20 = 3.0 -> Floor: 3
    [InlineData(19, 3)]  // 19 * 0.20 = 3.8 -> Floor: 3
    [InlineData(20, 4)]  // 20 * 0.20 = 4.0 -> Floor: 4
    [InlineData(4, 0)]   // 4 * 0.20 = 0.8 -> Floor: 0
    public void CalculateMaxExcellenceQuota_ShouldFloor20Percent_BR06(int totalGood, int expectedMaxQuota)
    {
        var quota = _service.CalculateMaxExcellenceQuota(totalGood);
        quota.Should().Be(expectedMaxQuota);
    }

    [Fact]
    public void ValidateExcellenceCap_WhenExceedingQuota_ThrowsBusinessRuleException_BR06()
    {
        // 10 cán bộ tốt -> trần 20% là 2. Nếu có 3 người xuất sắc -> Vi phạm BR-06
        Action act = () => _service.ValidateExcellenceCap(10, 3);

        act.Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-06");
    }

    [Fact]
    public void ValidateExcellenceCap_WhenWithinQuota_DoesNotThrow()
    {
        // 10 cán bộ tốt -> trần 2 xuất sắc. Nếu có 2 -> Hợp lệ
        Action act = () => _service.ValidateExcellenceCap(10, 2);

        act.Should().NotThrow();
    }

    #endregion

    #region BR-07: Điểm liệt kỷ luật

    [Fact]
    public void DetermineCivilServantRank_WhenDisciplined_ReturnsKhongHoanThanh_BR07()
    {
        // Cán bộ đạt 98 điểm KPI, có sáng kiến nhưng bị kỷ luật Đảng/chính quyền
        var rank = _service.DetermineCivilServantRank(98m, hasDisciplinaryAction: true, hasApprovedInitiative: true);

        rank.Should().Be("Không hoàn thành nhiệm vụ");
    }

    [Fact]
    public void DetermineCivilServantRank_WhenDisciplinedWithLowScore_ReturnsKhongHoanThanh()
    {
        var rank = _service.DetermineCivilServantRank(40m, hasDisciplinaryAction: true, hasApprovedInitiative: false);

        rank.Should().Be("Không hoàn thành nhiệm vụ");
    }

    #endregion

    #region BR-08: Sáng kiến cải tiến

    [Fact]
    public void DetermineCivilServantRank_WhenScore95WithoutInitiative_DowngradesToTot_BR08()
    {
        // Đạt 95 điểm KPI, không kỷ luật nhưng KHÔNG có sáng kiến cải tiến
        var rank = _service.DetermineCivilServantRank(95m, hasDisciplinaryAction: false, hasApprovedInitiative: false);

        // Bị hạ về Hoàn thành tốt nhiệm vụ
        rank.Should().Be("Hoàn thành tốt nhiệm vụ");
    }

    [Fact]
    public void DetermineCivilServantRank_WhenScore95WithInitiative_ReturnsXuatSac()
    {
        // Đạt 95 điểm KPI, không kỷ luật và CÓ sáng kiến
        var rank = _service.DetermineCivilServantRank(95m, hasDisciplinaryAction: false, hasApprovedInitiative: true);

        rank.Should().Be("Hoàn thành xuất sắc nhiệm vụ");
    }

    #endregion

    #region Thang điểm và xếp loại thường

    [Fact]
    public void DetermineCivilServantRank_WhenScoreBelow50_ReturnsKhongHoanThanh()
    {
        var rank = _service.DetermineCivilServantRank(45m, hasDisciplinaryAction: false, hasApprovedInitiative: false);
        rank.Should().Be("Không hoàn thành nhiệm vụ");
    }

    [Fact]
    public void DetermineCivilServantRank_WhenScore65_ReturnsHoanThanhNhiemVu()
    {
        var rank = _service.DetermineCivilServantRank(65m, hasDisciplinaryAction: false, hasApprovedInitiative: false);
        rank.Should().Be("Hoàn thành nhiệm vụ");
    }

    [Fact]
    public void DetermineCivilServantRank_WhenScore85_ReturnsHoanThanhTotNhiemVu()
    {
        var rank = _service.DetermineCivilServantRank(85m, hasDisciplinaryAction: false, hasApprovedInitiative: false);
        rank.Should().Be("Hoàn thành tốt nhiệm vụ");
    }

    #endregion

    #region Database Integration: SaveForm0102 & BR Validation

    [Fact]
    public async Task SaveForm0102_WhenDisciplinedAndChoosingExcellent_ThrowsBR07()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var eval = new Evaluation
        {
            Id = 101,
            EmployeeId = 1,
            PeriodId = 1,
            Status = EvaluationStatus.Manager_Reviewing,
            SelfTotalScore = 95m,
            ManagerTotalScore = 95m
        };
        context.Evaluations.Add(eval);
        await context.SaveChangesAsync();

        var dbService = new Decree90Service(context);
        var req = new SaveForm01_02Request
        {
            EmployeeId = 1,
            EvaluationId = 101,
            HasDisciplinaryAction = true, // Kỷ luật
            HasApprovedInitiative = true,
            FinalizedRank = "Hoàn thành xuất sắc nhiệm vụ" // Vi phạm BR-07
        };

        // Act
        Func<Task> act = async () => await dbService.SaveForm0102Async(req, 1);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-07");
    }

    [Fact]
    public async Task SaveForm0102_WhenNoInitiativeAndChoosingExcellent_ThrowsBR08()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var eval = new Evaluation
        {
            Id = 102,
            EmployeeId = 2,
            PeriodId = 1,
            Status = EvaluationStatus.Manager_Reviewing,
            SelfTotalScore = 95m,
            ManagerTotalScore = 95m
        };
        context.Evaluations.Add(eval);
        await context.SaveChangesAsync();

        var dbService = new Decree90Service(context);
        var req = new SaveForm01_02Request
        {
            EmployeeId = 2,
            EvaluationId = 102,
            HasDisciplinaryAction = false,
            HasApprovedInitiative = false, // Không có sáng kiến
            FinalizedRank = "Hoàn thành xuất sắc nhiệm vụ" // Vi phạm BR-08
        };

        // Act
        Func<Task> act = async () => await dbService.SaveForm0102Async(req, 1);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-08");
    }

    [Fact]
    public async Task SaveForm0102_WhenValid_SavesSuccessfullyAndWritesAudit()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var eval = new Evaluation
        {
            Id = 103,
            EmployeeId = 3,
            PeriodId = 1,
            Status = EvaluationStatus.Manager_Reviewing,
            SelfTotalScore = 88m,
            ManagerTotalScore = 88m
        };
        context.Evaluations.Add(eval);
        await context.SaveChangesAsync();

        var dbService = new Decree90Service(context);
        var req = new SaveForm01_02Request
        {
            EmployeeId = 3,
            EvaluationId = 103,
            PoliticalEthicsScore = 90,
            WorkDisciplineScore = 90,
            DutyExecutionScore = 88,
            LeadershipCapacityScore = 85,
            InnovationScore = 88,
            HasDisciplinaryAction = false,
            HasApprovedInitiative = true,
            CollectiveComments = "Đồng ý đề xuất tốt.",
            PartyOpinion = "Chi ủy nhất trí.",
            ProposedRank = "Hoàn thành tốt nhiệm vụ",
            FinalizedRank = "Hoàn thành tốt nhiệm vụ"
        };

        // Act
        var result = await dbService.SaveForm0102Async(req, 99);

        // Assert
        result.Should().BeTrue();
        var savedEval = await context.Evaluations.Include(e => e.CivilServantClassification).FirstOrDefaultAsync(e => e.Id == 103);
        savedEval.Should().NotBeNull();
        savedEval!.FinalClassification.Should().Be("Hoàn thành tốt nhiệm vụ");
        savedEval.CivilServantClassification.Should().NotBeNull();
        savedEval.CivilServantClassification!.FinalizedRank.Should().Be("Hoàn thành tốt nhiệm vụ");

        // Kiểm tra AuditLog
        var audit = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "CIVIL_SERVANT_CLASSIFICATION");
        audit.Should().NotBeNull();
        audit!.UserId.Should().Be(99);
    }

    #endregion
}
