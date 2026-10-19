using KPI.Application.DTOs;

namespace KPI.Application.Services;

/// <summary>
/// Giao diện dịch vụ Phân hệ Đánh giá hiệu suất & Workflow Engine
/// Thực thi các quy tắc: BR-04, BR-05, BR-09, BR-10, BR-12, BR-15
/// </summary>
public interface IEvaluationService
{
    /// <summary>
    /// Lấy hoặc tự động khởi tạo phiếu đánh giá cho một bản giao KPI
    /// </summary>
    Task<EvaluationDto?> GetOrCreateEvaluationForAssignmentAsync(long assignmentId, long currentUserId);

    /// <summary>
    /// Lấy chi tiết phiếu đánh giá theo Id
    /// </summary>
    Task<EvaluationDto?> GetEvaluationByIdAsync(long evaluationId, long currentUserId);

    /// <summary>
    /// Lấy phiếu đánh giá kỳ hiện tại của cán bộ đang đăng nhập
    /// </summary>
    Task<EvaluationDto?> GetMyActiveEvaluationAsync(long currentUserId);

    /// <summary>
    /// Lấy danh sách hồ sơ cần Quản lý thẩm định (BR-04: chỉ khi nhân viên đã nộp)
    /// </summary>
    Task<List<EvaluationDto>> GetPendingManagerReviewsAsync(long managerUserId, long? periodId = null, long? departmentId = null);

    /// <summary>
    /// Lấy danh sách hồ sơ trình Lãnh đạo viện phê chuẩn
    /// </summary>
    Task<List<EvaluationDto>> GetPendingDirectorApprovalsAsync(long? periodId = null, long? departmentId = null);

    /// <summary>
    /// Lưu nháp hoặc nộp chính thức bản tự đánh giá của nhân viên (BR-09, BR-10, BR-12)
    /// </summary>
    Task<EvaluationDto> SaveOrSubmitSelfAssessmentAsync(SubmitSelfAssessmentRequest request, long currentUserId);

    /// <summary>
    /// Lưu nháp hoặc hoàn tất thẩm định của Quản lý (BR-04, BR-05)
    /// </summary>
    Task<EvaluationDto> SaveOrSubmitManagerReviewAsync(SubmitManagerReviewRequest request, long currentUserId);

    /// <summary>
    /// Lãnh đạo viện phê duyệt chính thức và đóng băng dữ liệu (BR-15)
    /// </summary>
    Task<EvaluationDto> ApproveByDirectorAsync(DirectorApproveRequest request, long currentUserId);

    /// <summary>
    /// Trả lại hồ sơ đánh giá cho Quản lý hoặc Nhân viên
    /// </summary>
    Task<EvaluationDto> RejectEvaluationAsync(RejectEvaluationRequest request, long currentUserId);

    /// <summary>
    /// Tải lên tệp minh chứng số hóa cho một chỉ tiêu (BR-09)
    /// </summary>
    Task<EvidenceItemDto> UploadEvidenceAsync(long assignmentDetailId, string fileName, string fileType, long fileSize, Stream fileStream, string? description, long currentUserId);

    /// <summary>
    /// Xóa tệp minh chứng số hóa
    /// </summary>
    Task<bool> DeleteEvidenceAsync(long evidenceId, long currentUserId);

    /// <summary>
    /// Nộp đơn khiếu nại kết quả đánh giá trong 72h (BR-14)
    /// </summary>
    Task<EvaluationDisputeDto> SubmitDisputeAsync(SubmitDisputeRequest request, long currentUserId);

    /// <summary>
    /// Xử lý và giải quyết đơn khiếu nại kết quả đánh giá
    /// </summary>
    Task<EvaluationDisputeDto> ResolveDisputeAsync(ResolveDisputeRequest request, long currentUserId);

    /// <summary>
    /// Lấy danh sách các đơn khiếu nại trong kỳ
    /// </summary>
    Task<List<EvaluationDisputeDto>> GetDisputesAsync(long? periodId = null);

    /// <summary>
    /// Đánh giá 360 độ từ đồng nghiệp (Peer Review)
    /// </summary>
    Task<PeerReviewDto> SubmitPeerReviewAsync(SubmitPeerReviewRequest request, long currentUserId);

    /// <summary>
    /// Tự động quét và khóa/gán 0 điểm cho các hồ sơ quá hạn nộp tự chấm (BR-12)
    /// </summary>
    Task<int> CheckAndAutoCloseExpiredEvaluationsAsync();
}
