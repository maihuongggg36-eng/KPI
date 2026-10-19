using System.Security.Claims;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Domain.Enums;
using KPI.Web.ViewModels.Evaluation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[Authorize]
public class EvaluationController : Controller
{
    private readonly IEvaluationService _evaluationService;
    private readonly ICatalogService _catalogService;
    private readonly IDecree90Service _decree90Service;
    private readonly ILogger<EvaluationController> _logger;

    public EvaluationController(
        IEvaluationService evaluationService,
        ICatalogService catalogService,
        IDecree90Service decree90Service,
        ILogger<EvaluationController> logger)
    {
        _evaluationService = evaluationService;
        _catalogService = catalogService;
        _decree90Service = decree90Service;
        _logger = logger;
    }

    #region 1. Phiếu Tự Đánh Giá (Employee)

    [HttpGet]
    public async Task<IActionResult> SelfAssess(long? assignmentId, long? evaluationId)
    {
        var currentUserId = GetCurrentUserId();
        EvaluationDto? evaluation = null;

        if (evaluationId.HasValue && evaluationId.Value > 0)
        {
            evaluation = await _evaluationService.GetEvaluationByIdAsync(evaluationId.Value, currentUserId);
        }
        else if (assignmentId.HasValue && assignmentId.Value > 0)
        {
            evaluation = await _evaluationService.GetOrCreateEvaluationForAssignmentAsync(assignmentId.Value, currentUserId);
        }
        else
        {
            evaluation = await _evaluationService.GetMyActiveEvaluationAsync(currentUserId);
        }

        if (evaluation == null)
        {
            TempData["ErrorMessage"] = "Bạn chưa có bản giao KPI nào trong kỳ đánh giá hiện tại hoặc hồ sơ chưa sẵn sàng.";
            return RedirectToAction("MyKpi", "KpiPlanning");
        }

        var currentUserRole = GetCurrentUserRole();
        bool isReadOnly = evaluation.IsLocked || 
                          evaluation.Status != EvaluationStatus.Not_Started && 
                          evaluation.Status != EvaluationStatus.Self_Assessing;

        var viewModel = new SelfAssessmentViewModel
        {
            Evaluation = evaluation,
            CurrentUserRole = currentUserRole,
            IsReadOnly = isReadOnly
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSelfAssess([FromBody] SubmitSelfAssessmentRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _evaluationService.SaveOrSubmitSelfAssessmentAsync(request, currentUserId);
            var msg = request.IsDraft 
                ? "Lưu nháp phiếu tự đánh giá thành công!" 
                : "Nộp chính thức phiếu tự đánh giá thành công! Hồ sơ đã được chuyển lên Quản lý thẩm định.";

            return Json(new { success = true, message = msg, data = result });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, code = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nộp phiếu tự đánh giá");
            return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEvidence([FromForm] long assignmentDetailId, [FromForm] string? description, IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Vui lòng chọn tệp tài liệu minh chứng để tải lên." });
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            using var stream = file.OpenReadStream();
            var evidence = await _evaluationService.UploadEvidenceAsync(
                assignmentDetailId,
                file.FileName,
                file.ContentType,
                file.Length,
                stream,
                description,
                currentUserId);

            return Json(new { success = true, message = "Tải lên tài liệu minh chứng thành công!", evidence });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, code = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi upload file minh chứng");
            return Json(new { success = false, message = "Lỗi khi lưu tệp minh chứng: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvidence([FromBody] DeleteEvidenceRequest req)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _evaluationService.DeleteEvidenceAsync(req.EvidenceId, currentUserId);
            return Json(new { success, message = success ? "Đã xóa tài liệu minh chứng." : "Không tìm thấy tài liệu để xóa." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region 2. Thẩm Định Quản Lý (Manager)

    [HttpGet]
    public async Task<IActionResult> ManagerList(long? periodId, long? departmentId)
    {
        var currentUserId = GetCurrentUserId();
        var evaluations = await _evaluationService.GetPendingManagerReviewsAsync(currentUserId, periodId, departmentId);
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var departments = await _catalogService.GetAllDepartmentsFlatAsync();

        ViewBag.Periods = periods;
        ViewBag.Departments = departments;

        var model = new ManagerEvaluationListViewModel
        {
            Evaluations = evaluations,
            SelectedPeriodId = periodId,
            SelectedDepartmentId = departmentId
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ManagerReview(long evaluationId)
    {
        var currentUserId = GetCurrentUserId();
        var evaluation = await _evaluationService.GetEvaluationByIdAsync(evaluationId, currentUserId);

        if (evaluation == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ đánh giá.";
            return RedirectToAction(nameof(ManagerList));
        }

        // BR-04: Cảnh báo nếu nhân viên chưa nộp tự đánh giá
        bool isStaffSubmitted = evaluation.Status == EvaluationStatus.Self_Submitted || 
                                evaluation.Status == EvaluationStatus.Manager_Reviewing;

        var viewModel = new ManagerReviewViewModel
        {
            Evaluation = evaluation,
            CurrentUserRole = GetCurrentUserRole(),
            IsReadOnly = !isStaffSubmitted || evaluation.IsLocked
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitManagerReview([FromBody] SubmitManagerReviewRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _evaluationService.SaveOrSubmitManagerReviewAsync(request, currentUserId);
            var msg = request.IsDraft 
                ? "Lưu nháp thẩm định thành công!" 
                : "Hoàn tất thẩm định thành công! Hồ sơ đã được trình Lãnh đạo viện phê duyệt.";

            return Json(new { success = true, message = msg, data = result });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, code = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi thẩm định quản lý");
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnToStaff([FromBody] RejectEvaluationRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            request.TargetRole = "Staff";
            var result = await _evaluationService.RejectEvaluationAsync(request, currentUserId);
            return Json(new { success = true, message = "Đã trả lại hồ sơ yêu cầu cán bộ chỉnh sửa và bổ sung minh chứng.", data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region 3. Phê Duyệt Lãnh Đạo (Director)

    [HttpGet]
    public async Task<IActionResult> DirectorList(long? periodId, long? departmentId)
    {
        var evaluations = await _evaluationService.GetPendingDirectorApprovalsAsync(periodId, departmentId);
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        var departments = await _catalogService.GetAllDepartmentsFlatAsync();

        ViewBag.Periods = periods;
        ViewBag.Departments = departments;

        var model = new DirectorEvaluationListViewModel
        {
            Evaluations = evaluations,
            SelectedPeriodId = periodId,
            SelectedDepartmentId = departmentId
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DirectorReview(long evaluationId)
    {
        var currentUserId = GetCurrentUserId();
        var evaluation = await _evaluationService.GetEvaluationByIdAsync(evaluationId, currentUserId);

        if (evaluation == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ đánh giá.";
            return RedirectToAction(nameof(DirectorList));
        }

        // Gợi ý xếp loại chất lượng theo Nghị định 90
        var recommended = _decree90Service.DetermineCivilServantRank(
            evaluation.ManagerTotalScore > 0 ? evaluation.ManagerTotalScore : evaluation.SelfTotalScore,
            hasDisciplinaryAction: false,
            hasApprovedInitiative: true);

        var viewModel = new DirectorReviewViewModel
        {
            Evaluation = evaluation,
            HasApprovedInitiative = true,
            HasDisciplinaryAction = false,
            RecommendedRank = recommended
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve([FromBody] DirectorApproveRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _evaluationService.ApproveByDirectorAsync(request, currentUserId);
            return Json(new { success = true, message = $"Đã phê chuẩn kết quả đánh giá ({result.FinalClassification}) và đóng băng dữ liệu (BR-15)!", data = result });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, code = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi phê duyệt: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectToManager([FromBody] RejectEvaluationRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            request.TargetRole = "Manager";
            var result = await _evaluationService.RejectEvaluationAsync(request, currentUserId);
            return Json(new { success = true, message = "Đã trả lại hồ sơ yêu cầu Quản lý phòng ban rà soát lại kết quả.", data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region 4. Khiếu Nại & Đánh Giá 360 Độ (Disputes & Peer Reviews)

    [HttpGet]
    public async Task<IActionResult> Disputes(long? periodId)
    {
        var disputes = await _evaluationService.GetDisputesAsync(periodId);
        var periods = await _catalogService.GetEvaluationPeriodsAsync();
        ViewBag.Periods = periods;

        var model = new EvaluationDisputesViewModel
        {
            Disputes = disputes,
            SelectedPeriodId = periodId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitDispute([FromBody] SubmitDisputeRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _evaluationService.SubmitDisputeAsync(request, currentUserId);
            return Json(new { success = true, message = "Đã gửi đơn khiếu nại kết quả thành công! Hội đồng sẽ thụ lý trong thời gian quy định.", data = result });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, code = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi nộp khiếu nại: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _evaluationService.ResolveDisputeAsync(request, currentUserId);
            return Json(new { success = true, message = "Đã hoàn tất xử lý đơn khiếu nại.", data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPeerReview([FromBody] SubmitPeerReviewRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _evaluationService.SubmitPeerReviewAsync(request, currentUserId);
            return Json(new { success = true, message = "Gửi nhận xét đánh giá đồng nghiệp thành công!", data = result });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, code = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi gửi đánh giá đồng nghiệp: " + ex.Message });
        }
    }

    #endregion

    #region Helpers

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(claim, out var id))
        {
            return id;
        }
        return 1;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "Officer";
    }

    #endregion
}

public class DeleteEvidenceRequest
{
    public long EvidenceId { get; set; }
}
