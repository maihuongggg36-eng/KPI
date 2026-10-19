<?php
namespace App\Models;

use PDO;

class CivilServant {
    public static function getAllWithEvaluation(int $periodId): array {
        $db = Database::getConnection();
        $sql = "
            SELECT 
                u.id as user_id,
                u.full_name,
                u.username,
                u.email,
                d.name as department_name,
                p.name as position_name,
                p.is_leadership,
                e.id as evaluation_id,
                e.self_total_score,
                e.manager_total_score,
                e.final_score,
                e.final_classification,
                e.status as eval_status,
                csc.id as classification_id,
                csc.political_ethics_score,
                csc.work_discipline_score,
                csc.duty_execution_score,
                csc.has_disciplinary_action,
                csc.has_approved_initiative,
                csc.proposed_rank,
                csc.finalized_rank,
                csc.collective_comments,
                csc.party_opinion
            FROM users u
            JOIN departments d ON u.department_id = d.id
            JOIN positions p ON u.position_id = p.id
            LEFT JOIN evaluations e ON e.employee_id = u.id AND e.period_id = :period_id
            LEFT JOIN civil_servant_classifications csc ON csc.evaluation_id = e.id
            WHERE u.is_civil_servant = 1
            ORDER BY p.is_leadership DESC, d.id ASC, u.id ASC
        ";
        $stmt = $db->prepare($sql);
        $stmt->execute(['period_id' => $periodId]);
        return $stmt->fetchAll();
    }

    public static function getDetailForForm(int $userId, int $periodId): ?array {
        $db = Database::getConnection();
        $sql = "
            SELECT 
                u.id as user_id,
                u.full_name,
                u.username,
                u.email,
                d.name as department_name,
                p.name as position_name,
                p.is_leadership,
                o.name as org_name,
                ep.name as period_name,
                ep.year,
                e.id as evaluation_id,
                e.self_total_score,
                e.manager_total_score,
                e.final_score,
                e.final_classification,
                csc.political_ethics_score,
                csc.work_discipline_score,
                csc.duty_execution_score,
                csc.has_disciplinary_action,
                csc.has_approved_initiative,
                csc.proposed_rank,
                csc.finalized_rank,
                csc.collective_comments,
                csc.party_opinion
            FROM users u
            JOIN departments d ON u.department_id = d.id
            JOIN positions p ON u.position_id = p.id
            JOIN organizations o ON d.org_id = o.id
            CROSS JOIN evaluation_periods ep ON ep.id = :period_id
            LEFT JOIN evaluations e ON e.employee_id = u.id AND e.period_id = ep.id
            LEFT JOIN civil_servant_classifications csc ON csc.evaluation_id = e.id
            WHERE u.id = :user_id
        ";
        $stmt = $db->prepare($sql);
        $stmt->execute(['user_id' => $userId, 'period_id' => $periodId]);
        $row = $stmt->fetch();
        return $row ?: null;
    }

    public static function saveClassification(array $data): void {
        $db = Database::getConnection();
        $sql = "
            INSERT INTO civil_servant_classifications 
            (evaluation_id, civil_servant_id, political_ethics_score, work_discipline_score, duty_execution_score, has_disciplinary_action, has_approved_initiative, collective_comments, party_opinion, proposed_rank, finalized_rank)
            VALUES 
            (:evaluation_id, :civil_servant_id, :political_ethics_score, :work_discipline_score, :duty_execution_score, :has_disciplinary_action, :has_approved_initiative, :collective_comments, :party_opinion, :proposed_rank, :finalized_rank)
            ON CONFLICT(evaluation_id) DO UPDATE SET
            political_ethics_score = excluded.political_ethics_score,
            work_discipline_score = excluded.work_discipline_score,
            duty_execution_score = excluded.duty_execution_score,
            has_disciplinary_action = excluded.has_disciplinary_action,
            has_approved_initiative = excluded.has_approved_initiative,
            collective_comments = excluded.collective_comments,
            party_opinion = excluded.party_opinion,
            proposed_rank = excluded.proposed_rank,
            finalized_rank = excluded.finalized_rank
        ";
        $stmt = $db->prepare($sql);
        $stmt->execute([
            'evaluation_id' => $data['evaluation_id'],
            'civil_servant_id' => $data['civil_servant_id'],
            'political_ethics_score' => $data['political_ethics_score'] ?? 90,
            'work_discipline_score' => $data['work_discipline_score'] ?? 90,
            'duty_execution_score' => $data['duty_execution_score'] ?? 90,
            'has_disciplinary_action' => $data['has_disciplinary_action'] ?? 0,
            'has_approved_initiative' => $data['has_approved_initiative'] ?? 0,
            'collective_comments' => $data['collective_comments'] ?? '',
            'party_opinion' => $data['party_opinion'] ?? '',
            'proposed_rank' => $data['proposed_rank'] ?? '',
            'finalized_rank' => $data['finalized_rank'] ?? ''
        ]);
    }
}
