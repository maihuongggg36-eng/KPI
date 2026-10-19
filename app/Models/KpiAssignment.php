<?php
namespace App\Models;

use PDO;

class KpiAssignment {
    public static function getById(int $id): ?array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT a.*, u.full_name as employee_name, p.name as period_name, m.full_name as approver_name
            FROM kpi_assignments a
            JOIN users u ON a.employee_id = u.id
            JOIN evaluation_periods p ON a.period_id = p.id
            LEFT JOIN users m ON a.approver_id = m.id
            WHERE a.id = ?
        ");
        $stmt->execute([$id]);
        $assign = $stmt->fetch();
        if ($assign) {
            $stmtDet = $db->prepare("SELECT * FROM kpi_assignment_details WHERE assignment_id = ? ORDER BY id ASC");
            $stmtDet->execute([$id]);
            $assign['details'] = $stmtDet->fetchAll();
        }
        return $assign ?: null;
    }

    public static function getByPeriodAndEmployee(int $periodId, int $employeeId): ?array {
        $db = Database::getConnection();
        $stmt = $db->prepare("SELECT id FROM kpi_assignments WHERE period_id = ? AND employee_id = ?");
        $stmt->execute([$periodId, $employeeId]);
        $row = $stmt->fetch();
        return $row ? self::getById($row['id']) : null;
    }

    public static function getAllByPeriod(int $periodId): array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT a.*, 
                   u.full_name as employee_name, 
                   u.username as employee_code,
                   d.name as department_name, 
                   pos.name as position_name,
                   e.id as eval_id,
                   e.self_total_score,
                   e.manager_total_score,
                   e.final_score,
                   e.final_classification,
                   COALESCE(e.status, 'Chưa đánh giá') as eval_status
            FROM kpi_assignments a
            JOIN users u ON a.employee_id = u.id
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN positions pos ON u.position_id = pos.id
            LEFT JOIN evaluations e ON e.assignment_id = a.id
            WHERE a.period_id = ?
            ORDER BY a.id DESC
        ");
        $stmt->execute([$periodId]);
        return $stmt->fetchAll();
    }

    public static function create(array $data, array $details): int {
        $db = Database::getConnection();
        $db->beginTransaction();
        try {
            $stmt = $db->prepare("
                INSERT INTO kpi_assignments (period_id, employee_id, approver_id, title, total_weight, status, approved_at)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            ");
            $status = $data['status'] ?? 'Pending_Staff_Confirm';
            $approvedAt = ($status === 'Approved_Locked') ? date('Y-m-d H:i:s') : null;
            $stmt->execute([
                $data['period_id'],
                $data['employee_id'],
                $data['approver_id'],
                $data['title'],
                $data['total_weight'] ?? 100.0,
                $status,
                $approvedAt
            ]);
            $assignmentId = (int)$db->lastInsertId();

            $stmtDetail = $db->prepare("
                INSERT INTO kpi_assignment_details (assignment_id, kpi_item_id, kpi_name, unit, target_value, min_threshold, max_cap, weight, scoring_formula)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ");
            foreach ($details as $d) {
                $stmtDetail->execute([
                    $assignmentId,
                    $d['kpi_item_id'],
                    $d['kpi_name'],
                    $d['unit'],
                    $d['target_value'],
                    $d['min_threshold'] ?? 0.0,
                    $d['max_cap'] ?? 150.0,
                    $d['weight'],
                    $d['scoring_formula'] ?? 'HigherIsBetter'
                ]);
            }

            // Đồng thời tạo sẵn khung bản ghi Evaluation ở trạng thái 'Not_Started'
            $stmtEval = $db->prepare("
                INSERT INTO evaluations (assignment_id, employee_id, period_id, status)
                VALUES (?, ?, ?, 'Not_Started')
            ");
            $stmtEval->execute([$assignmentId, $data['employee_id'], $data['period_id']]);

            $db->commit();
            return $assignmentId;
        } catch (\Exception $e) {
            $db->rollBack();
            throw $e;
        }
    }
}
