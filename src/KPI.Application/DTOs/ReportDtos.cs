namespace KPI.Application.DTOs;

/// <summary>
/// Yêu cầu lọc báo cáo đa chiều (Filter Request)
/// </summary>
public class ReportFilterRequest
{
    public long PeriodId { get; set; } = 1;
    public List<long>? DepartmentIds { get; set; }
    public List<long>? PositionIds { get; set; }
    public List<string>? Statuses { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "ScoreDesc"; // ScoreDesc, ScoreAsc, Department, Classification
    public string? Preset { get; set; } // "this_quarter", "this_year", "my_dept"
}

/// <summary>
/// Thẻ thống kê tổng quan (Summary Cards)
/// </summary>
public class ExecutiveReportSummaryDto
{
    public int TotalEmployees { get; set; }
    public int EvaluatedEmployees { get; set; }
    public double CompletionRate { get; set; }
    public decimal AverageScore { get; set; }
    public decimal HighestScore { get; set; }
    public string HighestEmployeeName { get; set; } = string.Empty;
    public decimal LowestScore { get; set; }
    public string LowestEmployeeName { get; set; } = string.Empty;
    public decimal StandardDeviation { get; set; }
    public bool IsGaussCompliant { get; set; }
    public string ComplianceStatus { get; set; } = "Đạt chuẩn"; // "Đạt chuẩn" (✅), "Lệch chuẩn" (⚠️)
    public string ComplianceMessage { get; set; } = string.Empty;
}

/// <summary>
/// Dòng dữ liệu Bảng Xếp Hạng Hiệu Suất (Leaderboard Item)
/// </summary>
public class LeaderboardItemDto
{
    public int Rank { get; set; }
    public int PreviousRank { get; set; }
    public string TrendDirection { get; set; } = "Same"; // "Up", "Down", "Same"
    public int TrendDiff { get; set; } // Số bậc tăng hoặc giảm (+2, -1, 0)

    public long EmployeeId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = "/images/default-avatar.png";
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;

    public decimal SelfScore { get; set; }
    public decimal ManagerScore { get; set; }
    public decimal FinalScore { get; set; }
    public string Classification { get; set; } = string.Empty;

    public string BadgeType { get; set; } = string.Empty; // "Gold", "Silver", "Bronze", ""
    public bool IsTop3 => Rank <= 3;
}

/// <summary>
/// Phân bổ Chuông Gauss (Bell Curve Distribution)
/// </summary>
public class GaussBandDto
{
    public string BandCode { get; set; } = string.Empty; // "EXCELLENT", "GOOD", "SATISFACTORY", "UNSATISFACTORY"
    public string BandName { get; set; } = string.Empty; // "Xuất sắc", "Tốt", "Đạt", "Không đạt"
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public int ActualCount { get; set; }
    public double ActualPercentage { get; set; }
    public double IdealPercentage { get; set; } // Chuẩn: 15% (Xuất sắc), 70% (Tốt), 10% (Đạt), 5% (Không đạt)
    public double Deviation => Math.Round(ActualPercentage - IdealPercentage, 1);
    public bool IsCompliant => Math.Abs(Deviation) <= 5.0; // Biên độ dung sai cho phép 5%

    public List<LeaderboardItemDto> Employees { get; set; } = new();
}

public class GaussPointDto
{
    public double X { get; set; } // Điểm số (0 đến 100)
    public double Y { get; set; } // Tần suất mật độ xác suất f(x)
}

public class GaussDistributionDto
{
    public decimal Mean { get; set; }
    public decimal StdDev { get; set; }
    public bool IsCompliant { get; set; }
    public string ComplianceMessage { get; set; } = string.Empty;

    public List<GaussBandDto> Bands { get; set; } = new();
    public List<GaussPointDto> IdealCurvePoints { get; set; } = new();
}

/// <summary>
/// Phân tích Xu hướng (Trend Analysis)
/// </summary>
public class PeriodTrendPointDto
{
    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public int EmployeeCount { get; set; }
}

public class DepartmentComparisonDto
{
    public long DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal CurrentScore { get; set; }
    public decimal PreviousScore { get; set; }
    public decimal Difference => CurrentScore - PreviousScore;
    public string TrendDirection => Difference > 0 ? "Up" : (Difference < 0 ? "Down" : "Same");
}

public class HeatmapCellDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public string PeriodOrMonth { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string IntensityColor { get; set; } = string.Empty; // Hex color based on score
}

public class TrendAnalysisDto
{
    public List<PeriodTrendPointDto> OverallTrends { get; set; } = new();
    public List<DepartmentComparisonDto> DepartmentComparisons { get; set; } = new();
    public List<HeatmapCellDto> HeatmapMatrix { get; set; } = new();
}

/// <summary>
/// Trực quan hóa nâng cao (Data Visualization Extras)
/// </summary>
public class ScatterPointDto
{
    public long EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal SelfScore { get; set; }      // Trục X
    public decimal ManagerScore { get; set; }   // Trục Y
    public decimal FinalScore { get; set; }
    public decimal Gap => Math.Abs(SelfScore - ManagerScore);
    public bool IsOutlier => Gap >= 15.0m;      // Điểm chênh lệch lệch pha >= 15 điểm
}

public class BoxPlotDepartmentDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Min { get; set; }
    public decimal Q1 { get; set; }
    public decimal Median { get; set; }
    public decimal Q3 { get; set; }
    public decimal Max { get; set; }
}

public class FunnelStageDto
{
    public string StageName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class DataVizExtrasDto
{
    public List<ScatterPointDto> ScatterPoints { get; set; } = new();
    public List<BoxPlotDepartmentDto> BoxPlots { get; set; } = new();
    public List<FunnelStageDto> FunnelStages { get; set; } = new();
}

/// <summary>
/// Bộ lọc đã lưu (Saved Filter)
/// </summary>
public class SavedFilterDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string FilterJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Yêu cầu đặt lịch gửi email báo cáo (Scheduled Report Request)
/// </summary>
public class ScheduledReportRequest
{
    public long PeriodId { get; set; }
    public string Frequency { get; set; } = "End_Of_Period"; // "Weekly", "Monthly", "End_Of_Period"
    public string RecipientEmails { get; set; } = string.Empty;
    public string Format { get; set; } = "Excel"; // "Excel", "PDF", "CSV"
}
