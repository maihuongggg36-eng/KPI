using System.Security.Cryptography;
using System.Text;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KPI.Infrastructure.Data.Seed;

/// <summary>
/// Trình khởi tạo dữ liệu mẫu hạt giống (Seed Data) chuẩn hóa cho hệ thống KPI
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(KpiDbContext context, ILogger logger)
    {
        try
        {
            // Luôn đồng bộ 4 bảng mới và nạp dữ liệu hạt giống bổ sung
            await EnsureSchemaAndNewComponentsAsync(context, logger);

            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Cơ sở dữ liệu đã chứa dữ liệu người dùng, bỏ qua nạp User/Organization.");
                return;
            }

            logger.LogInformation("Bắt đầu nạp Dữ liệu mẫu Hạt giống (Seed Data)...");

            // 1. Phân hệ Roles (Vai trò)
            var roleAdmin = new Role { Code = "ADMIN", Name = "Quản trị viên Hệ thống", Description = "Toàn quyền quản trị danh mục và hệ thống", IsSystemDefault = true };
            var roleDirector = new Role { Code = "DIRECTOR", Name = "Giám đốc / Lãnh đạo cấp cao", Description = "Phê chuẩn kết luận, duyệt trần Nghị định 90", IsSystemDefault = true };
            var roleManager = new Role { Code = "MANAGER", Name = "Trưởng phòng / Quản lý chuyên môn", Description = "Giao KPI, chấm thẩm định điểm", IsSystemDefault = true };
            var roleOfficer = new Role { Code = "OFFICER", Name = "Cán bộ chuyên viên / Nhân viên", Description = "Nhập kết quả, tự chấm điểm, xem biểu mẫu", IsSystemDefault = true };
            var roleHr = new Role { Code = "HR_ADMIN", Name = "Cán bộ Tổ chức Cán bộ (HR)", Description = "Theo dõi nhân sự, kiểm soát tỷ lệ 20% khống chế", IsSystemDefault = true };
            var roleAccountant = new Role { Code = "ACCOUNTANT", Name = "Kế toán viên (Lương & Tiền mặt)", Description = "Xem KPI, tính toán lương theo kết quả KPI, chi tiền mặt", IsSystemDefault = true };

            context.Roles.AddRange(roleAdmin, roleDirector, roleManager, roleOfficer, roleHr, roleAccountant);
            await context.SaveChangesAsync();

            // 2. Cơ cấu Tổ chức
            var org = new Organization
            {
                Code = "NAPA",
                Name = "Học viện Hành chính và Quản trị công",
                TaxCode = "0101234567",
                Address = "Số 77 Nguyễn Chí Thanh, Đống Đa, Hà Nội",
                OrgType = OrgType.PublicAdministration,
                Status = "Active"
            };
            context.Organizations.Add(org);
            await context.SaveChangesAsync();

            // Phòng ban
            var deptBgd = new Department { OrgId = org.Id, Code = "BGD", Name = "Ban Giám đốc", TreePath = "/BGD", Level = 1 };
            var deptDaoTao = new Department { OrgId = org.Id, Code = "DT", Name = "Phòng Quản lý Đào tạo & Bồi dưỡng", TreePath = "/DT", Level = 1 };
            var deptTccb = new Department { OrgId = org.Id, Code = "TCCB", Name = "Phòng Tổ chức Cán bộ", TreePath = "/TCCB", Level = 1 };
            var deptKhtc = new Department { OrgId = org.Id, Code = "KHTC", Name = "Phòng Kế hoạch - Tài chính", TreePath = "/KHTC", Level = 1 };
            var deptKhoaQhcc = new Department { OrgId = org.Id, Code = "KQHCC", Name = "Khoa Quản trị và Hành chính công", TreePath = "/KQHCC", Level = 1 };

            context.Departments.AddRange(deptBgd, deptDaoTao, deptTccb, deptKhtc, deptKhoaQhcc);
            await context.SaveChangesAsync();

            // Chức danh vị trí việc làm
            var posDirector = new Position { Code = "GDHV", Name = "Giám đốc Học viện", RankLevel = "Lãnh đạo cấp cao", IsLeadership = true };
            var posManager = new Position { Code = "TP", Name = "Trưởng phòng Chuyên môn", RankLevel = "Lãnh đạo cấp phòng", IsLeadership = true };
            var posOfficer = new Position { Code = "CV", Name = "Chuyên viên Quản lý Đào tạo", RankLevel = "Chuyên viên", IsLeadership = false };
            var posHr = new Position { Code = "CV_TCCB", Name = "Chuyên viên Tổ chức Cán bộ", RankLevel = "Chuyên viên", IsLeadership = false };
            var posAccountant = new Position { Code = "KTV", Name = "Kế toán viên Thanh toán", RankLevel = "Chuyên viên", IsLeadership = false };
            var posAdmin = new Position { Code = "QTV", Name = "Chuyên viên Công nghệ thông tin", RankLevel = "Chuyên viên", IsLeadership = false };

            context.Positions.AddRange(posDirector, posManager, posOfficer, posHr, posAccountant, posAdmin);
            await context.SaveChangesAsync();

            // Danh mục Ngạch bậc công chức
            var rankCvc = new PositionRank { RankCode = "01.002", RankName = "Chuyên viên chính", RankGroup = "Công chức loại A2", MaxGrade = 8 };
            var rankCv = new PositionRank { RankCode = "01.003", RankName = "Chuyên viên", RankGroup = "Công chức loại A1", MaxGrade = 9 };
            var rankCsc = new PositionRank { RankCode = "01.001", RankName = "Chuyên viên cao cấp", RankGroup = "Công chức loại A3", MaxGrade = 6 };

            context.PositionRanks.AddRange(rankCsc, rankCvc, rankCv);
            await context.SaveChangesAsync();

            // 3. Người dùng mẫu (6 tài khoản chuẩn từ README)
            // Hash password cố định "123456" bằng BCrypt
            var salt = "bcrypt_salt_kpi";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

            var userDirector = new User { Username = "director", Email = "director@napa.edu.vn", PasswordHash = passwordHash, Salt = salt, Status = UserStatus.Active };
            var userManager = new User { Username = "manager", Email = "manager@napa.edu.vn", PasswordHash = passwordHash, Salt = salt, Status = UserStatus.Active };
            var userOfficer1 = new User { Username = "officer1", Email = "officer1@napa.edu.vn", PasswordHash = passwordHash, Salt = salt, Status = UserStatus.Active };
            var userHr = new User { Username = "hr_admin", Email = "hr_admin@napa.edu.vn", PasswordHash = passwordHash, Salt = salt, Status = UserStatus.Active };
            var userAccountant = new User { Username = "accountant", Email = "accountant@napa.edu.vn", PasswordHash = passwordHash, Salt = salt, Status = UserStatus.Active };
            var userAdmin = new User { Username = "admin", Email = "admin@napa.edu.vn", PasswordHash = passwordHash, Salt = salt, Status = UserStatus.Active };

            context.Users.AddRange(userDirector, userManager, userOfficer1, userHr, userAccountant, userAdmin);
            await context.SaveChangesAsync();

            // Gán Roles
            context.UserRoles.AddRange(
                new UserRole { UserId = userDirector.Id, RoleId = roleDirector.Id },
                new UserRole { UserId = userManager.Id, RoleId = roleManager.Id },
                new UserRole { UserId = userOfficer1.Id, RoleId = roleOfficer.Id },
                new UserRole { UserId = userHr.Id, RoleId = roleHr.Id },
                new UserRole { UserId = userAccountant.Id, RoleId = roleAccountant.Id },
                new UserRole { UserId = userAdmin.Id, RoleId = roleAdmin.Id }
            );
            await context.SaveChangesAsync();

            // Hồ sơ nhân viên (EmployeeProfile)
            var empDirector = new EmployeeProfile { UserId = userDirector.Id, DepartmentId = deptBgd.Id, PositionId = posDirector.Id, StaffCode = "CB001", FullName = "Trần Lãnh Đạo", Phone = "0912000001", JoinDate = new DateTime(2015, 1, 15) };
            var empManager = new EmployeeProfile { UserId = userManager.Id, DepartmentId = deptDaoTao.Id, PositionId = posManager.Id, StaffCode = "CB002", FullName = "Lê Trưởng Phòng", Phone = "0912000002", JoinDate = new DateTime(2018, 3, 1) };
            var empOfficer1 = new EmployeeProfile { UserId = userOfficer1.Id, DepartmentId = deptDaoTao.Id, PositionId = posOfficer.Id, StaffCode = "CB003", FullName = "Nguyễn Chuyên Viên", Phone = "0912000003", JoinDate = new DateTime(2021, 6, 1) };
            var empHr = new EmployeeProfile { UserId = userHr.Id, DepartmentId = deptTccb.Id, PositionId = posHr.Id, StaffCode = "CB004", FullName = "Vũ Thị Nhân Sự", Phone = "0912000004", JoinDate = new DateTime(2019, 9, 1) };
            var empAccountant = new EmployeeProfile { UserId = userAccountant.Id, DepartmentId = deptKhtc.Id, PositionId = posAccountant.Id, StaffCode = "CB005", FullName = "Hoàng Kế Toán", Phone = "0912000005", JoinDate = new DateTime(2020, 4, 15) };
            var empAdmin = new EmployeeProfile { UserId = userAdmin.Id, DepartmentId = deptBgd.Id, PositionId = posAdmin.Id, StaffCode = "CB000", FullName = "Quản trị viên Hệ thống", Phone = "0912000000", JoinDate = new DateTime(2022, 1, 1) };

            context.EmployeeProfiles.AddRange(empDirector, empManager, empOfficer1, empHr, empAccountant, empAdmin);
            await context.SaveChangesAsync();

            // Hồ sơ công chức (CivilServantProfile)
            var csOfficer1 = new CivilServantProfile
            {
                EmployeeId = empOfficer1.Id,
                CivilServantCode = "CC-DT-003",
                OfficialRankCode = "01.003",
                SalaryGrade = 3,
                SalaryCoefficient = 3.00m,
                AppointmentDate = new DateTime(2021, 6, 1),
                PartyStatus = PartyStatus.Official,
                PoliticalTheory = PoliticalTheory.Intermediate
            };
            var csManager = new CivilServantProfile
            {
                EmployeeId = empManager.Id,
                CivilServantCode = "CC-DT-002",
                OfficialRankCode = "01.002",
                SalaryGrade = 5,
                SalaryCoefficient = 5.42m,
                AppointmentDate = new DateTime(2018, 3, 1),
                PartyStatus = PartyStatus.Official,
                PoliticalTheory = PoliticalTheory.High
            };
            context.CivilServantProfiles.AddRange(csOfficer1, csManager);
            await context.SaveChangesAsync();

            // 4. Danh mục Master Catalogs
            var catProcess = new KpiCategory { Code = "CAT_PROCESS", Name = "Quy trình nội bộ & Nghiệp vụ đào tạo", Perspective = KpiPerspective.InternalProcess, TargetSector = TargetSector.All, DisplayOrder = 1 };
            var catEthics = new KpiCategory { Code = "CAT_ETHICS", Name = "Đạo đức công vụ & Kỷ cương hành chính", Perspective = KpiPerspective.PublicServiceEthics, TargetSector = TargetSector.PublicSector, DisplayOrder = 2 };
            var catCustomer = new KpiCategory { Code = "CAT_SERVICE", Name = "Phục vụ người học & Xã hội", Perspective = KpiPerspective.Customer, TargetSector = TargetSector.All, DisplayOrder = 3 };
            var catLearning = new KpiCategory { Code = "CAT_RESEARCH", Name = "Nghiên cứu khoa học & Phát triển năng lực", Perspective = KpiPerspective.LearningGrowth, TargetSector = TargetSector.All, DisplayOrder = 4 };

            context.KpiCategories.AddRange(catProcess, catEthics, catCustomer, catLearning);
            await context.SaveChangesAsync();

            // Đơn vị đo
            var uPercent = new MeasurementUnit { Code = "PERCENT", Name = "Phần trăm hoàn thành", Symbol = "%" };
            var uTask = new MeasurementUnit { Code = "TASK", Name = "Hồ sơ / Lớp học", Symbol = "hồ sơ" };
            var uHours = new MeasurementUnit { Code = "HOUR", Name = "Giờ làm việc / giảng dạy", Symbol = "giờ" };
            var uScore = new MeasurementUnit { Code = "POINT", Name = "Điểm đánh giá thang 100", Symbol = "điểm" };

            context.MeasurementUnits.AddRange(uPercent, uTask, uHours, uScore);
            await context.SaveChangesAsync();

            // Phương pháp đo
            var mSystem = new MeasurementMethod { Code = "SYS_TRACK", Name = "Thống kê tự động từ CSDL Đào tạo", DataSource = "Hệ thống Quản lý Đào tạo" };
            var mManual = new MeasurementMethod { Code = "SUPERVISOR", Name = "Thẩm định trực tiếp của Lãnh đạo phòng", DataSource = "Báo cáo công tác tuần/tháng" };

            context.MeasurementMethods.AddRange(mSystem, mManual);
            await context.SaveChangesAsync();

            // Ngân hàng chỉ số chuẩn
            var kpiItem1 = new KpiMasterItem { CategoryId = catProcess.Id, UnitId = uPercent.Id, MethodId = mSystem.Id, Code = "KPI-DT-01", Name = "Tỷ lệ tổ chức lớp bồi dưỡng công chức đúng tiến độ", Description = "Tỷ lệ các khóa bồi dưỡng được mở theo kế hoạch được duyệt", ScoringFormula = ScoringFormula.HigherIsBetter, IsActive = true };
            var kpiItem2 = new KpiMasterItem { CategoryId = catProcess.Id, UnitId = uTask.Id, MethodId = mSystem.Id, Code = "KPI-DT-02", Name = "Số lượng hồ sơ cấp chứng chỉ xử lý không có sai sót", Description = "Số chứng chỉ hoàn thành khóa học ban hành chính xác", ScoringFormula = ScoringFormula.HigherIsBetter, IsActive = true };
            var kpiItem3 = new KpiMasterItem { CategoryId = catEthics.Id, UnitId = uPercent.Id, MethodId = mManual.Id, Code = "KPI-CC-03", Name = "Tuân thủ quy chế văn hóa công vụ và giờ giấc hành chính", Description = "Đảm bảo 100% ngày công chuẩn, văn hóa giao tiếp công sở văn minh", ScoringFormula = ScoringFormula.HigherIsBetter, IsCivilServantOnly = true, IsActive = true };
            var kpiItem4 = new KpiMasterItem { CategoryId = catLearning.Id, UnitId = uTask.Id, MethodId = mManual.Id, Code = "KPI-NCKH-04", Name = "Tham gia đề tài NCKH hoặc viết bài hội thảo khoa học", Description = "Tối thiểu 1 bài báo/tham luận chuyên đề công tác hành chính", ScoringFormula = ScoringFormula.HigherIsBetter, IsActive = true };

            context.KpiMasterItems.AddRange(kpiItem1, kpiItem2, kpiItem3, kpiItem4);
            await context.SaveChangesAsync();

            // Thang điểm & Xếp loại Nghị định 90/2020
            var scale = new ScoreScale { Code = "SCALE100", Name = "Thang điểm chuẩn 100", MinScore = 0m, MaxScore = 100m, IsDefault = true };
            context.ScoreScales.Add(scale);
            await context.SaveChangesAsync();

            var levelXuatSac = new ClassificationLevel { ScaleId = scale.Id, Code = "XUAT_SAC", Name = "Hoàn thành xuất sắc nhiệm vụ", MinPercentage = 90.0m, MaxPercentage = 100.0m, RequiresInitiative = true, IsCappedAt20Percent = true };
            var levelTot = new ClassificationLevel { ScaleId = scale.Id, Code = "HOAN_THANH_TOT", Name = "Hoàn thành tốt nhiệm vụ", MinPercentage = 70.0m, MaxPercentage = 89.99m, RequiresInitiative = false, IsCappedAt20Percent = false };
            var levelHoanThanh = new ClassificationLevel { ScaleId = scale.Id, Code = "HOAN_THANH", Name = "Hoàn thành nhiệm vụ", MinPercentage = 50.0m, MaxPercentage = 69.99m, RequiresInitiative = false, IsCappedAt20Percent = false };
            var levelKhongHoanThanh = new ClassificationLevel { ScaleId = scale.Id, Code = "KHONG_HOAN_THANH", Name = "Không hoàn thành nhiệm vụ", MinPercentage = 0.0m, MaxPercentage = 49.99m, RequiresInitiative = false, IsCappedAt20Percent = false };

            context.ClassificationLevels.AddRange(levelXuatSac, levelTot, levelHoanThanh, levelKhongHoanThanh);
            await context.SaveChangesAsync();

            // Kỳ đánh giá
            var period2026 = new EvaluationPeriod
            {
                ScaleId = scale.Id,
                Code = "PERIOD-2026",
                Name = "Kỳ đánh giá kết quả thực hiện nhiệm vụ & KPI năm 2026",
                PeriodType = PeriodType.Annual,
                Year = 2026,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                SelfDeadline = new DateTime(2026, 11, 20),
                ManagerDeadline = new DateTime(2026, 11, 30),
                ClosingDate = new DateTime(2026, 12, 15),
                Status = PeriodStatus.Open
            };
            context.EvaluationPeriods.Add(period2026);
            await context.SaveChangesAsync();

            // 5. Nạp danh mục Quyền hạn (Permissions) & Phân quyền Role-Permission
            var allPerms = KPI.Application.Common.Security.AppPermissions.GetAllPermissions();
            var permEntities = new List<Permission>();
            foreach (var p in allPerms)
            {
                var permEntity = new Permission
                {
                    Code = p.Code,
                    Module = p.Module,
                    Action = p.Name,
                    Description = p.Description,
                    CreatedAt = DateTime.UtcNow
                };
                permEntities.Add(permEntity);
            }
            context.Permissions.AddRange(permEntities);
            await context.SaveChangesAsync();

            var permMap = permEntities.ToDictionary(p => p.Code, p => p.Id);

            // Gán quyền cho từng vai trò:
            // 5.1. ADMIN: TOÀN BỘ QUYỀN TRONG HỆ THỐNG
            foreach (var perm in permEntities)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = roleAdmin.Id, PermissionId = perm.Id });
            }

            // 5.2. DIRECTOR (Giám đốc): Do ADMIN phân quyền
            var directorPermCodes = new[]
            {
                "Dashboard.View", "Dashboard.Executive",
                "DirectorTarget.View", "DirectorTarget.Manage",
                "KpiPlanning.View", "KpiPlanning.Approve",
                "Evaluation.FinalApprove", "Evaluation.Dispute",
                "Decree90.View", "Decree90.FinalizeRank", "Decree90.ManageCap",
                "Payroll.View", "Payroll.ViewOwnPayslip",
                "Report.View", "Report.Export", "Attendance.View"
            };
            foreach (var code in directorPermCodes)
            {
                if (permMap.TryGetValue(code, out var pid))
                    context.RolePermissions.Add(new RolePermission { RoleId = roleDirector.Id, PermissionId = pid });
            }

            // 5.3. MANAGER (Trưởng phòng/Khoa): Do ADMIN phân quyền
            var managerPermCodes = new[]
            {
                "Dashboard.View",
                "DirectorTarget.View",
                "KpiPlanning.View", "KpiPlanning.Assign",
                "Evaluation.SelfAssess", "Evaluation.ManagerReview", "Evaluation.Dispute",
                "Decree90.View",
                "Attendance.View",
                "Payroll.ViewOwnPayslip",
                "Report.View"
            };
            foreach (var code in managerPermCodes)
            {
                if (permMap.TryGetValue(code, out var pid))
                    context.RolePermissions.Add(new RolePermission { RoleId = roleManager.Id, PermissionId = pid });
            }

            // 5.4. HR_ADMIN (Nhân sự - TCCB): Do ADMIN phân quyền
            var hrPermCodes = new[]
            {
                "Dashboard.View",
                "Attendance.View", "Attendance.Manage",
                "Decree90.View", "Decree90.ManageCap",
                "Payroll.ViewOwnPayslip",
                "Report.View", "Report.Export"
            };
            foreach (var code in hrPermCodes)
            {
                if (permMap.TryGetValue(code, out var pid))
                    context.RolePermissions.Add(new RolePermission { RoleId = roleHr.Id, PermissionId = pid });
            }

            // 5.5. ACCOUNTANT (Kế toán): Do ADMIN phân quyền
            var accPermCodes = new[]
            {
                "Dashboard.View",
                "Attendance.View",
                "Payroll.View", "Payroll.Calculate", "Payroll.PayoutCash", "Payroll.ViewOwnPayslip",
                "Report.View", "Report.Export"
            };
            foreach (var code in accPermCodes)
            {
                if (permMap.TryGetValue(code, out var pid))
                    context.RolePermissions.Add(new RolePermission { RoleId = roleAccountant.Id, PermissionId = pid });
            }

            // 5.6. OFFICER (Giảng viên / Chuyên viên): Do ADMIN phân quyền
            var officerPermCodes = new[]
            {
                "Dashboard.View",
                "DirectorTarget.View",
                "KpiPlanning.View",
                "Evaluation.SelfAssess", "Evaluation.Dispute",
                "Payroll.ViewOwnPayslip"
            };
            foreach (var code in officerPermCodes)
            {
                if (permMap.TryGetValue(code, out var pid))
                    context.RolePermissions.Add(new RolePermission { RoleId = roleOfficer.Id, PermissionId = pid });
            }
            await context.SaveChangesAsync();

            // 6. Nạp danh mục Menu động (MenuItems) do ADMIN toàn quyền quản lý
            var menuItems = GetDefaultMenuItems();
            context.MenuItems.AddRange(menuItems);
            await context.SaveChangesAsync();

            // 7. Nạp Mục tiêu Ban Giám đốc (Director Stretch Targets)
            var target1 = new KPI.Domain.Entities.Planning.DirectorTarget
            {
                PeriodId = period2026.Id,
                DepartmentId = null, // Toàn viện
                TargetName = "Định mức giảng dạy và bồi dưỡng chuẩn / kỳ",
                TargetValue = 320m,
                ChallengeValue = 350m,
                TargetUnit = "tiết",
                Description = "Định mức tối thiểu 320 tiết. Giảng viên đạt trên 350 tiết (vượt mục tiêu) mới đủ điều kiện xếp loại Hoàn thành tốt hoặc Xuất sắc.",
                CreatedBy = userDirector.Id
            };
            var target2 = new KPI.Domain.Entities.Planning.DirectorTarget
            {
                PeriodId = period2026.Id,
                DepartmentId = null,
                TargetName = "Bài báo / Đề tài Nghiên cứu Khoa học công bố",
                TargetValue = 2m,
                ChallengeValue = 3m,
                TargetUnit = "bài báo",
                Description = "Yêu cầu tối thiểu 2 bài báo chuyên ngành. Đạt từ 3 bài báo trở lên mới đủ điều kiện xét sáng kiến kinh nghiệm.",
                CreatedBy = userDirector.Id
            };
            var target3 = new KPI.Domain.Entities.Planning.DirectorTarget
            {
                PeriodId = period2026.Id,
                DepartmentId = deptDaoTao.Id,
                TargetName = "Tỷ lệ học viên đạt điểm chuẩn >= 5.0",
                TargetValue = 85m,
                ChallengeValue = 92m,
                TargetUnit = "%",
                Description = "Tỷ lệ đạt kết quả học tập khóa đào tạo bồi dưỡng công chức.",
                CreatedBy = userDirector.Id
            };
            context.DirectorTargets.AddRange(target1, target2, target3);
            await context.SaveChangesAsync();

            // 8. Nạp bản ghi Chấm công (AttendanceRecord) cho các nhân sự
            var allEmps = new[] { empDirector, empManager, empOfficer1, empHr, empAccountant, empAdmin };
            foreach (var emp in allEmps)
            {
                var att = new KPI.Domain.Entities.Evaluation.AttendanceRecord
                {
                    EmployeeId = emp.Id,
                    Year = 2026,
                    Month = 10,
                    StandardWorkDays = 22,
                    ActualWorkDays = 22,
                    PaidLeave = 0,
                    UnpaidLeave = 0,
                    SickLeave = 0,
                    LateCount = 0,
                    EarlyLeaveCount = 0,
                    UnauthorizedAbsence = 0,
                    MissedMeetings = 0,
                    DisciplineViolation = false,
                    Notes = "Đi làm đủ 22 ngày công chuẩn, chấp hành tốt kỷ luật công vụ"
                };
                att.CalculateScore();
                context.AttendanceRecords.Add(att);
            }
            await context.SaveChangesAsync();

            logger.LogInformation("Nạp Dữ liệu mẫu Hạt giống (Seed Data) thành công với 6 tài khoản, Menu động, Mục tiêu BGĐ và Chấm công!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xảy ra trong quá trình nạp dữ liệu Seed Data");
            throw;
        }
    }

    private static string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static async Task EnsureSchemaAndNewComponentsAsync(KpiDbContext context, ILogger logger)
    {
        // 1. DDL: Tạo các bảng mới nếu chưa tồn tại
        var ddl = @"
IF OBJECT_ID('dbo.menu_items', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.menu_items (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        ParentId BIGINT NULL,
        Title NVARCHAR(100) NOT NULL,
        Icon NVARCHAR(50) NULL,
        Url NVARCHAR(255) NULL,
        PermissionCode NVARCHAR(100) NULL,
        SortOrder INT DEFAULT 0 NOT NULL,
        IsActive BIT DEFAULT 1 NOT NULL,
        IsDeleted BIT DEFAULT 0 NOT NULL,
        SectionTitle NVARCHAR(100) NULL,
        CONSTRAINT FK_menu_items_parent FOREIGN KEY (ParentId) REFERENCES dbo.menu_items(Id)
    );
END

IF OBJECT_ID('dbo.director_targets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.director_targets (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        PeriodId BIGINT NOT NULL,
        DepartmentId BIGINT NULL,
        TargetName NVARCHAR(200) NOT NULL,
        TargetValue DECIMAL(18,2) NOT NULL,
        ChallengeValue DECIMAL(18,2) NOT NULL,
        TargetUnit NVARCHAR(50) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        CreatedBy BIGINT NOT NULL,
        IsActive BIT DEFAULT 1 NOT NULL,
        IsDeleted BIT DEFAULT 0 NOT NULL,
        CreatedAt DATETIME2 DEFAULT SYSDATETIME() NOT NULL,
        CONSTRAINT FK_director_targets_period FOREIGN KEY (PeriodId) REFERENCES dbo.evaluation_periods(Id),
        CONSTRAINT FK_director_targets_department FOREIGN KEY (DepartmentId) REFERENCES dbo.departments(Id) ON DELETE SET NULL,
        CONSTRAINT FK_director_targets_user FOREIGN KEY (CreatedBy) REFERENCES dbo.users(Id)
    );
END

IF OBJECT_ID('dbo.attendance_records', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.attendance_records (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId BIGINT NOT NULL,
        [Year] INT NOT NULL,
        [Month] INT NOT NULL,
        StandardWorkDays INT DEFAULT 22 NOT NULL,
        ActualWorkDays INT DEFAULT 22 NOT NULL,
        PaidLeave INT DEFAULT 0 NOT NULL,
        UnpaidLeave INT DEFAULT 0 NOT NULL,
        SickLeave INT DEFAULT 0 NOT NULL,
        LateCount INT DEFAULT 0 NOT NULL,
        EarlyLeaveCount INT DEFAULT 0 NOT NULL,
        UnauthorizedAbsence INT DEFAULT 0 NOT NULL,
        MissedMeetings INT DEFAULT 0 NOT NULL,
        DisciplineViolation BIT DEFAULT 0 NOT NULL,
        AttendanceScore DECIMAL(5,2) DEFAULT 20.00 NOT NULL,
        WorkDayRatio DECIMAL(5,4) DEFAULT 1.0000 NOT NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 DEFAULT SYSDATETIME() NOT NULL,
        UpdatedAt DATETIME2 NULL,
        IsDeleted BIT DEFAULT 0 NOT NULL,
        CONSTRAINT FK_attendance_records_employee FOREIGN KEY (EmployeeId) REFERENCES dbo.employee_profiles(Id)
    );
END
";
        try
        {
            await context.Database.ExecuteSqlRawAsync(ddl);
            logger.LogInformation("Đã kiểm tra và đồng bộ cấu trúc 4 bảng phân hệ mới thành công.");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Không thể chạy DDL tự động: {Message}", ex.Message);
        }

        // 2. Nạp Quyền hạn (Permissions) & RolePermissions nếu chưa có
        try
        {
            var existingPermCodes = (await context.Permissions.Select(p => p.Code).ToListAsync()).ToHashSet();
            var allPerms = KPI.Application.Common.Security.AppPermissions.GetAllPermissions();
            var newPerms = allPerms.Where(p => !existingPermCodes.Contains(p.Code)).Select(p => new Permission
            {
                Code = p.Code,
                Module = p.Module,
                Action = p.Name,
                Description = p.Description,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            if (newPerms.Any())
            {
                context.Permissions.AddRange(newPerms);
                await context.SaveChangesAsync();
                logger.LogInformation("Đã bổ sung {Count} quyền mới vào bảng permissions.", newPerms.Count);
            }

            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Code == "ADMIN");
            if (adminRole != null)
            {
                var allDbPermIds = await context.Permissions.Select(p => p.Id).ToListAsync();
                var currentAdminPermIds = (await context.RolePermissions
                    .Where(rp => rp.RoleId == adminRole.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync()).ToHashSet();

                var missingForAdmin = allDbPermIds.Where(id => !currentAdminPermIds.Contains(id)).ToList();
                if (missingForAdmin.Any())
                {
                    context.RolePermissions.AddRange(missingForAdmin.Select(pid => new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = pid
                    }));
                    await context.SaveChangesAsync();
                    logger.LogInformation("Đã cấp bổ sung {Count} quyền cho vai trò ADMIN.", missingForAdmin.Count);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Chưa đồng bộ permissions: {Message}", ex.Message);
        }

        // 3. Nạp MenuItems nếu chưa có
        try
        {
            if (!await context.MenuItems.AnyAsync())
            {
                var menuItems = GetDefaultMenuItems();
                context.MenuItems.AddRange(menuItems);
                await context.SaveChangesAsync();
                logger.LogInformation("Đã nạp {Count} đầu mục Menu động cho ADMIN.", menuItems.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Chưa nạp MenuItems: {Message}", ex.Message);
        }

        // 4. Nạp DirectorTargets nếu chưa có
        try
        {
            if (!await context.DirectorTargets.AnyAsync())
            {
                var period = await context.EvaluationPeriods.FirstOrDefaultAsync(p => p.Year == 2026) 
                             ?? await context.EvaluationPeriods.FirstOrDefaultAsync();
                var directorUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "director")
                                   ?? await context.Users.FirstOrDefaultAsync();
                var deptDaoTao = await context.Departments.FirstOrDefaultAsync(d => d.Code == "DT");

                if (period != null && directorUser != null)
                {
                    context.DirectorTargets.AddRange(
                        new KPI.Domain.Entities.Planning.DirectorTarget
                        {
                            PeriodId = period.Id,
                            DepartmentId = null,
                            TargetName = "Định mức giảng dạy và bồi dưỡng chuẩn / kỳ",
                            TargetValue = 320m,
                            ChallengeValue = 350m,
                            TargetUnit = "tiết",
                            Description = "Định mức tối thiểu 320 tiết. Giảng viên đạt trên 350 tiết (vượt mục tiêu) mới đủ điều kiện xếp loại Hoàn thành tốt hoặc Xuất sắc.",
                            CreatedBy = directorUser.Id
                        },
                        new KPI.Domain.Entities.Planning.DirectorTarget
                        {
                            PeriodId = period.Id,
                            DepartmentId = null,
                            TargetName = "Bài báo / Đề tài Nghiên cứu Khoa học công bố",
                            TargetValue = 2m,
                            ChallengeValue = 3m,
                            TargetUnit = "bài báo",
                            Description = "Yêu cầu tối thiểu 2 bài báo chuyên ngành. Đạt từ 3 bài báo trở lên mới đủ điều kiện xét sáng kiến kinh nghiệm.",
                            CreatedBy = directorUser.Id
                        },
                        new KPI.Domain.Entities.Planning.DirectorTarget
                        {
                            PeriodId = period.Id,
                            DepartmentId = deptDaoTao?.Id,
                            TargetName = "Tỷ lệ học viên đạt điểm chuẩn >= 5.0",
                            TargetValue = 85m,
                            ChallengeValue = 92m,
                            TargetUnit = "%",
                            Description = "Tỷ lệ đạt kết quả học tập khóa đào tạo bồi dưỡng công chức.",
                            CreatedBy = directorUser.Id
                        }
                    );
                    await context.SaveChangesAsync();
                    logger.LogInformation("Đã nạp Mục tiêu kéo giãn Ban Giám đốc mẫu.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Chưa nạp DirectorTargets: {Message}", ex.Message);
        }

        // 5. Nạp AttendanceRecords nếu chưa có
        try
        {
            if (!await context.AttendanceRecords.AnyAsync())
            {
                var emps = await context.EmployeeProfiles.ToListAsync();
                foreach (var emp in emps)
                {
                    var att = new KPI.Domain.Entities.Evaluation.AttendanceRecord
                    {
                        EmployeeId = emp.Id,
                        Year = 2026,
                        Month = 10,
                        StandardWorkDays = 22,
                        ActualWorkDays = 22,
                        PaidLeave = 0,
                        UnpaidLeave = 0,
                        SickLeave = 0,
                        LateCount = 0,
                        EarlyLeaveCount = 0,
                        UnauthorizedAbsence = 0,
                        MissedMeetings = 0,
                        DisciplineViolation = false,
                        Notes = "Đi làm đủ 22 ngày công chuẩn, chấp hành tốt kỷ luật công vụ"
                    };
                    att.CalculateScore();
                    context.AttendanceRecords.Add(att);
                }
                await context.SaveChangesAsync();
                logger.LogInformation("Đã nạp Bảng chấm công mẫu cho {Count} nhân sự.", emps.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Chưa nạp AttendanceRecords: {Message}", ex.Message);
        }
    }

    private static List<MenuItem> GetDefaultMenuItems()
    {
        return new List<MenuItem>
        {
            // Tổng quan
            new() { Title = "Bàn làm việc (Dashboard)", Icon = "bi-grid-fill", Url = "/Dashboard/Index", PermissionCode = "Dashboard.View", SortOrder = 1, SectionTitle = "Tổng quan" },

            // Kế hoạch & Giao KPI
            new() { Title = "Mục tiêu Ban Giám đốc", Icon = "bi-flag-fill", Url = "/KpiPlanning/DirectorTargets", PermissionCode = "DirectorTarget.View", SortOrder = 2, SectionTitle = "Kế hoạch & Giao KPI" },
            new() { Title = "KPI Của Tôi", Icon = "bi-person-workspace", Url = "/KpiPlanning/MyKpi", PermissionCode = "KpiPlanning.View", SortOrder = 3, SectionTitle = "Kế hoạch & Giao KPI" },
            new() { Title = "Giao Chỉ Tiêu (Wizard)", Icon = "bi-bullseye", Url = "/KpiPlanning/Assign", PermissionCode = "KpiPlanning.Assign", SortOrder = 4, SectionTitle = "Kế hoạch & Giao KPI" },
            new() { Title = "Phê Duyệt Kế Hoạch (BR-03)", Icon = "bi-patch-check-fill", Url = "/KpiPlanning/Approvals", PermissionCode = "KpiPlanning.Approve", SortOrder = 5, SectionTitle = "Kế hoạch & Giao KPI" },
            new() { Title = "Ngân hàng KPI Chuẩn", Icon = "bi-journal-bookmark-fill", Url = "/Catalog/KpiBank", PermissionCode = "System.ManageCatalogs", SortOrder = 6, SectionTitle = "Kế hoạch & Giao KPI" },
            new() { Title = "Quản trị Danh mục", Icon = "bi-collection-fill", Url = "/Catalog/Index", PermissionCode = "System.ManageCatalogs", SortOrder = 7, SectionTitle = "Kế hoạch & Giao KPI" },

            // Chấm công & Kỷ luật
            new() { Title = "Bảng Chấm Công (20% KPI)", Icon = "bi-calendar-check-fill", Url = "/Attendance/Index", PermissionCode = "Attendance.View", SortOrder = 8, SectionTitle = "Chấm công & Kỷ luật" },

            // Đánh giá & Thẩm định
            new() { Title = "Tự đánh giá (Self-Assess)", Icon = "bi-pencil-square", Url = "/Evaluation/SelfAssess", PermissionCode = "Evaluation.SelfAssess", SortOrder = 9, SectionTitle = "Đánh giá & Thẩm định" },
            new() { Title = "Quản lý Thẩm định điểm", Icon = "bi-clipboard-check-fill", Url = "/Evaluation/ManagerList", PermissionCode = "Evaluation.ManagerReview", SortOrder = 10, SectionTitle = "Đánh giá & Thẩm định" },
            new() { Title = "Lãnh đạo Phê duyệt", Icon = "bi-award-fill", Url = "/Evaluation/DirectorList", PermissionCode = "Evaluation.FinalApprove", SortOrder = 11, SectionTitle = "Đánh giá & Thẩm định" },
            new() { Title = "Khiếu nại Đánh giá (BR-14)", Icon = "bi-shield-shaded", Url = "/Evaluation/Disputes", PermissionCode = "Evaluation.Dispute", SortOrder = 12, SectionTitle = "Đánh giá & Thẩm định" },

            // Nghị định 90 & 48
            new() { Title = "Xếp loại CBCC (NĐ 90)", Icon = "bi-patch-check-fill", Url = "/CivilServant/Index", PermissionCode = "Decree90.View", SortOrder = 13, SectionTitle = "Nghị định 90 & 48" },
            new() { Title = "Kiểm soát Trần 20% & Gauss", Icon = "bi-pie-chart-fill", Url = "/CivilServant/Dashboard", PermissionCode = "Decree90.ManageCap", SortOrder = 14, SectionTitle = "Nghị định 90 & 48" },

            // Lương 3P & Chi trả
            new() { Title = "Tính lương KPI (P3)", Icon = "bi-cash-stack", Url = "/Payroll/Index", PermissionCode = "Payroll.View", SortOrder = 15, SectionTitle = "Lương 3P & Chi trả" },
            new() { Title = "Ký nhận Tiền mặt (05a)", Icon = "bi-pen-fill", Url = "/Payroll/CashSheet", PermissionCode = "Payroll.PayoutCash", SortOrder = 16, SectionTitle = "Lương 3P & Chi trả" },
            new() { Title = "Phiếu lương Cá nhân", Icon = "bi-file-earmark-person-fill", Url = "/Payroll/MyPayslip", PermissionCode = "Payroll.ViewOwnPayslip", SortOrder = 17, SectionTitle = "Lương 3P & Chi trả" },
            new() { Title = "Báo cáo Quỹ lương", Icon = "bi-bar-chart-line-fill", Url = "/Payroll/Reports", PermissionCode = "Payroll.View", SortOrder = 18, SectionTitle = "Lương 3P & Chi trả" },

            // Báo cáo & Phân tích
            new() { Title = "Báo cáo & Chuông Gauss", Icon = "bi-graph-up-arrow", Url = "/Report/Index", PermissionCode = "Report.View", SortOrder = 19, SectionTitle = "Báo cáo & Phân tích" },
            new() { Title = "Bảng Xếp Hạng", Icon = "bi-trophy-fill", Url = "/Report/Leaderboard", PermissionCode = "Report.View", SortOrder = 20, SectionTitle = "Báo cáo & Phân tích" },

            // Quản trị Hệ thống (CHỈ DÀNH CHO ADMIN)
            new() { Title = "Quản lý Người dùng", Icon = "bi-people-fill", Url = "/Admin/Users", PermissionCode = "Admin.ManageUsers", SortOrder = 21, SectionTitle = "Quản trị Hệ thống (ADMIN)" },
            new() { Title = "Quản lý Menu Đầu mục", Icon = "bi-menu-button-wide-fill", Url = "/Admin/Menus", PermissionCode = "Admin.ManageMenus", SortOrder = 22, SectionTitle = "Quản trị Hệ thống (ADMIN)" },
            new() { Title = "Phân quyền Hệ thống", Icon = "bi-shield-lock-fill", Url = "/Admin/Permissions", PermissionCode = "Admin.ManagePermissions", SortOrder = 23, SectionTitle = "Quản trị Hệ thống (ADMIN)" },
            new() { Title = "Nhật ký Kiểm toán", Icon = "bi-clock-history", Url = "/Admin/AuditLog", PermissionCode = "Admin.ViewAuditLogs", SortOrder = 24, SectionTitle = "Quản trị Hệ thống (ADMIN)" }
        };
    }
}
