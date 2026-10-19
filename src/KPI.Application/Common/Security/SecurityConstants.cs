namespace KPI.Application.Common.Security;

/// <summary>
/// Phạm vi truy xuất dữ liệu (Data Scope Authorization)
/// </summary>
public enum DataScope
{
    Self = 1,          // Chỉ xem/sửa dữ liệu của bản thân (Chuyên viên / Giảng viên)
    Department = 2,    // Xem/quản lý toàn bộ nhân sự trong phòng ban trực thuộc (Trưởng phòng / Trưởng khoa)
    Organization = 3   // Xem/phê duyệt toàn bộ cơ quan (Giám đốc, HR, Kế toán, Admin)
}

/// <summary>
/// 6 Vai trò chuẩn trong toàn hệ thống
/// </summary>
public static class AppRoles
{
    public const string Admin = "ADMIN";
    public const string Director = "DIRECTOR";
    public const string Manager = "MANAGER";
    public const string Officer = "OFFICER";
    public const string HrAdmin = "HR_ADMIN";
    public const string Accountant = "ACCOUNTANT";

    public static DataScope GetDefaultDataScope(string role) => role switch
    {
        Director => DataScope.Organization,
        Admin => DataScope.Organization,
        HrAdmin => DataScope.Organization,
        Accountant => DataScope.Organization,
        Manager => DataScope.Department,
        _ => DataScope.Self
    };
}

/// <summary>
/// Danh mục Quyền hạn theo Module (Permission Matrix)
/// Quy tắc: CHỈ ADMIN có toàn bộ quyền. Các vai trò khác chỉ có quyền do ADMIN phân quyền.
/// </summary>
public static class AppPermissions
{
    public static class Admin
    {
        public const string FullAccess = "Admin.FullAccess";
        public const string ManageUsers = "Admin.ManageUsers";             // CRUD User, sửa thông tin cá nhân (tên, tuổi...), khóa/mở
        public const string ResetPassword = "Admin.ResetPassword";         // Cấp lại mật khẩu cho User
        public const string ManageMenus = "Admin.ManageMenus";             // Thêm/Sửa/Xóa/Bật tắt menu các đầu mục
        public const string ManagePermissions = "Admin.ManagePermissions"; // Phân quyền cho từng User / Role
        public const string ViewAuditLogs = "Admin.ViewAuditLogs";         // Xem toàn bộ nhật ký hệ thống
    }

    public static class KpiPlanning
    {
        public const string View = "KpiPlanning.View";
        public const string Create = "KpiPlanning.Create";
        public const string Assign = "KpiPlanning.Assign";
        public const string Approve = "KpiPlanning.Approve";
    }

    public static class DirectorTarget
    {
        public const string View = "DirectorTarget.View";
        public const string Manage = "DirectorTarget.Manage";              // Đặt mục tiêu BGĐ (vượt mục tiêu mới đạt Tốt/Xuất sắc)
    }

    public static class Attendance
    {
        public const string View = "Attendance.View";                      // Xem bảng chấm công
        public const string Manage = "Attendance.Manage";                  // Nhập/Sửa chấm công, tính điểm 20%
    }

    public static class Evaluation
    {
        public const string SelfAssess = "Evaluation.SelfAssess";
        public const string ManagerReview = "Evaluation.ManagerReview";
        public const string FinalApprove = "Evaluation.FinalApprove";
        public const string Dispute = "Evaluation.Dispute";
    }

    public static class Decree90
    {
        public const string View = "Decree90.View";
        public const string FinalizeRank = "Decree90.FinalizeRank";
        public const string ManageCap = "Decree90.ManageCap";
    }

    public static class Payroll
    {
        public const string View = "Payroll.View";
        public const string Calculate = "Payroll.Calculate";
        public const string PayoutCash = "Payroll.PayoutCash";
        public const string ViewOwnPayslip = "Payroll.ViewOwnPayslip";
    }

    public static class Report
    {
        public const string View = "Report.View";
        public const string Export = "Report.Export";
    }

    public static class Dashboard
    {
        public const string View = "Dashboard.View";
        public const string Executive = "Dashboard.Executive";
    }

    public static class System
    {
        public const string ManageUsers = "System.ManageUsers";
        public const string ManageCatalogs = "System.ManageCatalogs";
        public const string ViewAuditLogs = "System.ViewAuditLogs";
    }

    /// <summary>
    /// Danh sách tất cả các mã quyền hạn trong hệ thống kèm mô tả để hiển thị trên UI phân quyền
    /// </summary>
    public static List<(string Code, string Module, string Name, string Description)> GetAllPermissions()
    {
        return new List<(string Code, string Module, string Name, string Description)>
        {
            // Quản trị Admin
            (Admin.ManageUsers, "Quản trị Admin", "Quản lý Người dùng", "Tạo, sửa tên, tuổi, phòng ban, khóa/mở tài khoản"),
            (Admin.ResetPassword, "Quản trị Admin", "Cấp lại Mật khẩu", "Admin cấp lại mật khẩu cho bất kỳ tài khoản nào"),
            (Admin.ManageMenus, "Quản trị Admin", "Quản lý Menu Đầu mục", "Thêm, sửa, xóa, sắp xếp và bật tắt menu hệ thống"),
            (Admin.ManagePermissions, "Quản trị Admin", "Phân quyền Hệ thống", "Cấp và thu hồi quyền hạn cho từng vai trò và người dùng"),
            (Admin.ViewAuditLogs, "Quản trị Admin", "Xem Nhật ký Kiểm toán", "Truy cập nhật ký kiểm toán và an toàn thông tin"),

            // Kế hoạch & Giao KPI
            (DirectorTarget.Manage, "Mục tiêu Ban Giám đốc", "Thiết lập Mục tiêu BGĐ", "Tạo và ban hành mục tiêu toàn viện / từng khoa"),
            (DirectorTarget.View, "Mục tiêu Ban Giám đốc", "Xem Mục tiêu BGĐ", "Xem danh sách chỉ tiêu thách thức của BGĐ"),
            (KpiPlanning.View, "Kế hoạch KPI", "Xem Kế hoạch KPI", "Xem danh sách kế hoạch và bản giao KPI"),
            (KpiPlanning.Create, "Kế hoạch KPI", "Tạo Kế hoạch KPI", "Tạo mới khung kế hoạch KPI theo kỳ"),
            (KpiPlanning.Assign, "Kế hoạch KPI", "Giao chỉ tiêu KPI", "Phân bổ và giao chỉ tiêu cho giảng viên/nhân viên"),
            (KpiPlanning.Approve, "Kế hoạch KPI", "Phê duyệt Kế hoạch", "Duyệt bản giao kế hoạch KPI của khoa/phòng"),

            // Chấm công & Kỷ luật
            (Attendance.View, "Chấm công", "Xem Chấm công", "Xem bảng chấm công và kỷ luật lao động"),
            (Attendance.Manage, "Chấm công", "Quản lý Chấm công", "Nhập, sửa ngày công, điểm danh, tính điểm 20%"),

            // Đánh giá & Thẩm định
            (Evaluation.SelfAssess, "Đánh giá KPI", "Tự đánh giá", "Nhập kết quả tự chấm điểm và cung cấp minh chứng"),
            (Evaluation.ManagerReview, "Đánh giá KPI", "Thẩm định điểm Quản lý", "Lãnh đạo khoa/phòng chấm điểm thẩm định"),
            (Evaluation.FinalApprove, "Đánh giá KPI", "Phê duyệt Kết quả", "Ban Giám đốc phê duyệt kết quả đánh giá cuối cùng"),
            (Evaluation.Dispute, "Đánh giá KPI", "Khiếu nại Đánh giá", "Gửi và xử lý đơn khiếu nại kết quả KPI"),

            // Nghị định 90 & 48
            (Decree90.View, "Nghị định 90", "Xem Xếp loại NĐ 90", "Xem danh sách và kết quả xếp loại cán bộ công chức"),
            (Decree90.FinalizeRank, "Nghị định 90", "Xác nhận Xếp loại", "Chốt xếp loại 4 mức theo Nghị định 90"),
            (Decree90.ManageCap, "Nghị định 90", "Kiểm soát Trần 20%", "Kiểm tra và áp dụng khống chế trần 20% Xuất sắc"),

            // Lương & Chi trả tiền mặt
            (Payroll.View, "Lương & Tài chính", "Xem Bảng lương", "Xem tổng hợp bảng lương 3P toàn cơ quan"),
            (Payroll.Calculate, "Lương & Tài chính", "Tính toán Bảng lương", "Chạy công thức tính lương ngạch bậc, phụ cấp, KPI"),
            (Payroll.PayoutCash, "Lương & Tài chính", "Chi trả Tiền mặt", "Thực hiện lập phiếu chi và ghi nhận ký nhận tiền mặt"),
            (Payroll.ViewOwnPayslip, "Lương & Tài chính", "Xem Phiếu lương cá nhân", "Cán bộ xem phiếu chi tiết lương của chính mình"),

            // Báo cáo & Dashboard
            (Dashboard.View, "Tổng quan", "Xem Dashboard cơ bản", "Bàn làm việc và thông báo cá nhân"),
            (Dashboard.Executive, "Tổng quan", "Dashboard Lãnh đạo", "Xem biểu đồ tổng thể, tiến độ toàn viện"),
            (Report.View, "Báo cáo", "Xem Báo cáo Tổng hợp", "Xem báo cáo KPI, chuông Gauss, bảng xếp hạng"),
            (Report.Export, "Báo cáo", "Xuất Báo cáo", "Xuất file Excel / PDF báo cáo"),

            // Danh mục
            (System.ManageCatalogs, "Hệ thống", "Quản lý Danh mục", "Quản lý các bảng danh mục dùng chung")
        };
    }
}
