<?php
namespace App\Models;

use PDO;
use App\Services\KpiCalculationService;

class Evaluation {
    public static function getByAssignmentId(int $assignmentId): ?array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT e.*, a.title as assignment_title, a.total_weight, u.full_name as employee_name,
                   d.name as department_name, pos.name as position_name, p.name as period_name,
                   u.is_civil_servant
            FROM evaluations e
            JOIN kpi_assignments a ON e.assignment_id = a.id
            JOIN users u ON e.employee_id = u.id
            JOIN evaluation_periods p ON e.period_id = p.id
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN positions pos ON u.position_id = pos.id
            WHERE e.assignment_id = ?
        ");
        $stmt->execute([$assignmentId]);
        $eval = $stmt->fetch();
        if ($eval) {
            $stmtDet = $db->prepare("
                SELECT ad.id, ad.id as assignment_detail_id, ad.kpi_name, ad.unit, ad.target_value, ad.weight, ad.max_cap, ad.scoring_formula,
                       ed.id as eval_detail_id, ed.actual_value, ed.completion_rate, ed.self_score, ed.manager_score, ed.score_gap,
                       ed.self_remarks, ed.manager_remarks, ed.score_gap_reason
                FROM kpi_assignment_details ad
                LEFT JOIN evaluation_details ed ON ed.assignment_detail_id = ad.id AND ed.evaluation_id = ?
                WHERE ad.assignment_id = ?
                ORDER BY ad.id ASC
            ");
            $stmtDet->execute([$eval['id'], $assignmentId]);
            $eval['details'] = $stmtDet->fetchAll();

            // Lấy thông tin công chức nếu có
            if ($eval['is_civil_servant']) {
                $stmtCiv = $db->prepare("SELECT * FROM civil_servant_classifications WHERE evaluation_id = ?");
                $stmtCiv->execute([$eval['id']]);
                $eval['civil_servant'] = $stmtCiv->fetch() ?: null;
            }
        }
        return $eval ?: null;
    }

    public static function saveSelfAssessment(int $evalId, array $items): bool {
        $db = Database::getConnection();
        $db->beginTransaction();
        try {
            $totalSelfScore = 0.0;

            foreach ($items as $k => $item) {
                $detailId = (int)($item['assignment_detail_id'] ?? $item['id'] ?? $k);
                $actual = floatval($item['actual_value'] ?? 0.0);
                $target = floatval($item['target_value'] ?? 100.0);
                $weight = floatval($item['weight'] ?? 0.0);
                $formula = $item['scoring_formula'] ?? 'HigherIsBetter';
                $maxCap = floatval($item['max_cap'] ?? 150.0);
                $remarks = $item['self_remarks'] ?? '';

                $rate = KpiCalculationService::calculateCompletionRate($actual, $target, $formula, $maxCap);
                $selfScore = round(($rate * $weight) / 100.0, 2);
                $totalSelfScore += $selfScore;

                $stmt = $db->prepare("
                    INSERT INTO evaluation_details (evaluation_id, assignment_detail_id, actual_value, completion_rate, self_score, self_remarks)
                    VALUES (?, ?, ?, ?, ?, ?)
                    ON CONFLICT(evaluation_id, assignment_detail_id) DO UPDATE SET
                        actual_value = excluded.actual_value,
                        completion_rate = excluded.completion_rate,
                        self_score = excluded.self_score,
                        self_remarks = excluded.self_remarks
                ");
                $stmt->execute([$evalId, $detailId, $actual, $rate, $selfScore, $remarks]);
            }

            $stmtUpdate = $db->prepare("
                UPDATE evaluations 
                SET self_total_score = ?, status = 'Self_Submitted' 
                WHERE id = ?
            ");
            $stmtUpdate->execute([round($totalSelfScore, 2), $evalId]);

            $db->commit();
            return true;
        } catch (\Exception $e) {
            $db->rollBack();
            throw $e;
        }
    }

    public static function saveManagerReview(int $evalId, array $items, string $managerNote = ''): bool {
        $db = Database::getConnection();
        $db->beginTransaction();
        try {
            $totalManagerScore = 0.0;

            foreach ($items as $k => $item) {
                $detailId = (int)($item['assignment_detail_id'] ?? $item['id'] ?? $k);
                $mgrScore = floatval($item['manager_score'] ?? 0.0);
                $selfScore = floatval($item['self_score'] ?? 0.0);
                $gap = abs($selfScore - $mgrScore);
                $reason = $item['score_gap_reason'] ?? null;
                $remarks = $item['manager_remarks'] ?? '';

                $totalManagerScore += $mgrScore;

                $stmt = $db->prepare("
                    UPDATE evaluation_details 
                    SET manager_score = ?, score_gap = ?, manager_remarks = ?, score_gap_reason = ?
                    WHERE evaluation_id = ? AND assignment_detail_id = ?
                ");
                $stmt->execute([$mgrScore, $gap, $remarks, $reason, $evalId, $detailId]);
            }

            $totalManagerScore = round($totalManagerScore, 2);

            // Xác định sơ bộ xếp loại
            $rank = 'Hoàn thành nhiệm vụ';
            if ($totalManagerScore >= 90.0) $rank = 'Hoàn thành xuất sắc nhiệm vụ';
            elseif ($totalManagerScore >= 75.0) $rank = 'Hoàn thành tốt nhiệm vụ';
            elseif ($totalManagerScore < 50.0) $rank = 'Không hoàn thành nhiệm vụ';

            $stmtUpdate = $db->prepare("
                UPDATE evaluations 
                SET manager_total_score = ?, final_score = ?, final_classification = ?, status = 'Manager_Reviewed' 
                WHERE id = ?
            ");
            $stmtUpdate->execute([$totalManagerScore, $totalManagerScore, $rank, $evalId]);

            $db->commit();
            return true;
        } catch (\Exception $e) {
            $db->rollBack();
            throw $e;
        }
    }

    public static function finalizeApproval(int $evalId, float $finalScore, string $finalRank): bool {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            UPDATE evaluations 
            SET final_score = ?, final_classification = ?, status = 'Final_Approved', is_locked = 1
            WHERE id = ?
        ");
        return $stmt->execute([$finalScore, $finalRank, $evalId]);
    }

    public static function getDashboardStats(int $periodId): array {
        $db = Database::getConnection();
        
        $total = $db->query("SELECT COUNT(*) FROM evaluations WHERE period_id = $periodId")->fetchColumn();
        $approved = $db->query("SELECT COUNT(*) FROM evaluations WHERE period_id = $periodId AND status IN ('Final_Approved', 'Manager_Reviewed')")->fetchColumn();
        $pending = $total - $approved;
        $avgScore = $db->query("SELECT ROUND(AVG(final_score), 2) FROM evaluations WHERE period_id = $periodId AND final_score > 0")->fetchColumn() ?: 0.0;

        // Phân phối Gauss
        $ranks = [
            'Hoàn thành xuất sắc nhiệm vụ' => 0,
            'Hoàn thành tốt nhiệm vụ' => 0,
            'Hoàn thành nhiệm vụ' => 0,
            'Không hoàn thành nhiệm vụ' => 0
        ];
        $rows = $db->query("SELECT final_classification, COUNT(*) as count FROM evaluations WHERE period_id = $periodId AND final_classification IS NOT NULL GROUP BY final_classification")->fetchAll();
        foreach ($rows as $r) {
            if (isset($ranks[$r['final_classification']])) {
                $ranks[$r['final_classification']] = (int)$r['count'];
            }
        }

        return [
            'total' => (int)$total,
            'approved' => (int)$approved,
            'pending' => (int)$pending,
            'avgScore' => (float)$avgScore,
            'ranks' => $ranks
        ];
    }
}
