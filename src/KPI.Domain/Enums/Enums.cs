namespace KPI.Domain.Enums;

/// <summary>
/// Trạng thái tài khoản người dùng
/// </summary>
public enum UserStatus
{
    Pending_Activation = 1,
    Active = 2,
    Locked = 3,
    Suspended = 4,
    Deactivated = 5
}

/// <summary>
/// Loại cơ quan, tổ chức
/// </summary>
public enum OrgType
{
    Enterprise = 1,
    PublicAdministration = 2,
    PublicService = 3
}

/// <summary>
/// 4 Viễn cảnh BSC + Đạo đức công vụ
/// </summary>
public enum KpiPerspective
{
    Financial = 1,
    Customer = 2,
    InternalProcess = 3,
    LearningGrowth = 4,
    PublicServiceEthics = 5
}

/// <summary>
/// Lĩnh vực áp dụng chỉ số KPI
/// </summary>
public enum TargetSector
{
    Enterprise = 1,
    PublicSector = 2,
    All = 3
}

/// <summary>
/// Công thức tính điểm chỉ số
/// </summary>
public enum ScoringFormula
{
    HigherIsBetter = 1,
    LowerIsBetter = 2,
    TargetRange = 3
}

/// <summary>
/// Loại chu kỳ đánh giá
/// </summary>
public enum PeriodType
{
    Monthly = 1,
    Quarterly = 2,
    SemiAnnual = 3,
    Annual = 4
}

/// <summary>
/// Trạng thái kỳ đánh giá
/// </summary>
public enum PeriodStatus
{
    Planning = 1,
    Open = 2,
    InEvaluation = 3,
    Closing = 4,
    Closed = 5,
    Archived = 6
}

/// <summary>
/// Trạng thái bản giao KPI cá nhân
/// </summary>
public enum AssignmentStatus
{
    Draft = 1,
    Pending_Staff_Confirm = 2,
    Pending_Approval = 3,
    Approved_Locked = 4,
    In_Evaluation = 5,
    Completed = 6,
    Archived = 7
}

/// <summary>
/// Trạng thái phiếu đánh giá hiệu suất
/// </summary>
public enum EvaluationStatus
{
    Not_Started = 1,
    Self_Assessing = 2,
    Self_Submitted = 3,
    Manager_Reviewing = 4,
    Pending_Director_Approval = 5,
    Final_Approved = 6,
    Locked = 7
}

/// <summary>
/// Tình trạng đảng viên
/// </summary>
public enum PartyStatus
{
    None = 0,
    Probationary = 1,
    Official = 2
}

/// <summary>
/// Trình độ lý luận chính trị
/// </summary>
public enum PoliticalTheory
{
    None = 0,
    Primary = 1,
    Intermediate = 2,
    High = 3
}

/// <summary>
/// Trạng thái khiếu nại đánh giá (BR-14)
/// </summary>
public enum DisputeStatus
{
    Open = 1,
    UnderReview = 2,
    Resolved = 3,
    Rejected = 4
}

/// <summary>
/// Loại thông báo hệ thống
/// </summary>
public enum NotificationType
{
    Info = 1,
    Warning = 2,
    Success = 3,
    Error = 4,
    Urgent = 5
}

/// <summary>
/// Trạng thái hàng đợi email
/// </summary>
public enum EmailStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}

/// <summary>
/// Định dạng xuất báo cáo
/// </summary>
public enum ExportFormat
{
    XLSX = 1,
    PDF = 2,
    CSV = 3
}
