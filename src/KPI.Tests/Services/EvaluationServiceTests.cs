using FluentAssertions;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Evaluation;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Enums;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KPI.Tests.Services;

public class EvaluationServiceTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly IKpiCalculationService _calculationService;
    private readonly IDecree90Service _decree90Service;

    public EvaluationServiceTests()
    {
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockEnv.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());
        _calculationService = new KpiCalculationService();
        _decree90Service = new Decree90Service();
    }

    private KpiDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new KpiDbContext(options);
        SeedSampleData(context);
        return context;
    }

    private void SeedSampleData(KpiDbContext context)
    {
        var period = new EvaluationPeriod
        {
            Id = 1,
            ScaleId = 1,
            Code = "2026_Q1",
            Name = "Kỳ đánh giá Quý 1/2026",
            Year = 2026,
            Status = PeriodStatus.InEvaluation,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(1),
            SelfDeadline = DateTime.UtcNow.AddDays(7), // Còn hạn
            ManagerDeadline = DateTime.UtcNow.AddDays(14),
            ClosingDate = DateTime.UtcNow.AddDays(21)
        };

        var dept = new Department
        {
            Id = 1,
            OrgId = 1,
            Code = "PKHTC",
            Name = "Phòng Kế hoạch Tài chính"
        };

        var pos = new Position
        {
            Id = 1,
            Code = "CVC_TC",
            Name = "Chuyên viên chính"
        };

        var employeeUser = new User
        {
            Id = 3,
            Username = "officer1",
            Email = "officer@example.com",
            PasswordHash = "hashed",
            Status = UserStatus.Active
        };

        var employee = new EmployeeProfile
        {
            Id = 1,
            UserId = 3,
            DepartmentId = 1,
            PositionId = 1,
            StaffCode = "CB001",
            FullName = "Nguyễn Văn Cán Bộ",
            Department = dept,
            Position = pos
        };

        var masterItem1 = new KpiMasterItem
        {
            Id = 1,
            Code = "KPI_01",
            Name = "Thu ngân sách đào tạo",
            CategoryId = 1,
            UnitId = 1,
            MethodId = 1
        };

        var masterItem2 = new KpiMasterItem
        {
            Id = 2,
            Code = "KPI_02",
            Name = "Tỷ lệ hồ sơ thanh quyết toán đúng hạn",
            CategoryId = 1,
            UnitId = 1,
            MethodId = 1
        };

        var assignment = new KpiAssignment
        {
            Id = 1,
            PeriodId = 1,
            EmployeeId = 1,
            TotalWeight = 100.0m,
            Status = AssignmentStatus.Approved_Locked,
            Employee = employee,
            Period = period
        };

        var detail1 = new KpiAssignmentDetail
        {
            Id = 101,
            AssignmentId = 1,
            KpiItemId = 1,
            KpiName = "Thu ngân sách đào tạo",
            Unit = "Triệu VNĐ",
            TargetValue = 100.0m,
            Weight = 50.0m,
            MaxCap = 150.0m,
            ScoringFormula = "HigherIsBetter"
        };

        var detail2 = new KpiAssignmentDetail
        {
            Id = 102,
            AssignmentId = 1,
            KpiItemId = 2,
            KpiName = "Tỷ lệ hồ sơ thanh quyết toán đúng hạn",
            Unit = "%",
            TargetValue = 100.0m,
            Weight = 50.0m,
            MaxCap = 150.0m,
            ScoringFormula = "HigherIsBetter"
        };

        assignment.AssignmentDetails.Add(detail1);
        assignment.AssignmentDetails.Add(detail2);

        context.Users.Add(employeeUser);
        context.Departments.Add(dept);
        context.Positions.Add(pos);
        context.EmployeeProfiles.Add(employee);
        context.EvaluationPeriods.Add(period);
        context.KpiMasterItems.AddRange(masterItem1, masterItem2);
        context.KpiAssignments.Add(assignment);
        context.SaveChanges();
    }

    [Fact]
    public async Task GetOrCreateEvaluation_ShouldInitializeDetailsAndWorkflow_WhenNotExisting()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);

        // Act
        var result = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        // Assert
        result.Should().NotBeNull();
        result!.AssignmentId.Should().Be(1);
        result.EmployeeName.Should().Be("Nguyễn Văn Cán Bộ");
        result.Status.Should().Be(EvaluationStatus.Not_Started);
        result.Details.Should().HaveCount(2);
        result.Details.First().TargetValue.Should().Be(100.0m);
    }

    [Fact]
    public async Task SaveOrSubmitSelfAssessment_ShouldThrowBR09_WhenRateOver120AndNoEvidence()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        var request = new SubmitSelfAssessmentRequest
        {
            EvaluationId = eval!.Id,
            AssignmentId = 1,
            IsDraft = false, // Nộp chính thức
            Items = new List<SelfAssessmentItemRequest>
            {
                new() { AssignmentDetailId = 101, ActualValue = 130.0m, SelfRemarks = "Vượt chỉ tiêu 130%" }, // Rate = 130% >= 120%
                new() { AssignmentDetailId = 102, ActualValue = 100.0m, SelfRemarks = "Đạt 100%" }
            }
        };

        // Act & Assert
        var act = async () => await service.SaveOrSubmitSelfAssessmentAsync(request, 3);
        var exception = await act.Should().ThrowAsync<BusinessRuleException>();
        exception.Which.RuleCode.Should().Be("BR-09");
        exception.Which.Message.Should().ContainEquivalentOf("bắt buộc phải tải lên tệp minh chứng");
    }

    [Fact]
    public async Task SaveOrSubmitSelfAssessment_ShouldSucceed_WhenRateOver120AndHasEvidence()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        // Đính kèm 1 minh chứng cho chỉ tiêu 101
        context.KpiEvidences.Add(new KpiEvidence
        {
            AssignmentDetailId = 101,
            FileName = "BaoCaoNganSach.pdf",
            FilePath = "/uploads/evidences/test.pdf",
            FileType = "PDF",
            FileSize = 1024,
            UploadedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var request = new SubmitSelfAssessmentRequest
        {
            EvaluationId = eval!.Id,
            AssignmentId = 1,
            IsDraft = false, // Nộp chính thức
            Items = new List<SelfAssessmentItemRequest>
            {
                new() { AssignmentDetailId = 101, ActualValue = 130.0m, SelfRemarks = "Vượt chỉ tiêu 130%" },
                new() { AssignmentDetailId = 102, ActualValue = 100.0m, SelfRemarks = "Đạt 100%" }
            }
        };

        // Act
        var result = await service.SaveOrSubmitSelfAssessmentAsync(request, 3);

        // Assert
        result.Status.Should().Be(EvaluationStatus.Self_Submitted);
        result.SelfTotalScore.Should().Be(115.0m); // (130*50 + 100*50)/100 = 65 + 50 = 115
    }

    [Fact]
    public async Task SaveOrSubmitSelfAssessment_ShouldThrowBR12_WhenSelfDeadlineExpired()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var period = await context.EvaluationPeriods.FindAsync(1L);
        period!.SelfDeadline = DateTime.UtcNow.AddDays(-1); // Đã hết hạn
        await context.SaveChangesAsync();

        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        var request = new SubmitSelfAssessmentRequest
        {
            EvaluationId = eval!.Id,
            AssignmentId = 1,
            IsDraft = false,
            Items = new List<SelfAssessmentItemRequest>
            {
                new() { AssignmentDetailId = 101, ActualValue = 100.0m },
                new() { AssignmentDetailId = 102, ActualValue = 100.0m }
            }
        };

        // Act & Assert
        var act = async () => await service.SaveOrSubmitSelfAssessmentAsync(request, 3);
        var exception = await act.Should().ThrowAsync<BusinessRuleException>();
        exception.Which.RuleCode.Should().Be("BR-12");
        exception.Which.Message.Should().Contain("Đã hết thời hạn nộp tự đánh giá");
    }

    [Fact]
    public async Task SaveOrSubmitManagerReview_ShouldThrowBR04_WhenStaffNotSubmitted()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3); // Status: Not_Started

        var request = new SubmitManagerReviewRequest
        {
            EvaluationId = eval!.Id,
            IsDraft = false,
            Items = new List<ManagerReviewItemRequest>
            {
                new() { DetailId = eval.Details[0].Id, ManagerScore = 90.0m },
                new() { DetailId = eval.Details[1].Id, ManagerScore = 90.0m }
            }
        };

        // Act & Assert
        var act = async () => await service.SaveOrSubmitManagerReviewAsync(request, 2);
        var exception = await act.Should().ThrowAsync<BusinessRuleException>();
        exception.Which.RuleCode.Should().Be("BR-04");
        exception.Which.Message.Should().Contain("chỉ được phép thẩm định khi nhân viên đã nộp");
    }

    [Fact]
    public async Task SaveOrSubmitManagerReview_ShouldThrowBR05_WhenScoreGapOver10AndReasonLessThan50Chars()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        // Cho nhân viên nộp trước (SelfScore = 100)
        await service.SaveOrSubmitSelfAssessmentAsync(new SubmitSelfAssessmentRequest
        {
            EvaluationId = eval!.Id,
            AssignmentId = 1,
            IsDraft = false,
            Items = new List<SelfAssessmentItemRequest>
            {
                new() { AssignmentDetailId = 101, ActualValue = 100.0m },
                new() { AssignmentDetailId = 102, ActualValue = 100.0m }
            }
        }, 3);

        // Quản lý chấm 85 điểm (Lệch 15 điểm > 10 điểm) nhưng giải trình ngắn
        var mgrRequest = new SubmitManagerReviewRequest
        {
            EvaluationId = eval.Id,
            IsDraft = false,
            Items = new List<ManagerReviewItemRequest>
            {
                new() { DetailId = 101, ManagerScore = 85.0m, ScoreGapReason = "Số liệu chưa chính xác" }, // Ngắn < 50 chars
                new() { DetailId = 102, ManagerScore = 100.0m }
            }
        };

        // Act & Assert
        var act = async () => await service.SaveOrSubmitManagerReviewAsync(mgrRequest, 2);
        var exception = await act.Should().ThrowAsync<BusinessRuleException>();
        exception.Which.RuleCode.Should().Be("BR-05");
        exception.Which.Message.Should().Contain("bắt buộc phải có nội dung giải trình tối thiểu 50 ký tự");
    }

    [Fact]
    public async Task SaveOrSubmitManagerReview_ShouldSucceed_WhenScoreGapOver10AndReasonHasAtLeast50Chars()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        await service.SaveOrSubmitSelfAssessmentAsync(new SubmitSelfAssessmentRequest
        {
            EvaluationId = eval!.Id,
            AssignmentId = 1,
            IsDraft = false,
            Items = new List<SelfAssessmentItemRequest>
            {
                new() { AssignmentDetailId = 101, ActualValue = 100.0m },
                new() { AssignmentDetailId = 102, ActualValue = 100.0m }
            }
        }, 3);

        // Lý do hợp lệ >= 50 ký tự
        var validReason = "Qua đối chiếu thực tế với chứng từ phòng tài chính, số liệu chỉ tiêu đạt 85% tiến độ kế hoạch đề ra.";

        var mgrRequest = new SubmitManagerReviewRequest
        {
            EvaluationId = eval.Id,
            IsDraft = false,
            Items = new List<ManagerReviewItemRequest>
            {
                new() { DetailId = 101, ManagerScore = 85.0m, ScoreGapReason = validReason },
                new() { DetailId = 102, ManagerScore = 95.0m }
            }
        };

        // Act
        var result = await service.SaveOrSubmitManagerReviewAsync(mgrRequest, 2);

        // Assert
        result.Status.Should().Be(EvaluationStatus.Pending_Director_Approval);
        result.ManagerTotalScore.Should().Be(90.0m); // (85*50 + 95*50)/100 = 90
    }

    [Fact]
    public async Task ApproveByDirector_ShouldSetStatusFinalApprovedAndLockEvaluation_BR15()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        var approveRequest = new DirectorApproveRequest
        {
            EvaluationId = eval!.Id,
            FinalScore = 92.5m,
            FinalClassification = "Hoàn thành tốt nhiệm vụ",
            Remarks = "Đồng ý phê chuẩn kết quả đánh giá"
        };

        // Act
        var result = await service.ApproveByDirectorAsync(approveRequest, 1);

        // Assert (BR-15)
        result.Status.Should().Be(EvaluationStatus.Final_Approved);
        result.IsLocked.Should().BeTrue();
        result.FinalScore.Should().Be(92.5m);
        result.FinalClassification.Should().Be("Hoàn thành tốt nhiệm vụ");
        result.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitDispute_ShouldSucceed_Within72Hours_BR14()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        await service.ApproveByDirectorAsync(new DirectorApproveRequest
        {
            EvaluationId = eval!.Id,
            FinalScore = 88.0m,
            FinalClassification = "Hoàn thành tốt nhiệm vụ"
        }, 1);

        // Act (Nộp khiếu nại trong 72h)
        var dispute = await service.SubmitDisputeAsync(new SubmitDisputeRequest
        {
            EvaluationId = eval.Id,
            Reason = "Đề nghị tính thêm kết quả đề tài NCKH được nghiệm thu cuối kỳ",
            ProposedAdjustment = "Đạt Xuất sắc nhiệm vụ (>=90 điểm)"
        }, 3);

        // Assert
        dispute.Should().NotBeNull();
        dispute.Status.Should().Be(DisputeStatus.Open);
        dispute.Reason.Should().Contain("NCKH");

        var updatedEval = await service.GetEvaluationByIdAsync(eval.Id, 3);
        updatedEval!.IsDisputed.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAndAutoCloseExpiredEvaluations_ShouldAssignZeroAndClose_BR12()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new EvaluationService(context, _calculationService, _decree90Service, _mockEnv.Object);
        var eval = await service.GetOrCreateEvaluationForAssignmentAsync(1, 3);

        // Sửa kỳ đánh giá thành quá hạn
        var period = await context.EvaluationPeriods.FindAsync(1L);
        period!.SelfDeadline = DateTime.UtcNow.AddHours(-5);
        await context.SaveChangesAsync();

        // Act (Chạy job tự động quét)
        var closedCount = await service.CheckAndAutoCloseExpiredEvaluationsAsync();

        // Assert (BR-12)
        closedCount.Should().Be(1);

        var updatedEval = await service.GetEvaluationByIdAsync(eval!.Id, 3);
        updatedEval!.Status.Should().Be(EvaluationStatus.Self_Submitted);
        updatedEval.SelfTotalScore.Should().Be(0.0m);
        updatedEval.Details.First().SelfRemarks.Should().Contain("tự động gán 0 điểm do quá thời hạn");
    }
}
