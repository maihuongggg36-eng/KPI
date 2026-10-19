using KPI.Application.Common.Exceptions;

namespace KPI.Application.Services;

public interface IKpiCalculationService
{
    void ValidateWeights(IEnumerable<decimal> weights);
    decimal CalculateCompletionRate(decimal actual, decimal target, decimal maxCap = 150.0m, string formula = "HigherIsBetter");
    decimal CalculateWeightedScore(decimal completionRate, decimal weight);
    void ValidateScoreGap(decimal selfScore, decimal managerScore, string? reason);
}

/// <summary>
/// Dịch vụ tính toán chỉ số KPI và kiểm tra các quy tắc nghiệp vụ BR-01, BR-02, BR-05, BR-10
/// </summary>
public class KpiCalculationService : IKpiCalculationService
{
    /// <summary>
    /// BR-01: Tổng trọng số = 100.0%
    /// BR-02: Mỗi chỉ tiêu có trọng số từ 5% đến 50%
    /// </summary>
    public void ValidateWeights(IEnumerable<decimal> weights)
    {
        var weightList = weights.ToList();
        if (!weightList.Any())
        {
            throw new BusinessRuleException("BR-01", "Danh sách chỉ tiêu KPI không được để trống.");
        }

        foreach (var w in weightList)
        {
            if (w < 5.0m || w > 50.0m)
            {
                throw new BusinessRuleException("BR-02", $"Trọng số của mỗi chỉ tiêu phải nằm trong khoảng [5.0%, 50.0%]. Giá trị thực tế: {w}%");
            }
        }

        var total = weightList.Sum();
        if (Math.Abs(total - 100.0m) > 0.01m)
        {
            throw new BusinessRuleException("BR-01", $"Tổng trọng số các chỉ tiêu phải bằng chính xác 100.00%. Giá trị hiện tại: {total}%");
        }
    }

    /// <summary>
    /// BR-10: Tính tỷ lệ hoàn thành có áp trần MaxCap (mặc định 150%)
    /// </summary>
    public decimal CalculateCompletionRate(decimal actual, decimal target, decimal maxCap = 150.0m, string formula = "HigherIsBetter")
    {
        if (target <= 0) return 0m;

        decimal rate;
        if (formula.Equals("LowerIsBetter", StringComparison.OrdinalIgnoreCase))
        {
            rate = (target / Math.Max(actual, 0.001m)) * 100.0m;
        }
        else
        {
            rate = (actual / target) * 100.0m;
        }

        // Chặn trần MaxCap theo BR-10
        if (rate > maxCap)
        {
            rate = maxCap;
        }

        return Math.Round(rate, 2);
    }

    /// <summary>
    /// Tính điểm chỉ tiêu theo trọng số: Điểm = (Tỷ lệ hoàn thành * Trọng số) / 100
    /// </summary>
    public decimal CalculateWeightedScore(decimal completionRate, decimal weight)
    {
        return Math.Round((completionRate * weight) / 100.0m, 2);
    }

    /// <summary>
    /// BR-05: Khi chênh lệch giữa Tự chấm và Quản lý chấm > 10 điểm, bắt buộc có giải trình >= 50 ký tự
    /// </summary>
    public void ValidateScoreGap(decimal selfScore, decimal managerScore, string? reason)
    {
        var gap = Math.Abs(selfScore - managerScore);
        if (gap > 10.0m)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 50)
            {
                throw new BusinessRuleException("BR-05", 
                    $"Độ lệch điểm giữa tự chấm ({selfScore}) và quản lý thẩm định ({managerScore}) vượt quá 10 điểm (|gap| = {gap}). Bắt buộc nhập lý do giải trình tối thiểu 50 ký tự.");
            }
        }
    }
}
