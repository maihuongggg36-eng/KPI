namespace KPI.Application.DTOs;

/// <summary>
/// DTO tổng quan theo dõi cán bộ công chức trong kỳ đánh giá
/// </summary>
public class CivilServantOverviewDto
{
    public long EmployeeId { get; set; }
    public long CivilServantProfileId { get; set; }
    public string CivilServantCode { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = "/images/default-avatar.png";

    public long DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public bool IsLeadership { get; set; }

    public string OfficialRankCode { get; set; } = string.Empty;
    public string OfficialRankName { get; set; } = string.Empty;
    public int SalaryGrade { get; set; }
    public decimal SalaryCoefficient { get; set; }

    // Kết quả đánh giá hiệu suất
    public long? EvaluationId { get; set; }
    public decimal KpiScore { get; set; }
    public string? FinalClassification { get; set; }

    // Các điều kiện nghiệp vụ theo Nghị định 90 & 48
    public bool HasDisciplinaryAction { get; set; } // BR-07: Kỷ luật
    public bool HasApprovedInitiative { get; set; } // BR-08: Sáng kiến
    public string ProposedRank { get; set; } = string.Empty;
    public string FinalizedRank { get; set; } = string.Empty;

    public string RankBadgeClass => GetRankBadgeClass(FinalizedRank ?? FinalClassification ?? ProposedRank);

    public static string GetRankBadgeClass(string? rank) => rank switch
    {
        var r when r != null && (r.Contains("xuất sắc") || r.Contains("Xuất sắc")) => "badge-gold",
        var r when r != null && (r.Contains("tốt") || r.Contains("Tốt")) => "badge-success",
        var r when r != null && (r.Contains("Không hoàn thành") || r.Contains("không hoàn thành")) => "badge-danger",
        var r when !string.IsNullOrEmpty(r) => "badge-primary",
        _ => "badge-neutral"
    };
}

/// <summary>
/// DTO kiểm soát tỷ lệ khống chế 20% Xuất sắc theo Nghị định 90 (BR-06)
/// </summary>
public class CivilServantQuotaStatusDto
{
    public int TotalCivilServants { get; set; }
    public int CountGoodAndAbove { get; set; } // Số lượng Hoàn thành tốt trở lên
    public int CurrentExcellentCount { get; set; } // Số lượng Hoàn thành xuất sắc hiện tại
    public int MaxAllowedExcellent { get; set; } // Trần tối đa cho phép: floor(0.20 * CountGoodAndAbove)
    public double CurrentRate { get; set; } // Tỷ lệ % hiện tại: (CurrentExcellent / CountGoodAndAbove) * 100
    public bool IsCapExceeded => CurrentExcellentCount > MaxAllowedExcellent;
    public bool IsCapReached => CurrentExcellentCount >= MaxAllowedExcellent;
    public string WarningMessage { get; set; } = string.Empty;
}

/// <summary>
/// DTO Biểu mẫu Quốc gia Mẫu 01 (Lãnh đạo) & Mẫu 02 (Chuyên viên)
/// </summary>
public class Form01_02Dto
{
    public long EmployeeId { get; set; }
    public long? EvaluationId { get; set; }
    public long PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int Year { get; set; }

    // Thông tin cán bộ
    public string FullName { get; set; } = string.Empty;
    public string CivilServantCode { get; set; } = string.Empty;
    public string DateOfBirthFormatted { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public decimal SalaryCoefficient { get; set; }
    public string PartyStatusName { get; set; } = string.Empty;
    public bool IsLeadership { get; set; } // true: Mẫu 01, false: Mẫu 02

    // 6 Tiêu chí đánh giá theo Nghị định 90 & 48
    public int PoliticalEthicsScore { get; set; } = 95; // 1. Chính trị tư tưởng, đạo đức
    public int WorkDisciplineScore { get; set; } = 95;   // 2. Ý thức tổ chức kỷ luật, tác phong
    public int DutyExecutionScore { get; set; } = 90;    // 3. Kết quả KPI chức trách nhiệm vụ
    public int LeadershipCapacityScore { get; set; } = 90; // 4. Năng lực lãnh đạo (Chỉ Mẫu 01)
    public int InnovationScore { get; set; } = 90;       // 5. Đổi mới sáng tạo, chuyển đổi số

    public decimal TotalCalculatedScore { get; set; } = 92.0m;

    // Điều kiện pháp lý
    public bool HasDisciplinaryAction { get; set; } // BR-07
    public bool HasApprovedInitiative { get; set; } // BR-08
    public string? InitiativeName { get; set; }

    // Ý kiến đóng góp & nhận xét
    public string? CollectiveComments { get; set; } // Ý kiến tập thể phòng ban
    public string? PartyOpinion { get; set; }        // Ý kiến Chi ủy / Cấp ủy nơi công tác
    public string ProposedRank { get; set; } = "Hoàn thành tốt nhiệm vụ";
    public string FinalizedRank { get; set; } = "Hoàn thành tốt nhiệm vụ";
    public string? DirectorFeedback { get; set; }
}

/// <summary>
/// Yêu cầu lưu kết quả phân loại Mẫu 01/02
/// </summary>
public class SaveForm01_02Request
{
    public long EmployeeId { get; set; }
    public long EvaluationId { get; set; }
    public int PoliticalEthicsScore { get; set; }
    public int WorkDisciplineScore { get; set; }
    public int DutyExecutionScore { get; set; }
    public int LeadershipCapacityScore { get; set; }
    public int InnovationScore { get; set; }
    public bool HasDisciplinaryAction { get; set; }
    public bool HasApprovedInitiative { get; set; }
    public string? InitiativeName { get; set; }
    public string? CollectiveComments { get; set; }
    public string? PartyOpinion { get; set; }
    public string ProposedRank { get; set; } = string.Empty;
    public string FinalizedRank { get; set; } = string.Empty;
}

/// <summary>
/// DTO Dashboard Thống kê Công chức & Phân bổ Gauss
/// </summary>
public class CivilServantDashboardDto
{
    public int TotalCivilServants { get; set; }
    public int CountExcellent { get; set; }
    public int CountGood { get; set; }
    public int CountSatisfactory { get; set; }
    public int CountUnsatisfactory { get; set; }

    // Tỷ lệ thực tế %
    public double RateExcellent { get; set; }
    public double RateGood { get; set; }
    public double RateSatisfactory { get; set; }
    public double RateUnsatisfactory { get; set; }

    // Tỷ lệ chuẩn Gauss (15% - 70% - 10% - 5%)
    public double GaussStandardExcellent => 15.0;
    public double GaussStandardGood => 70.0;
    public double GaussStandardSatisfactory => 10.0;
    public double GaussStandardUnsatisfactory => 5.0;

    public CivilServantQuotaStatusDto QuotaStatus { get; set; } = new();

    // Xu hướng qua 3 kỳ gần nhất
    public List<string> TrendPeriods { get; set; } = new();
    public List<double> TrendExcellentRates { get; set; } = new();
    public List<double> TrendGoodRates { get; set; } = new();
}
