using System.Security.Claims;
using KPI.Application.Common.Exceptions;
using KPI.Application.DTOs;
using KPI.Application.Services;
using KPI.Web.ViewModels.Planning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[Authorize]
public class KpiPlanningController : Controller
{
    private readonly IKpiPlanningService _planningService;
    private readonly ICatalogService _catalogService;
    private readonly IDirectorTargetService _targetService;
    private readonly ILogger<KpiPlanningController> _logger;

    public KpiPlanningController(
        IKpiPlanningService planningService,
        ICatalogService catalogService,
        IDirectorTargetService targetService,
        ILogger<KpiPlanningController> logger)
    {
        _planningService = planningService;
        _catalogService = catalogService;
        _targetService = targetService;
        _logger = logger;
    }

    #region 1. Màn hình Giao KPI (Wizard 3 bước cho Quản lý)
    [HttpGet]
    public async Task<IActionResult> Assign(long? employeeId, long? periodId, long? id)
    {
        var currentUserId = GetCurrentUserId();
        var employees = await _planningService.GetEmployeesForAssignmentAsync(currentUserId);
        var periods = await _planningService.GetEvaluationPeriodsForPlanningAsync();
        var masterKpis = await _catalogService.GetKpiMasterItemsAsync();

        KpiAssignmentDto? existing = null;
        if (id.HasValue && id.Value > 0)
        {
            existing = await _planningService.GetAssignmentByIdAsync(id.Value);
            if (existing != null)
            {
                employeeId ??= existing.EmployeeId;
                periodId ??= existing.PeriodId;
            }
        }

        // Chọn mặc định kỳ đánh giá đầu tiên nếu chưa chọn
        periodId ??= periods.FirstOrDefault(p => p.IsActiveForPlanning)?.Id ?? periods.FirstOrDefault()?.Id;

        TemplateSuggestDto? suggestions = null;
        if (employeeId.HasValue && employeeId.Value > 0)
        {
            suggestions = await _planningService.GetTemplateSuggestionsAsync(employeeId.Value);
        }

        var model = new AssignKpiViewModel
        {
            Employees = employees,
            Periods = periods,
            MasterKpis = masterKpis,
            SelectedEmployeeId = employeeId,
            SelectedPeriodId = periodId,
            ExistingAssignment = existing,
            SuggestedTemplate = suggestions
        };

        return View(model);
    }
    #endregion

    #region 2. Màn hình "KPI Của Tôi" (Employee / Officer)
    [HttpGet]
    public async Task<IActionResult> MyKpi(long? periodId, string viewMode = "cards")
    {
        var currentUserId = GetCurrentUserId();
        var periods = await _planningService.GetEvaluationPeriodsForPlanningAsync();
        var selectedPeriod = periodId ?? periods.FirstOrDefault()?.Id ?? 1;

        var assignment = await _planningService.GetMyKpiAsync(currentUserId, selectedPeriod);

        var model = new MyKpiViewModel
        {
            Assignment = assignment,
            Periods = periods,
            SelectedPeriodId = selectedPeriod,
            ViewMode = viewMode
        };

        return View(model);
    }
    #endregion

    #region 3. Màn hình Xác nhận Cam kết Điện tử (Employee)
    [HttpGet]
    public async Task<IActionResult> Confirm(long id)
    {
        var assignment = await _planningService.GetAssignmentByIdAsync(id);
        if (assignment == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy bản giao KPI yêu cầu.";
            return RedirectToAction(nameof(MyKpi));
        }

        var model = new StaffConfirmViewModel
        {
            Assignment = assignment,
            CanConfirm = assignment.Status == Domain.Enums.AssignmentStatus.Pending_Staff_Confirm
        };

        return View(model);
    }
    #endregion

    #region 4. Màn hình Phê duyệt (Director / Manager)
    [HttpGet]
    public async Task<IActionResult> Approvals(long? periodId, long? departmentId, string? status, string? search)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        var assignments = await _planningService.GetAssignmentsAsync(periodId, departmentId, status, search, currentUserId, role);
        var periods = await _planningService.GetEvaluationPeriodsForPlanningAsync();
        var departments = await _catalogService.GetAllDepartmentsFlatAsync();

        var model = new ApprovalListViewModel
        {
            Assignments = assignments,
            Periods = periods,
            Departments = departments,
            SelectedPeriodId = periodId,
            SelectedDepartmentId = departmentId,
            SelectedStatus = status,
            SearchKeyword = search
        };

        return View(model);
    }
    #endregion

    #region 5. Màn hình So sánh Diff Điều chỉnh Giữa kỳ
    [HttpGet]
    public async Task<IActionResult> Diff(long id)
    {
        var diffData = await _planningService.GetRevisionDiffAsync(id);
        var assignment = await _planningService.GetAssignmentByIdAsync(id);

        var model = new RevisionDiffViewModel
        {
            DiffData = diffData,
            Assignment = assignment
        };

        return View(model);
    }
    #endregion

    #region 6. AJAX Endpoints Thực thi Nghiệp vụ (BR-01, BR-02, BR-03)
    [HttpPost]
    public async Task<IActionResult> SaveDraft([FromBody] CreateAssignmentRequest request)
    {
        try
        {
            request.IsDraft = true;
            var id = await _planningService.SaveOrSubmitAssignmentAsync(request, GetCurrentUserId());
            return Json(new { success = true, id, message = "Đã lưu bản nháp giao KPI thành công!" });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, ruleCode = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi lưu bản nháp: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] CreateAssignmentRequest request)
    {
        try
        {
            request.IsDraft = false;
            var id = await _planningService.SaveOrSubmitAssignmentAsync(request, GetCurrentUserId());
            return Json(new { success = true, id, message = "Đã gửi bản giao KPI đến nhân viên để cam kết mục tiêu!" });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, ruleCode = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi gửi bản giao: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> StaffConfirm([FromBody] StaffConfirmRequest request)
    {
        try
        {
            var success = await _planningService.StaffConfirmAssignmentAsync(request, GetCurrentUserId());
            return Json(new {
                success,
                message = success ? "Ký cam kết điện tử thành công! Hồ sơ đã chuyển đến Lãnh đạo phê duyệt." : "Không thể ký cam kết."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> StaffRequestAdjustment([FromBody] StaffConfirmRequest request)
    {
        try
        {
            var success = await _planningService.StaffRequestAdjustmentAsync(request.AssignmentId, request.AdjustmentReason ?? "Yêu cầu xem xét lại", GetCurrentUserId());
            return Json(new {
                success,
                message = success ? "Đã gửi yêu cầu điều chỉnh bản giao KPI đến Quản lý." : "Không thể gửi yêu cầu."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Approve([FromBody] ApproveAssignmentRequest request)
    {
        try
        {
            var success = await _planningService.DirectorApproveAsync(request, GetCurrentUserId());
            return Json(new {
                success,
                message = success ? "[BR-03] Đã phê duyệt và KHÓA toàn bộ bản giao KPI thành công! Kế hoạch chính thức có hiệu lực." : "Lỗi phê duyệt."
            });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, ruleCode = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Reject([FromBody] RejectAssignmentRequest request)
    {
        try
        {
            var success = await _planningService.DirectorRejectAsync(request, GetCurrentUserId());
            return Json(new {
                success,
                message = success ? "Đã trả lại bản giao KPI kèm lý do cho Quản lý điều chỉnh." : "Lỗi từ chối."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplateSuggestions(long employeeId)
    {
        var suggestions = await _planningService.GetTemplateSuggestionsAsync(employeeId);
        return Json(new { success = suggestions != null, data = suggestions });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRevision([FromBody] RevisionRequest request)
    {
        try
        {
            var success = await _planningService.CreateRevisionAsync(request, GetCurrentUserId());
            return Json(new {
                success,
                message = success ? "Đã tạo phiên bản điều chỉnh giữa kỳ và chuyển Lãnh đạo phê duyệt!" : "Không thể tạo điều chỉnh."
            });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, ruleCode = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteDraft(long id)
    {
        try
        {
            var success = await _planningService.DeleteDraftAssignmentAsync(id, GetCurrentUserId());
            return Json(new { success, message = success ? "Đã xóa bản nháp giao KPI thành công!" : "Không thể xóa." });
        }
        catch (BusinessRuleException ex)
        {
            return Json(new { success = false, ruleCode = ex.RuleCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAssignmentDetails(long id)
    {
        var data = await _planningService.GetAssignmentByIdAsync(id);
        if (data == null) return NotFound();
        return Json(data);
    }
    #endregion

    #region 5. Mục tiêu Ban Giám đốc (Director Stretch Targets)
    [HttpGet]
    public async Task<IActionResult> DirectorTargets(long? periodId, long? departmentId)
    {
        var periods = await _planningService.GetEvaluationPeriodsForPlanningAsync();
        var selectedPeriodId = periodId ?? periods.FirstOrDefault(p => p.IsActiveForPlanning)?.Id ?? periods.FirstOrDefault()?.Id ?? 1;

        ViewBag.Periods = periods;
        ViewBag.SelectedPeriodId = selectedPeriodId;
        ViewBag.DepartmentId = departmentId;

        var targets = await _targetService.GetTargetsAsync(selectedPeriodId, departmentId);
        return View(targets);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDirectorTarget(CreateDirectorTargetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetName) || request.TargetValue <= 0)
        {
            TempData["ErrorMessage"] = "Tên mục tiêu và Chỉ tiêu định mức không được để trống.";
            return RedirectToAction(nameof(DirectorTargets), new { periodId = request.PeriodId });
        }

        var result = await _targetService.SaveTargetAsync(request, GetCurrentUserId());
        if (result)
        {
            TempData["SuccessMessage"] = "Đã lưu mục tiêu chỉ đạo của Ban Giám đốc thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể lưu mục tiêu Ban Giám đốc.";
        }

        return RedirectToAction(nameof(DirectorTargets), new { periodId = request.PeriodId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDirectorTarget(long id, long periodId)
    {
        var result = await _targetService.DeleteTargetAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Đã xóa mục tiêu thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể xóa mục tiêu này.";
        }

        return RedirectToAction(nameof(DirectorTargets), new { periodId });
    }
    #endregion

    #region Helper Claims
    private long GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(idClaim, out var id) ? id : 1;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "Officer";
    }
    #endregion
}
