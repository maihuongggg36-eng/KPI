using FluentAssertions;
using KPI.Application.Common.Exceptions;
using KPI.Application.Services;
using Xunit;

namespace KPI.Tests.Services;

public class KpiCalculationServiceTests
{
    private readonly KpiCalculationService _service = new();

    [Fact]
    public void ValidateWeights_SumNot100_ThrowsBusinessRuleException_BR01()
    {
        // Arrange: Tổng 90% != 100%
        var weights = new List<decimal> { 30m, 30m, 30m };

        // Act
        Action act = () => _service.ValidateWeights(weights);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-01");
    }

    [Fact]
    public void ValidateWeights_WeightLessThan5Percent_ThrowsBusinessRuleException_BR02()
    {
        // Arrange: Có chỉ tiêu trọng số 3% < 5%
        var weights = new List<decimal> { 3m, 47m, 50m };

        // Act
        Action act = () => _service.ValidateWeights(weights);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-02");
    }

    [Fact]
    public void ValidateWeights_WeightGreaterThan50Percent_ThrowsBusinessRuleException_BR02()
    {
        // Arrange: Có chỉ tiêu trọng số 55% > 50%
        var weights = new List<decimal> { 55m, 25m, 20m };

        // Act
        Action act = () => _service.ValidateWeights(weights);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-02");
    }

    [Fact]
    public void ValidateWeights_ValidWeights_Succeeds()
    {
        // Arrange: Hợp lệ tổng 100%, mỗi chỉ tiêu trong [5%, 50%]
        var weights = new List<decimal> { 40m, 30m, 20m, 10m };

        // Act
        Action act = () => _service.ValidateWeights(weights);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void CalculateCompletionRate_ExceedsMaxCap_CappedAt150_BR10()
    {
        // Arrange: Đạt 200/100 -> Tỷ lệ tính toán 200%, trần 150%
        var rate = _service.CalculateCompletionRate(200m, 100m, 150m);

        // Assert
        rate.Should().Be(150.0m);
    }

    [Fact]
    public void ValidateScoreGap_GapExceeds10_WithoutExplanation_ThrowsBusinessRuleException_BR05()
    {
        // Arrange: Tự chấm 95, Quản lý chấm 80 (lệch 15 điểm > 10 điểm), giải trình < 50 ký tự
        var reason = "Quá ngắn";

        // Act
        Action act = () => _service.ValidateScoreGap(95m, 80m, reason);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == "BR-05");
    }

    [Fact]
    public void ValidateScoreGap_GapExceeds10_WithValidExplanation_Succeeds_BR05()
    {
        // Arrange: Giải trình đầy đủ >= 50 ký tự
        var reason = "Cán bộ tự đánh giá chưa trừ điểm chậm hạn nộp báo cáo chuyên đề và chưa tính đến sai sót trong hồ sơ số liệu.";

        // Act
        Action act = () => _service.ValidateScoreGap(95m, 80m, reason);

        // Assert
        act.Should().NotThrow();
    }
}
