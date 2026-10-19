using FluentAssertions;
using KPI.Application.DTOs;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Payroll;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using EvaluationEntity = KPI.Domain.Entities.Evaluation.Evaluation;

namespace KPI.Tests.Services;

public class PayrollServiceTests
{
    private KpiDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new KpiDbContext(options);
    }

    #region 1. Unit Tests Chuyển Đổi Số Sang Chữ Tiếng Việt Chuẩn Kế Toán

    [Theory]
    [InlineData(0, "Không đồng chẵn.")]
    [InlineData(1500000, "Một triệu năm trăm nghìn đồng chẵn.")]
    [InlineData(15250000, "Mười lăm triệu hai trăm năm mươi nghìn đồng chẵn.")]
    [InlineData(2005000, "Hai triệu lẻ năm nghìn đồng chẵn.")]
    [InlineData(21000000, "Hai mươi mốt triệu đồng chẵn.")]
    [InlineData(100000000, "Một trăm triệu đồng chẵn.")]
    public void ConvertNumberToVietnameseWords_ShouldReturnAccurateWords(decimal amount, string expected)
    {
        using var context = CreateInMemoryDbContext();
        var service = new PayrollService(context);

        var result = service.ConvertNumberToVietnameseWords(amount);

        result.Should().Be(expected);
    }

    #endregion

    #region 2. Unit Tests Mapping Hệ Số KPI (P3 Multiplier)

    [Theory]
    [InlineData("Hoàn thành xuất sắc nhiệm vụ", 95.0, 1.30)]
    [InlineData("Hoàn thành tốt nhiệm vụ", 85.0, 1.15)]
    [InlineData("Hoàn thành nhiệm vụ", 65.0, 1.00)]
    [InlineData("Không hoàn thành nhiệm vụ", 42.0, 0.70)]
    [InlineData(null, 92.5, 1.30)]
    [InlineData(null, 75.0, 1.15)]
    [InlineData(null, 55.0, 1.00)]
    [InlineData(null, 35.0, 0.70)]
    public void CalculateKpiMultiplier_ShouldReturnCorrectFactor(string? classification, decimal score, decimal expectedMultiplier)
    {
        using var context = CreateInMemoryDbContext();
        var service = new PayrollService(context);

        var multiplier = service.CalculateKpiMultiplier(classification, (decimal)score);

        multiplier.Should().Be(expectedMultiplier);
    }

    #endregion

    #region 3. Unit Tests Batch Calculate Payroll (Tính Lương P3 Toàn Bộ)

    [Fact]
    public async Task CalculateAllPayrollAsync_ShouldComputeP3AndNetSalaryCorrectly()
    {
        using var context = CreateInMemoryDbContext();

        // Seed Kỳ đánh giá
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

        // Seed Phòng ban & Chức danh
        var dept = new Department { Id = 1, Code = "CNTT", Name = "Trung tâm CNTT", Level = 1 };
        var pos = new Position { Id = 1, Code = "CHUYEN_VIEN", Name = "Chuyên viên chính" };
        context.Departments.Add(dept);
        context.Positions.Add(pos);

        // Seed Tài khoản Người dùng
        var user = new User
        {
            Id = 10,
            Username = "nguyenvana",
            Email = "vana@hocvien.edu.vn",
            Status = UserStatus.Active
        };
        context.Users.Add(user);

        // Seed Hồ sơ nhân viên
        var employee = new EmployeeProfile
        {
            Id = 10,
            UserId = 10,
            StaffCode = "CB-0010",
            FullName = "Nguyễn Văn A",
            DepartmentId = 1,
            PositionId = 1,
            JoinDate = new DateTime(2020, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        context.EmployeeProfiles.Add(employee);

        // Seed Hồ sơ công chức (Hệ số lương 2.0)
        var civilProfile = new CivilServantProfile
        {
            Id = 10,
            EmployeeId = 10,
            CivilServantCode = "CC-0010",
            SalaryCoefficient = 2.0m
        };
        context.CivilServantProfiles.Add(civilProfile);

        // Seed Đánh giá KPI hoàn thành xuất sắc (95 điểm)
        var eval = new EvaluationEntity
        {
            Id = 100,
            PeriodId = 1,
            EmployeeId = 10,
            Status = EvaluationStatus.Final_Approved,
            FinalScore = 95.0m,
            FinalClassification = "Hoàn thành xuất sắc nhiệm vụ"
        };
        context.Evaluations.Add(eval);

        await context.SaveChangesAsync();

        var service = new PayrollService(context);

        // ACT
        var count = await service.CalculateAllPayrollAsync(period.Id, 1);

        // ASSERT
        count.Should().Be(1);

        var record = await context.Payrolls.FirstOrDefaultAsync(p => p.PeriodId == period.Id && p.EmployeeId == employee.Id);
        record.Should().NotBeNull();
        record!.BaseSalary.Should().Be(8500000m); // Default position salary for CHUYEN_VIEN
        record.SalaryCoefficient.Should().Be(2.0m);
        record.KpiScore.Should().Be(95.0m);
        record.KpiMultiplier.Should().Be(1.30m);

        // Lương_P3 = 8,500,000 * 2.0 * 1.30 = 22,100,000
        record.P3Salary.Should().Be(22100000m);

        // Mã phiếu chi tự động KPI-PC-YYYY-NNNN
        record.VoucherCode.Should().StartWith("KPI-PC-2026-");
        record.PaymentStatus.Should().Be("Chưa thanh toán");
        record.PaymentMethod.Should().Be("Tiền mặt");
        record.VerificationHash.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region 4. Unit Tests Chi Tiền Mặt Đơn Lẻ & Hàng Loạt

    [Fact]
    public async Task MarkCashPaidAsync_ShouldMarkPaid_AndSetCashier()
    {
        using var context = CreateInMemoryDbContext();

        var payroll = new PayrollRecord
        {
            Id = 1,
            PeriodId = 1,
            EmployeeId = 10,
            BaseSalary = 8000000m,
            SalaryCoefficient = 1.5m,
            P3Salary = 12000000m,
            NetSalary = 20000000m,
            VoucherCode = "KPI-PC-2026-0001",
            PaymentStatus = "Chưa thanh toán"
        };
        context.Payrolls.Add(payroll);
        await context.SaveChangesAsync();

        var service = new PayrollService(context);

        // ACT: Chi tiền mặt cho cán bộ
        var success = await service.MarkCashPaidAsync(1, "Đã nhận tiền mặt tại quỹ", 99);

        // ASSERT
        success.Should().BeTrue();

        var updated = await context.Payrolls.FindAsync(1L);
        updated.Should().NotBeNull();
        updated!.PaymentStatus.Should().Be("Đã thanh toán");
        updated.CashierId.Should().Be(99);
        updated.PaidAt.Should().NotBeNull();
        updated.RecipientSignature.Should().Be("Đã nhận tiền mặt tại quỹ");
    }

    [Fact]
    public async Task MarkAllCashPaidAsync_ShouldUpdateAllPendingRecords()
    {
        using var context = CreateInMemoryDbContext();

        context.Payrolls.AddRange(
            new PayrollRecord
            {
                Id = 1,
                PeriodId = 1,
                EmployeeId = 10,
                NetSalary = 15000000m,
                PaymentStatus = "Chưa thanh toán",
                VoucherCode = "KPI-PC-2026-0001"
            },
            new PayrollRecord
            {
                Id = 2,
                PeriodId = 1,
                EmployeeId = 11,
                NetSalary = 18000000m,
                PaymentStatus = "Chưa thanh toán",
                VoucherCode = "KPI-PC-2026-0002"
            },
            new PayrollRecord
            {
                Id = 3,
                PeriodId = 1,
                EmployeeId = 12,
                NetSalary = 20000000m,
                PaymentStatus = "Đã thanh toán",
                VoucherCode = "KPI-PC-2026-0003"
            }
        );
        await context.SaveChangesAsync();

        var service = new PayrollService(context);

        // ACT
        var updatedCount = await service.MarkAllCashPaidAsync(1, 99);

        // ASSERT
        updatedCount.Should().Be(2);

        var remainingPending = await context.Payrolls.CountAsync(p => p.PeriodId == 1 && p.PaymentStatus == "Chưa thanh toán");
        remainingPending.Should().Be(0);

        var totalPaid = await context.Payrolls.CountAsync(p => p.PeriodId == 1 && p.PaymentStatus == "Đã thanh toán");
        totalPaid.Should().Be(3);
    }

    #endregion

    #region 5. Unit Tests Phiếu Chi Mẫu 02-TT & Phiếu Lương Cá Nhân

    [Fact]
    public async Task GetCashVoucherAsync_ShouldReturnValidVoucherDetails_AccordingToCircular133()
    {
        using var context = CreateInMemoryDbContext();

        var period = new EvaluationPeriod
        {
            Id = 1,
            Code = "Q1_2026",
            Name = "Quý 1/2026",
            Year = 2026,
            PeriodType = PeriodType.Quarterly,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31)
        };
        context.EvaluationPeriods.Add(period);

        var dept = new Department { Id = 2, Code = "KHTC", Name = "Phòng Kế hoạch - Tài chính", Level = 1 };
        var pos = new Position { Id = 2, Code = "CHUYEN_VIEN", Name = "Chuyên viên" };
        context.Departments.Add(dept);
        context.Positions.Add(pos);

        var employee = new EmployeeProfile
        {
            Id = 20,
            UserId = 20,
            StaffCode = "CB-0020",
            FullName = "Trần Thị B",
            DepartmentId = 2,
            PositionId = 2
        };
        context.EmployeeProfiles.Add(employee);

        var payroll = new PayrollRecord
        {
            Id = 5,
            PeriodId = 1,
            EmployeeId = 20,
            BaseSalary = 12000000m,
            SalaryCoefficient = 1.8m,
            P3Salary = 24840000m,
            NetSalary = 35000000m,
            VoucherCode = "KPI-PC-2026-0005",
            CreatedAt = new DateTime(2026, 3, 25)
        };
        context.Payrolls.Add(payroll);
        await context.SaveChangesAsync();

        var service = new PayrollService(context);

        // ACT
        var voucher = await service.GetCashVoucherAsync(5);

        // ASSERT
        voucher.Should().NotBeNull();
        voucher!.VoucherCode.Should().Be("KPI-PC-2026-0005");
        voucher.FullName.Should().Be("Trần Thị B");
        voucher.Amount.Should().Be(35000000m);
        voucher.AmountInWords.Should().Be("Ba mươi lăm triệu đồng chẵn.");
        voucher.DebitAccount.Should().Contain("TK 3341");
        voucher.CreditAccount.Should().Contain("TK 1111");
    }

    [Fact]
    public async Task GetMyPayslipAsync_ShouldReturn3PBreakdown()
    {
        using var context = CreateInMemoryDbContext();

        var period = new EvaluationPeriod
        {
            Id = 1,
            Code = "Q1_2026",
            Name = "Quý 1/2026",
            Year = 2026,
            PeriodType = PeriodType.Quarterly,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31)
        };
        context.EvaluationPeriods.Add(period);

        var dept = new Department { Id = 3, Code = "TCCB", Name = "Phòng Tổ chức Cán bộ", Level = 1 };
        var pos = new Position { Id = 3, Code = "CHUYEN_VIEN", Name = "Chuyên viên chính" };
        context.Departments.Add(dept);
        context.Positions.Add(pos);

        var user = new User { Id = 30, Username = "levanc" };
        context.Users.Add(user);

        var employee = new EmployeeProfile
        {
            Id = 30,
            UserId = 30,
            StaffCode = "CB-0030",
            FullName = "Lê Văn C",
            DepartmentId = 3,
            PositionId = 3
        };
        context.EmployeeProfiles.Add(employee);

        var payroll = new PayrollRecord
        {
            Id = 9,
            PeriodId = 1,
            EmployeeId = 30,
            BaseSalary = 9000000m,
            SalaryCoefficient = 2.0m,
            Allowances = 1500000m,
            KpiScore = 88.5m,
            KpiClassification = "Hoàn thành tốt nhiệm vụ",
            KpiMultiplier = 1.15m,
            P3Salary = 20700000m,
            GrossIncome = 40200000m,
            NetSalary = 35000000m,
            VoucherCode = "KPI-PC-2026-0009",
            PaymentStatus = "Đã thanh toán",
            VerificationHash = "HASH123456"
        };
        context.Payrolls.Add(payroll);
        await context.SaveChangesAsync();

        var service = new PayrollService(context);

        // ACT
        var payslip = await service.GetMyPayslipAsync(1, 30);

        // ASSERT
        payslip.Should().NotBeNull();
        payslip!.StaffCode.Should().Be("CB-0030");
        payslip.BaseSalary.Should().Be(9000000m);
        payslip.SalaryCoefficient.Should().Be(2.0m);
        payslip.Allowances.Should().Be(1500000m);
        payslip.KpiScore.Should().Be(88.5m);
        payslip.KpiMultiplier.Should().Be(1.15m);
        payslip.P3Salary.Should().Be(20700000m);
        payslip.NetSalary.Should().Be(35000000m);
        payslip.AmountInWords.Should().Be("Ba mươi lăm triệu đồng chẵn.");
        payslip.VerificationHash.Should().Be("HASH123456");
        payslip.QrCodeContent.Should().Contain("KPI-PC-2026-0009");
    }

    #endregion
}
