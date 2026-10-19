<?php
namespace App\Controllers;

use App\Models\Evaluation;
use App\Models\KpiAssignment;
use App\Models\KpiCatalog;
use App\Services\KpiCalculationService;
use App\Services\AuditService;

class EvaluationController {
    public function showSelfAssess(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $userId = $_SESSION['user_id'];
        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026'];
        $assignment = KpiAssignment::getByPeriodAndEmployee($period['id'], $userId);

        if (!$assignment) {
            $_SESSION['error'] = 'Bạn chưa được giao bản chỉ tiêu KPI cho kỳ này.';
            header('Location: /dashboard');
            exit;
        }

        $evaluation = Evaluation::getByAssignmentId($assignment['id']);
        require BASE_PATH . '/app/Views/evaluation/self_assess.php';
    }

    public function submitSelfAssess(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $evalId = intval($_POST['evaluation_id'] ?? 0);
        $items = $_POST['items'] ?? [];

        if (empty($evalId) || empty($items)) {
            $_SESSION['error'] = 'Dữ liệu đánh giá không hợp lệ.';
            header('Location: /evaluation/self-assess');
            exit;
        }

        // Xử lý tệp đính kèm và kiểm tra BR-09 (Bắt buộc minh chứng khi đạt >= 120%)
        foreach ($items as $detailId => $item) {
            $actual = floatval($item['actual_value'] ?? 0);
            $target = floatval($item['target_value'] ?? 100);
            $formula = $item['scoring_formula'] ?? 'HigherIsBetter';
            $rate = KpiCalculationService::calculateCompletionRate($actual, $target, $formula);

            // Xử lý upload file nếu có
            if (!empty($_FILES['evidence_' . $detailId]['name'])) {
                $file = $_FILES['evidence_' . $detailId];
                if ($file['error'] === UPLOAD_ERR_OK) {
                    $ext = pathinfo($file['name'], PATHINFO_EXTENSION);
                    $destName = 'evidence_' . $evalId . '_' . $detailId . '_' . time() . '.' . $ext;
                    $destPath = BASE_PATH . '/public/uploads/' . $destName;
                    move_uploaded_file($file['tmp_name'], $destPath);
                    $items[$detailId]['evidence_file'] = $destName;
                }
            }

            // Kiểm tra BR-09: Nếu kết quả >= 120% nhưng chưa có file minh chứng đính kèm
            if ($rate >= KPI_EVIDENCE_THRESHOLD && empty($items[$detailId]['evidence_file']) && empty($item['existing_evidence'])) {
                $_SESSION['error'] = "Chỉ tiêu '{$item['kpi_name']}' đạt {$rate}% (>= 120%). Bắt buộc phải đính kèm tệp minh chứng chứng minh thành tích vượt trần (BR-09).";
                header('Location: /evaluation/self-assess');
                exit;
            }
        }

        try {
            Evaluation::saveSelfAssessment($evalId, $items);
            AuditService::log($_SESSION['user_id'], 'SUBMIT_SELF_ASSESSMENT', 'evaluations', $evalId, 'Nộp phiếu tự đánh giá');

            $_SESSION['flash_message'] = 'Nộp phiếu tự đánh giá thành công! Hồ sơ đã được chuyển lên Quản lý trực tiếp để thẩm định.';
            header('Location: /dashboard');
            exit;
        } catch (\Exception $e) {
            $_SESSION['error'] = 'Lỗi hệ thống: ' . $e->getMessage();
            header('Location: /evaluation/self-assess');
            exit;
        }
    }

    public function showManagerReview(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $assignmentId = intval($_GET['assignment_id'] ?? 0);
        $evaluation = Evaluation::getByAssignmentId($assignmentId);

        if (!$evaluation) {
            $_SESSION['error'] = 'Không tìm thấy hồ sơ đánh giá.';
            header('Location: /dashboard');
            exit;
        }

        require BASE_PATH . '/app/Views/evaluation/manager_review.php';
    }

    public function submitManagerReview(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $evalId = intval($_POST['evaluation_id'] ?? 0);
        $items = $_POST['items'] ?? [];
        $assignmentId = intval($_POST['assignment_id'] ?? 0);

        // Kiểm tra BR-05: Lệch điểm > 10% bắt buộc giải trình >= 50 ký tự
        foreach ($items as $item) {
            $selfScore = floatval($item['self_score'] ?? 0);
            $mgrScore = floatval($item['manager_score'] ?? 0);
            $reason = trim($item['score_gap_reason'] ?? '');

            $gapCheck = KpiCalculationService::checkScoreGap($selfScore, $mgrScore, $reason);
            if (!$gapCheck['isValid']) {
                $_SESSION['error'] = $gapCheck['error'];
                header("Location: /evaluation/manager-review?assignment_id=$assignmentId");
                exit;
            }
        }

        try {
            Evaluation::saveManagerReview($evalId, $items);
            AuditService::log($_SESSION['user_id'], 'MANAGER_REVIEW', 'evaluations', $evalId, 'Quản lý hoàn tất chấm thẩm định');

            $_SESSION['flash_message'] = 'Thẩm định và chấm điểm nhân viên thành công! Hồ sơ đã được trình cấp Lãnh đạo phê chuẩn.';
            header('Location: /dashboard');
            exit;
        } catch (\Exception $e) {
            $_SESSION['error'] = 'Lỗi hệ thống: ' . $e->getMessage();
            header("Location: /evaluation/manager-review?assignment_id=$assignmentId");
            exit;
        }
    }

    public function approve(): void {
        if (empty($_SESSION['user_id']) || !in_array($_SESSION['role'], ['Director', 'Super Admin'])) {
            $_SESSION['error'] = 'Bạn không có thẩm quyền phê duyệt kết quả cuối cùng.';
            header('Location: /dashboard');
            exit;
        }

        $evalId = intval($_POST['evaluation_id'] ?? 0);
        $finalScore = floatval($_POST['final_score'] ?? 0);
        $finalRank = trim($_POST['final_classification'] ?? 'Hoàn thành tốt nhiệm vụ');

        Evaluation::finalizeApproval($evalId, $finalScore, $finalRank);
        AuditService::log($_SESSION['user_id'], 'FINAL_APPROVE', 'evaluations', $evalId, "Phê chuẩn kết quả cuối cùng: $finalRank ($finalScore điểm)");

        $_SESSION['flash_message'] = "Đã phê duyệt và đóng băng dữ liệu kết quả đánh giá (BR-15)! Mức xếp loại: <strong>$finalRank</strong>.";
        header('Location: /dashboard');
        exit;
    }
}
