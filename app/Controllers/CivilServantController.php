<?php
namespace App\Controllers;

use App\Models\CivilServant;
use App\Models\KpiCatalog;
use App\Services\Decree90Service;
use App\Services\AuditService;

class CivilServantController {
    public function index(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026', 'year' => 2026];
        $civilServants = CivilServant::getAllWithEvaluation($period['id']);

        // Đếm số lượng theo mức xếp loại để kiểm tra BR-06 (Khống chế Xuất sắc <= 20% Tốt)
        $countExcellent = 0;
        $countGood = 0;
        $countSatisfactory = 0;
        $countUnsatisfactory = 0;

        foreach ($civilServants as $cs) {
            $rank = $cs['finalized_rank'] ?? $cs['final_classification'] ?? '';
            if (str_contains($rank, 'xuất sắc') || str_contains($rank, 'Xuất sắc')) {
                $countExcellent++;
            } elseif (str_contains($rank, 'tốt') || str_contains($rank, 'Tốt')) {
                $countGood++;
            } elseif (str_contains($rank, 'Không hoàn thành') || str_contains($rank, 'không hoàn thành')) {
                $countUnsatisfactory++;
            } elseif (!empty($rank)) {
                $countSatisfactory++;
            }
        }

        $capValidation = Decree90Service::validateExcellenceCap($countGood, $countExcellent);

        require BASE_PATH . '/app/Views/civil_servant/index.php';
    }

    public function showForm(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $userId = intval($_GET['user_id'] ?? $_SESSION['user_id']);
        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026', 'year' => 2026];
        
        $data = CivilServant::getDetailForForm($userId, $period['id']);
        if (!$data) {
            $_SESSION['error'] = 'Không tìm thấy hồ sơ cán bộ công chức.';
            header('Location: /civil-servant');
            exit;
        }

        // Form 01 dành cho lãnh đạo, Form 02 dành cho chuyên viên
        $isLeadership = !empty($data['is_leadership']);
        $formType = $isLeadership ? 'Mẫu số 01 (Lãnh đạo, quản lý)' : 'Mẫu số 02 (Công chức không giữ chức vụ lãnh đạo)';

        require BASE_PATH . '/app/Views/civil_servant/form_01_02.php';
    }

    public function updateClassification(): void {
        if (empty($_SESSION['user_id']) || !in_array($_SESSION['role'], ['Director', 'HR Admin', 'Super Admin'])) {
            $_SESSION['error'] = 'Bạn không có quyền cập nhật phân loại cán bộ công chức.';
            header('Location: /civil-servant');
            exit;
        }

        $evalId = intval($_POST['evaluation_id'] ?? 0);
        $civilServantId = intval($_POST['civil_servant_id'] ?? 0);
        $politicalScore = intval($_POST['political_ethics_score'] ?? 90);
        $disciplineScore = intval($_POST['work_discipline_score'] ?? 90);
        $dutyScore = intval($_POST['duty_execution_score'] ?? 90);
        $hasDiscipline = !empty($_POST['has_disciplinary_action']) ? 1 : 0;
        $hasInitiative = !empty($_POST['has_approved_initiative']) ? 1 : 0;
        $proposedRank = trim($_POST['proposed_rank'] ?? '');
        $finalRank = trim($_POST['finalized_rank'] ?? $proposedRank);
        $comments = trim($_POST['collective_comments'] ?? '');
        $partyOpinion = trim($_POST['party_opinion'] ?? '');

        // Kiểm tra logic NĐ 90 & 48 qua Service
        $rankCheck = Decree90Service::determineRank($dutyScore, $politicalScore, (bool)$hasDiscipline, (bool)$hasInitiative);

        if ($hasDiscipline && str_contains($finalRank, 'xuất sắc')) {
            $_SESSION['error'] = 'Vi phạm quy định: Cán bộ bị kỷ luật không được xếp loại Hoàn thành xuất sắc nhiệm vụ (BR-07).';
            header('Location: /civil-servant');
            exit;
        }

        CivilServant::saveClassification([
            'evaluation_id' => $evalId,
            'civil_servant_id' => $civilServantId,
            'political_ethics_score' => $politicalScore,
            'work_discipline_score' => $disciplineScore,
            'duty_execution_score' => $dutyScore,
            'has_disciplinary_action' => $hasDiscipline,
            'has_approved_initiative' => $hasInitiative,
            'collective_comments' => $comments,
            'party_opinion' => $partyOpinion,
            'proposed_rank' => $proposedRank,
            'finalized_rank' => $finalRank
        ]);

        AuditService::log($_SESSION['user_id'], 'UPDATE_CIVIL_SERVANT_RANK', 'civil_servant_classifications', $evalId, "Cập nhật xếp loại CBCC: $finalRank");

        $_SESSION['flash_message'] = 'Cập nhật phân loại đánh giá cán bộ công chức theo Nghị định 90 & 48 thành công!';
        header('Location: /civil-servant');
        exit;
    }
}
