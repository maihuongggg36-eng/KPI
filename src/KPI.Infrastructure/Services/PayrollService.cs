using System.Security.Cryptography;
using System.Text;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Evaluation;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Payroll;
using KPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KPI.Infrastructure.Services;

/// <summary>
/// Thực thi dịch vụ Quản lý Lương 3P (P1-P2-P3) & Chi trả Tiền mặt
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly KpiDbContext _context;

    // Bảng định mức Lương cơ bản và Phụ cấp mặc định theo vị trí
    private static readonly Dictionary<string, (decimal BaseSalary, decimal Allowance)> DefaultPositionSalaries = new(StringComparer.OrdinalIgnoreCase)
    {
        { "GIAM_DOC", (18000000m, 3500000m) },
        { "PHO_GIAM_DOC", (15000000m, 2800000m) },
        { "TRUONG_PHONG", (13500000m, 2200000m) },
        { "PHO_PHONG", (11500000m, 1800000m) },
        { "KE_TOAN", (10500000m, 1500000m) },
        { "CHUYEN_VIEN_CC", (12000000m, 1600000m) },
        { "CHUYEN_VIEN_CHINH", (10000000m, 1400000m) },
        { "CHUYEN_VIEN", (8500000m, 1000000m) },
        { "CAN_BO", (8000000m, 900000m) }
    };

    public PayrollService(KpiDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách bảng tính lương kèm bộ lọc
    /// </summary>
    public async Task<List<PayrollItemDto>> GetPayrollListAsync(long periodId, long? departmentId = null, string? paymentStatus = null)
    {
        var query = _context.Payrolls
            .Include(p => p.Period)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Position)
            .Include(p => p.Cashier)
            .Where(p => p.PeriodId == periodId)
            .AsQueryable();

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(p => p.Employee.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            query = query.Where(p => p.PaymentStatus == paymentStatus);
        }

        var list = await query
            .OrderBy(p => p.Employee.DepartmentId)
            .ThenByDescending(p => p.NetSalary)
            .ToListAsync();

        return list.Select(p => new PayrollItemDto
        {
            Id = p.Id,
            PeriodId = p.PeriodId,
            PeriodName = p.Period?.Name ?? "Kỳ đánh giá",
            PeriodYear = p.Period?.Year ?? DateTime.Now.Year,
            EmployeeId = p.EmployeeId,
            StaffCode = p.Employee?.StaffCode ?? $"CB-{p.EmployeeId:D4}",
            FullName = p.Employee?.FullName ?? "Cán bộ",
            AvatarUrl = "/images/default-avatar.png",
            DepartmentId = p.Employee?.DepartmentId ?? 0,
            DepartmentName = p.Employee?.Department?.Name ?? "Phòng ban",
            PositionName = p.Employee?.Position?.Name ?? "Chuyên viên",
            BaseSalary = p.BaseSalary,
            SalaryCoefficient = p.SalaryCoefficient,
            BaseSalaryUnit = p.BaseSalaryUnit,
            StandardWorkDays = p.StandardWorkDays,
            ActualWorkDays = p.ActualWorkDays,
            WorkDayRatio = p.WorkDayRatio,
            AttendanceScore = p.AttendanceScore,
            PositionAllowance = p.PositionAllowance,
            SeniorityAllowance = p.SeniorityAllowance,
            ProfessionAllowance = p.ProfessionAllowance,
            OtherAllowance = p.OtherAllowance,
            Allowances = p.Allowances,
            KpiScore = p.KpiScore,
            KpiClassification = p.KpiClassification,
            KpiMultiplier = p.KpiMultiplier,
            P3Salary = p.P3Salary,
            BonusAmount = p.BonusAmount,
            BhxhDeduction = p.BhxhDeduction,
            BhytDeduction = p.BhytDeduction,
            BhtnDeduction = p.BhtnDeduction,
            UnionFee = p.UnionFee,
            Deductions = p.Deductions,
            PersonalIncomeTax = p.PersonalIncomeTax,
            GrossIncome = p.GrossIncome,
            NetSalary = p.NetSalary,
            VoucherCode = p.VoucherCode,
            PaymentMethod = p.PaymentMethod,
            PaymentStatus = p.PaymentStatus,
            PaidAt = p.PaidAt,
            CashierName = p.Cashier?.Username,
            RecipientSignature = p.RecipientSignature,
            Notes = p.Notes,
            VerificationHash = p.VerificationHash
        }).ToList();
    }

    /// <summary>
    /// Thống kê tổng hợp Quỹ lương & Giải ngân tiền mặt
    /// </summary>
    public async Task<PayrollStatsDto> GetPayrollStatsAsync(long periodId)
    {
        var records = await _context.Payrolls
            .Where(p => p.PeriodId == periodId)
            .ToListAsync();

        if (records.Count == 0)
        {
            return new PayrollStatsDto();
        }

        return new PayrollStatsDto
        {
            TotalEmployees = records.Count,
            TotalBaseSalary = records.Sum(r => r.BaseSalary),
            TotalAllowances = records.Sum(r => r.Allowances),
            TotalP3Salary = records.Sum(r => r.P3Salary),
            TotalBonus = records.Sum(r => r.BonusAmount),
            TotalDeductions = records.Sum(r => r.Deductions + r.PersonalIncomeTax),
            TotalNetSalary = records.Sum(r => r.NetSalary),
            TotalCashPaid = records.Where(r => r.PaymentStatus == "Đã thanh toán").Sum(r => r.NetSalary),
            TotalCashPending = records.Where(r => r.PaymentStatus != "Đã thanh toán").Sum(r => r.NetSalary),
            CountPaid = records.Count(r => r.PaymentStatus == "Đã thanh toán"),
            CountPending = records.Count(r => r.PaymentStatus != "Đã thanh toán")
        };
    }

    /// <summary>
    /// Tính toán bảng lương toàn bộ nhân sự theo kết quả đánh giá KPI trong kỳ
    /// Công thức: Lương_P3 = LươngCơBản * HệSốLương * HệSốKPI
    /// </summary>
    public async Task<int> CalculateAllPayrollAsync(long periodId, long currentUserId)
    {
        var period = await _context.EvaluationPeriods.FindAsync(periodId);
        if (period == null)
        {
            throw new BusinessRuleException("BR-PAY-404", "Không tìm thấy kỳ đánh giá được yêu cầu.");
        }

        var employees = await _context.EmployeeProfiles
            .Include(e => e.Position)
            .Include(e => e.Department)
            .Include(e => e.CivilServantProfile)
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        var evaluations = await _context.Evaluations
            .Include(e => e.CivilServantClassification)
            .Where(e => e.PeriodId == periodId)
            .ToListAsync();

        var evalMap = evaluations.ToDictionary(e => e.EmployeeId);

        var existingPayrolls = await _context.Payrolls
            .Where(p => p.PeriodId == periodId)
            .ToListAsync();

        var payrollMap = existingPayrolls.ToDictionary(p => p.EmployeeId);

        // Lấy danh sách chấm công trong kỳ
        var attendanceList = await _context.AttendanceRecords
            .Where(a => a.Year == period.Year)
            .ToListAsync();
        var attendanceMap = attendanceList.GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Month).First());

        // Lấy mục tiêu Ban Giám đốc đặt ra
        var directorTargets = await _context.DirectorTargets
            .Where(t => t.PeriodId == periodId && t.IsActive)
            .ToListAsync();

        int processedCount = 0;

        foreach (var emp in employees)
        {
            evalMap.TryGetValue(emp.Id, out var eval);
            attendanceMap.TryGetValue(emp.Id, out var attendance);

            // 1. Chấm công
            int standardWorkDays = attendance?.StandardWorkDays ?? 22;
            int actualWorkDays = attendance?.ActualWorkDays ?? 22;
            decimal attendanceScore = attendance?.AttendanceScore ?? 20.0m;
            decimal workDayRatio = standardWorkDays > 0 ? Math.Clamp((decimal)actualWorkDays / standardWorkDays, 0m, 1.5m) : 1.0m;

            // 2. Hệ số lương theo Ngạch bậc (Nghị định 204/2004)
            decimal salaryCoefficient = emp.CivilServantProfile?.SalaryCoefficient ?? 2.34m;
            if (salaryCoefficient <= 0) salaryCoefficient = 2.34m;

            // 3. Phụ cấp chuẩn Học viện
            decimal positionAllowance = 0m;
            if (emp.Position != null && emp.Position.IsLeadership)
            {
                positionAllowance = Math.Round(2340000m * 0.5m, 0); // Hệ số chức vụ lãnh đạo
            }

            decimal seniorityAllowance = 0m;
            var yearsOfService = DateTime.UtcNow.Year - emp.JoinDate.Year;
            if (yearsOfService >= 5)
            {
                seniorityAllowance = Math.Round(2340000m * salaryCoefficient * 0.05m, 0); // 5% thâm niên
            }

            decimal baseSalaryRaw = 2340000m * salaryCoefficient * workDayRatio;
            decimal professionAllowance = Math.Round((baseSalaryRaw + positionAllowance) * 0.30m, 0); // 30% ưu đãi nhà giáo
            decimal otherAllowance = 500000m; // Phụ cấp công việc

            // 4. Kết quả KPI & Vượt mục tiêu BGĐ
            decimal kpiScore = 80.0m;
            string classification = "Hoàn thành tốt nhiệm vụ";

            if (eval != null)
            {
                // KPI = 70% Chuyên môn + 20% Chấm công + 10% Đánh giá đồng nghiệp
                decimal expertScore = eval.FinalScore > 0 ? eval.FinalScore : (eval.ManagerTotalScore > 0 ? eval.ManagerTotalScore : eval.SelfTotalScore);
                decimal peerScore = (eval.PeerReviews != null && eval.PeerReviews.Any()) ? eval.PeerReviews.Average(pr => pr.BehaviorScore) : 9.0m;
                kpiScore = Math.Round((expertScore * 0.70m) + (attendanceScore) + peerScore, 1);

                // Kiểm tra điều kiện VƯỢT MỤC TIÊU BAN GIÁM ĐỐC
                bool hasExceededDirectorTarget = expertScore > 100.0m || expertScore >= 85.0m; // Đã vượt chỉ tiêu

                if (kpiScore >= 90.0m && hasExceededDirectorTarget)
                {
                    classification = "Hoàn thành xuất sắc nhiệm vụ";
                }
                else if (kpiScore >= 70.0m && hasExceededDirectorTarget)
                {
                    classification = "Hoàn thành tốt nhiệm vụ";
                }
                else if (kpiScore >= 50.0m)
                {
                    classification = "Hoàn thành nhiệm vụ";
                }
                else
                {
                    classification = "Không hoàn thành nhiệm vụ";
                }
            }
            else
            {
                kpiScore = Math.Round(56.0m + attendanceScore + 9.0m, 1);
                classification = "Hoàn thành tốt nhiệm vụ";
            }

            decimal kpiMultiplier = CalculateKpiMultiplier(classification, kpiScore);

            // Sinh mã phiếu chi: KPI-PC-YYYY-NNNN
            string voucherCode = $"KPI-PC-{period.Year}-{emp.Id:D4}";

            if (!payrollMap.TryGetValue(emp.Id, out var record))
            {
                record = new PayrollRecord
                {
                    PeriodId = periodId,
                    EmployeeId = emp.Id,
                    EvaluationId = eval?.Id,
                    VoucherCode = voucherCode,
                    PaymentMethod = "Tiền mặt",
                    PaymentStatus = "Chưa thanh toán",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };
                _context.Payrolls.Add(record);
            }

            record.BaseSalaryUnit = 2340000m;
            record.SalaryCoefficient = salaryCoefficient;
            record.StandardWorkDays = standardWorkDays;
            record.ActualWorkDays = actualWorkDays;
            record.AttendanceScore = attendanceScore;
            record.PositionAllowance = positionAllowance;
            record.SeniorityAllowance = seniorityAllowance;
            record.ProfessionAllowance = professionAllowance;
            record.OtherAllowance = otherAllowance;
            record.KpiScore = kpiScore;
            record.KpiClassification = classification;
            record.KpiMultiplier = kpiMultiplier;
            record.BonusAmount = classification == "Hoàn thành xuất sắc nhiệm vụ" ? 1000000m : 0m;
            record.Notes = $"Lương viên chức theo NĐ 204 + NĐ 73 + KPI ({classification})";
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = currentUserId;

            // Thực thi tính lương tự động
            record.CalculateSalary();
            record.VerificationHash = GenerateVerificationHash(voucherCode, record.NetSalary, emp.StaffCode);

            processedCount++;
        }

        // Ghi Audit Log
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentUserId,
            Action = "CALCULATE_PAYROLL",
            EntityName = "payrolls",
            EntityId = periodId,
            NewValues = $"Đã tính toán lương 3P (P1-P2-P3) cho {processedCount} cán bộ, nhân viên kỳ {period.Name}.",
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1"
        });

        await _context.SaveChangesAsync();
        return processedCount;
    }

    /// <summary>
    /// Kế toán điều chỉnh một dòng phiếu lương
    /// </summary>
    public async Task<bool> UpdatePayrollItemAsync(UpdatePayrollItemRequest request, long currentUserId)
    {
        var record = await _context.Payrolls.FindAsync(request.PayrollId);
        if (record == null) return false;

        record.BaseSalary = request.BaseSalary;
        record.Allowances = request.Allowances;
        record.BonusAmount = request.BonusAmount;
        record.Deductions = request.Deductions;
        record.Notes = request.Notes;

        // Tính lại P3, Gross, Net
        record.P3Salary = Math.Round(record.BaseSalary * record.SalaryCoefficient * record.KpiMultiplier);
        record.GrossIncome = record.BaseSalary + record.Allowances + record.P3Salary + record.BonusAmount;
        record.NetSalary = Math.Max(0, record.GrossIncome - record.Deductions - record.PersonalIncomeTax);
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = currentUserId;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentUserId,
            Action = "UPDATE_PAYROLL",
            EntityName = "payrolls",
            EntityId = record.Id,
            NewValues = $"Cập nhật phiếu lương: Net={record.NetSalary:N0} VNĐ",
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Xác nhận ký nhận chi tiền mặt cho 1 nhân viên
    /// </summary>
    public async Task<bool> MarkCashPaidAsync(long payrollId, string signature, long currentUserId)
    {
        var record = await _context.Payrolls.FindAsync(payrollId);
        if (record == null) return false;

        record.PaymentStatus = "Đã thanh toán";
        record.PaidAt = DateTime.UtcNow;
        record.CashierId = currentUserId;
        record.RecipientSignature = string.IsNullOrWhiteSpace(signature) 
            ? "Đã nhận đủ tiền mặt tại quỹ Học viện" 
            : signature;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = currentUserId;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentUserId,
            Action = "PAY_CASH",
            EntityName = "payrolls",
            EntityId = record.Id,
            NewValues = $"Đã chi tiền mặt {record.NetSalary:N0} VNĐ cho nhân viên {record.EmployeeId}",
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Đánh dấu tất cả nhân sự đã nhận tiền mặt tại quỹ
    /// </summary>
    public async Task<int> MarkAllCashPaidAsync(long periodId, long currentUserId)
    {
        var pendingRecords = await _context.Payrolls
            .Where(p => p.PeriodId == periodId && p.PaymentStatus != "Đã thanh toán")
            .ToListAsync();

        if (pendingRecords.Count == 0) return 0;

        foreach (var r in pendingRecords)
        {
            r.PaymentStatus = "Đã thanh toán";
            r.PaidAt = DateTime.UtcNow;
            r.CashierId = currentUserId;
            r.RecipientSignature = "Đã nhận đủ tiền mặt tại quỹ cơ quan";
            r.UpdatedAt = DateTime.UtcNow;
            r.UpdatedBy = currentUserId;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = currentUserId,
            Action = "BATCH_PAY_CASH",
            EntityName = "payrolls",
            EntityId = periodId,
            NewValues = $"Đã xác nhận giải ngân tiền mặt đồng loạt cho {pendingRecords.Count} cán bộ.",
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1"
        });

        await _context.SaveChangesAsync();
        return pendingRecords.Count;
    }

    /// <summary>
    /// Lấy thông tin Phiếu Chi tiền mặt Mẫu 02-TT
    /// </summary>
    public async Task<CashVoucherDto?> GetCashVoucherAsync(long payrollId)
    {
        var record = await _context.Payrolls
            .Include(p => p.Period)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Position)
            .FirstOrDefaultAsync(p => p.Id == payrollId);

        if (record == null) return null;

        var now = record.PaidAt ?? DateTime.Now;

        return new CashVoucherDto
        {
            PayrollId = record.Id,
            VoucherCode = record.VoucherCode,
            DateFormatted = $"Ngày {now.Day:D2} tháng {now.Month:D2} năm {now.Year}",
            Day = now.Day,
            Month = now.Month,
            Year = now.Year,
            OrganizationName = "HỌC VIỆN HÀNH CHÍNH QUỐC GIA",
            DepartmentName = record.Employee?.Department?.Name ?? "Phòng Quản lý Đào tạo",
            FullName = record.Employee?.FullName ?? "Cán bộ công chức",
            StaffCode = record.Employee?.StaffCode ?? $"CB-{record.EmployeeId:D4}",
            PositionName = record.Employee?.Position?.Name ?? "Chuyên viên",
            Address = "Học viện Hành chính Quốc gia, Hà Nội",
            Reason = $"Chi trả lương và thu nhập hiệu quả công tác (P3) gắn với kết quả KPI kỳ {record.Period?.Name}",
            Amount = record.NetSalary,
            AmountInWords = ConvertNumberToVietnameseWords(record.NetSalary),
            DebitAccount = "TK 3341, TK 642",
            CreditAccount = "TK 1111 (Tiền mặt)",
            Attachments = "01 Bảng đánh giá KPI và Bảng thanh toán lương Mẫu 05a",
            DirectorName = "PGS.TS Nguyễn Văn Giám Đốc",
            ChiefAccountantName = "ThS. Trần Thị Kế Toán Trưởng",
            CashierName = "Lê Thị Thủ Quỹ",
            PreparerName = "Phạm Văn Lập Phiếu"
        };
    }

    /// <summary>
    /// Lấy dữ liệu Bảng Ký Nhận Tiền Mặt Mẫu 05a
    /// </summary>
    public async Task<CashSheetDto> GetCashSheetAsync(long periodId, long? departmentId = null)
    {
        var period = await _context.EvaluationPeriods.FindAsync(periodId);
        var query = _context.Payrolls
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Position)
            .Where(p => p.PeriodId == periodId)
            .AsQueryable();

        string deptName = "Toàn cơ quan Học viện";
        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(p => p.Employee.DepartmentId == departmentId.Value);
            var dept = await _context.Departments.FindAsync(departmentId.Value);
            if (dept != null) deptName = dept.Name;
        }

        var list = await query
            .OrderBy(p => p.Employee.DepartmentId)
            .ThenBy(p => p.Employee.FullName)
            .ToListAsync();

        var items = new List<CashSheetItemDto>();
        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            items.Add(new CashSheetItemDto
            {
                Index = i + 1,
                PayrollId = p.Id,
                EmployeeId = p.EmployeeId,
                StaffCode = p.Employee?.StaffCode ?? $"CB-{p.EmployeeId:D4}",
                FullName = p.Employee?.FullName ?? "",
                PositionName = p.Employee?.Position?.Name ?? "",
                DepartmentName = p.Employee?.Department?.Name ?? "",
                BaseSalary = p.BaseSalary,
                P3Salary = p.P3Salary,
                Allowances = p.Allowances,
                NetSalary = p.NetSalary,
                IsPaid = p.PaymentStatus == "Đã thanh toán",
                PaidAt = p.PaidAt,
                RecipientSignature = p.RecipientSignature
            });
        }

        decimal totalAmount = items.Sum(i => i.NetSalary);

        return new CashSheetDto
        {
            PeriodId = periodId,
            PeriodName = period?.Name ?? "Kỳ đánh giá",
            Year = period?.Year ?? DateTime.Now.Year,
            DepartmentId = departmentId,
            DepartmentName = deptName,
            Items = items,
            TotalAmount = totalAmount,
            TotalAmountInWords = ConvertNumberToVietnameseWords(totalAmount)
        };
    }

    /// <summary>
    /// Lấy Phiếu lương chi tiết (Payslip) cá nhân cho nhân viên
    /// </summary>
    public async Task<PayslipDto?> GetMyPayslipAsync(long periodId, long employeeId)
    {
        var record = await _context.Payrolls
            .Include(p => p.Period)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Position)
            .Include(p => p.Employee)
                .ThenInclude(e => e.CivilServantProfile)
                    .ThenInclude(cs => cs!.OfficialRank)
            .FirstOrDefaultAsync(p => p.PeriodId == periodId && (p.EmployeeId == employeeId || (p.Employee != null && p.Employee.UserId == employeeId)));

        if (record == null) return null;

        var emp = record.Employee;
        decimal insurable = emp.CivilServantProfile != null ? (2340000m * record.SalaryCoefficient) : (record.BaseSalary * 0.7m);
        decimal social = Math.Round(insurable * 0.08m);
        decimal health = Math.Round(insurable * 0.015m);
        decimal unemp = Math.Round(insurable * 0.01m);

        return new PayslipDto
        {
            PayrollId = record.Id,
            PeriodId = record.PeriodId,
            PeriodName = record.Period?.Name ?? "Kỳ đánh giá",
            Year = record.Period?.Year ?? DateTime.Now.Year,
            EmployeeId = record.EmployeeId,
            StaffCode = emp?.StaffCode ?? $"CB-{emp?.Id:D4}",
            FullName = emp?.FullName ?? "",
            PositionName = emp?.Position?.Name ?? "Chuyên viên",
            DepartmentName = emp?.Department?.Name ?? "Phòng ban",
            RankName = emp?.CivilServantProfile?.OfficialRank?.RankName ?? "Chuyên viên",
            BaseSalaryUnit = record.BaseSalaryUnit,
            BaseSalary = record.BaseSalary,
            SalaryCoefficient = record.SalaryCoefficient,
            StandardWorkDays = record.StandardWorkDays,
            ActualWorkDays = record.ActualWorkDays,
            AttendanceScore = record.AttendanceScore,
            PositionAllowance = record.PositionAllowance,
            SeniorityAllowance = record.SeniorityAllowance,
            ProfessionAllowance = record.ProfessionAllowance,
            OtherAllowance = record.OtherAllowance,
            Allowances = record.Allowances,
            KpiScore = record.KpiScore,
            KpiClassification = record.KpiClassification,
            KpiMultiplier = record.KpiMultiplier,
            P3Salary = record.P3Salary,
            BonusAmount = record.BonusAmount,
            SocialInsurance = record.BhxhDeduction > 0 ? record.BhxhDeduction : social,
            HealthInsurance = record.BhytDeduction > 0 ? record.BhytDeduction : health,
            UnemploymentInsurance = record.BhtnDeduction > 0 ? record.BhtnDeduction : unemp,
            UnionFee = record.UnionFee,
            TotalDeductions = record.Deductions,
            PersonalIncomeTax = record.PersonalIncomeTax,
            GrossIncome = record.GrossIncome,
            NetSalary = record.NetSalary,
            AmountInWords = ConvertNumberToVietnameseWords(record.NetSalary),
            VoucherCode = record.VoucherCode,
            PaymentStatus = record.PaymentStatus,
            PaidAt = record.PaidAt,
            QrCodeContent = $"HỌC VIỆN HÀNH CHÍNH|PHIẾU LƯƠNG|{record.VoucherCode}|{emp?.FullName}|{record.NetSalary:N0} VNĐ|XÁC THỰC:{record.VerificationHash?.Substring(0, 8)}",
            VerificationHash = record.VerificationHash ?? ""
        };
    }

    /// <summary>
    /// Lấy dữ liệu Báo cáo & Phân tích cơ cấu Lương theo phòng ban và theo band
    /// </summary>
    public async Task<PayrollReportDto> GetPayrollReportAsync(long periodId)
    {
        var period = await _context.EvaluationPeriods.FindAsync(periodId);
        var records = await _context.Payrolls
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Where(p => p.PeriodId == periodId)
            .ToListAsync();

        var stats = await GetPayrollStatsAsync(periodId);

        // Biểu đồ: Chi lương theo phòng ban (Stacked Bar: P1, P2, P3)
        var deptGroups = records
            .GroupBy(r => r.Employee?.Department?.Name ?? "Khác")
            .Select(g => new DepartmentPayrollChartDto
            {
                DepartmentName = g.Key,
                TotalP1 = g.Sum(r => r.BaseSalary),
                TotalP2 = g.Sum(r => r.Allowances),
                TotalP3 = g.Sum(r => r.P3Salary),
                TotalNet = g.Sum(r => r.NetSalary)
            })
            .OrderByDescending(d => d.TotalNet)
            .ToList();

        // Biểu đồ: Phân bổ lương theo band (Histogram)
        var bands = new List<SalaryBandDto>
        {
            new() { BandLabel = "< 10 Triệu", EmployeeCount = records.Count(r => r.NetSalary < 10000000m), TotalAmount = records.Where(r => r.NetSalary < 10000000m).Sum(r => r.NetSalary) },
            new() { BandLabel = "10 - 15 Triệu", EmployeeCount = records.Count(r => r.NetSalary >= 10000000m && r.NetSalary < 15000000m), TotalAmount = records.Where(r => r.NetSalary >= 10000000m && r.NetSalary < 15000000m).Sum(r => r.NetSalary) },
            new() { BandLabel = "15 - 20 Triệu", EmployeeCount = records.Count(r => r.NetSalary >= 15000000m && r.NetSalary < 20000000m), TotalAmount = records.Where(r => r.NetSalary >= 15000000m && r.NetSalary < 20000000m).Sum(r => r.NetSalary) },
            new() { BandLabel = "20 - 25 Triệu", EmployeeCount = records.Count(r => r.NetSalary >= 20000000m && r.NetSalary < 25000000m), TotalAmount = records.Where(r => r.NetSalary >= 20000000m && r.NetSalary < 25000000m).Sum(r => r.NetSalary) },
            new() { BandLabel = "> 25 Triệu", EmployeeCount = records.Count(r => r.NetSalary >= 25000000m), TotalAmount = records.Where(r => r.NetSalary >= 25000000m).Sum(r => r.NetSalary) }
        };

        return new PayrollReportDto
        {
            PeriodId = periodId,
            PeriodName = period?.Name ?? "Kỳ đánh giá",
            Stats = stats,
            DepartmentCharts = deptGroups,
            SalaryBands = bands
        };
    }

    /// <summary>
    /// Mapping hệ số KPI (Xuất sắc: 1.3, Tốt: 1.15, Đạt: 1.0, Không đạt: 0.7)
    /// </summary>
    public decimal CalculateKpiMultiplier(string? classification, decimal kpiScore)
    {
        var text = (classification ?? "").ToLowerInvariant();

        // 1. Hoàn thành xuất sắc nhiệm vụ (hoặc >= 90 điểm) -> 1.3
        if (text.Contains("xuất sắc") || text.Contains("xuat sac") || kpiScore >= 90.0m)
        {
            return 1.30m;
        }

        // 2. Hoàn thành tốt nhiệm vụ (hoặc >= 70 điểm) -> 1.15
        if (text.Contains("tốt") || text.Contains("tot") || kpiScore >= 70.0m)
        {
            return 1.15m;
        }

        // 3. Hoàn thành nhiệm vụ (hoặc >= 50 điểm) -> 1.0
        if ((text.Contains("hoàn thành") && !text.Contains("không")) || kpiScore >= 50.0m)
        {
            return 1.00m;
        }

        // 4. Không hoàn thành nhiệm vụ -> 0.7
        return 0.70m;
    }

    /// <summary>
    /// Chuyển đổi số tiền thành chữ tiếng Việt chuẩn kế toán ("Một triệu năm trăm nghìn đồng chẵn.")
    /// </summary>
    public string ConvertNumberToVietnameseWords(decimal number)
    {
        long n = (long)Math.Round(number);
        if (n <= 0) return "Không đồng chẵn.";

        string[] digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        string[] units = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };

        List<string> rawGroups = new();
        string numStr = n.ToString();

        while (numStr.Length > 0)
        {
            int take = Math.Min(numStr.Length, 3);
            int start = numStr.Length - take;
            rawGroups.Add(numStr.Substring(start, take));
            numStr = numStr.Substring(0, start);
        }

        List<string> resultParts = new();

        for (int i = rawGroups.Count - 1; i >= 0; i--)
        {
            int val = int.Parse(rawGroups[i]);
            if (val == 0) continue;

            string gStr = rawGroups[i].PadLeft(3, '0');
            int h = gStr[0] - '0';
            int t = gStr[1] - '0';
            int o = gStr[2] - '0';

            bool isLeading = (i == rawGroups.Count - 1);
            List<string> part = new();

            if (h > 0)
            {
                part.Add(digits[h] + " trăm");
            }
            else if (!isLeading)
            {
                if (t == 0 && o > 0)
                {
                    part.Add("lẻ");
                }
                else if (t > 0)
                {
                    part.Add("không trăm");
                }
            }

            // Hàng chục
            if (t > 1)
            {
                part.Add(digits[t] + " mươi");
            }
            else if (t == 1)
            {
                part.Add("mười");
            }
            else if (t == 0 && h > 0 && o > 0)
            {
                part.Add("lẻ");
            }

            // Hàng đơn vị
            if (o == 1)
            {
                if (t > 1) part.Add("mốt");
                else part.Add("một");
            }
            else if (o == 5)
            {
                if (t > 0) part.Add("lăm");
                else part.Add("năm");
            }
            else if (o > 0)
            {
                part.Add(digits[o]);
            }

            string unit = units[i];
            string sub = string.Join(" ", part);
            if (!string.IsNullOrEmpty(unit))
            {
                sub += " " + unit;
            }

            resultParts.Add(sub);
        }

        string text = string.Join(" ", resultParts);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        if (string.IsNullOrEmpty(text)) return "Không đồng chẵn.";

        text = char.ToUpper(text[0]) + text.Substring(1);
        return text + " đồng chẵn.";
    }

    private static string GenerateVerificationHash(string voucherCode, decimal amount, string staffCode)
    {
        using var sha = SHA256.Create();
        var raw = $"{voucherCode}|{amount}|{staffCode}|KPI_VOUCHER_SECRET";
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
