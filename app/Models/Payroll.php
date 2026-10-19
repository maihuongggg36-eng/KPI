<?php
namespace App\Models;

use PDO;

class Payroll {
    public static function getAllByPeriod(int $periodId, ?int $deptId = null): array {
        $db = Database::getConnection();
        $query = "
            SELECT 
                pr.*,
                u.full_name as employee_name,
                u.username,
                u.email,
                d.name as department_name,
                d.id as department_id,
                pos.name as position_name,
                pos.code as position_code,
                e.final_classification,
                e.status as evaluation_status,
                cashier.full_name as cashier_name
            FROM payrolls pr
            JOIN users u ON pr.employee_id = u.id
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN positions pos ON u.position_id = pos.id
            LEFT JOIN evaluations e ON pr.evaluation_id = e.id
            LEFT JOIN users cashier ON pr.cashier_id = cashier.id
            WHERE pr.period_id = ?
        ";
        $params = [$periodId];

        if ($deptId) {
            $query .= " AND u.department_id = ?";
            $params[] = $deptId;
        }

        $query .= " ORDER BY d.id ASC, pr.net_salary DESC";

        $stmt = $db->prepare($query);
        $stmt->execute($params);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    public static function getById(int $id): ?array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT 
                pr.*,
                u.full_name as employee_name,
                u.username,
                u.email,
                u.is_civil_servant,
                d.name as department_name,
                pos.name as position_name,
                e.final_classification,
                e.self_total_score,
                e.manager_total_score,
                e.final_score,
                ep.name as period_name,
                ep.year as period_year,
                cashier.full_name as cashier_name
            FROM payrolls pr
            JOIN users u ON pr.employee_id = u.id
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN positions pos ON u.position_id = pos.id
            LEFT JOIN evaluations e ON pr.evaluation_id = e.id
            LEFT JOIN evaluation_periods ep ON pr.period_id = ep.id
            LEFT JOIN users cashier ON pr.cashier_id = cashier.id
            WHERE pr.id = ?
        ");
        $stmt->execute([$id]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);
        return $row ?: null;
    }

    public static function getByPeriodAndEmployee(int $periodId, int $employeeId): ?array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT 
                pr.*,
                u.full_name as employee_name,
                u.username,
                d.name as department_name,
                pos.name as position_name,
                e.final_classification,
                ep.name as period_name,
                cashier.full_name as cashier_name
            FROM payrolls pr
            JOIN users u ON pr.employee_id = u.id
            LEFT JOIN departments d ON u.department_id = d.id
            LEFT JOIN positions pos ON u.position_id = pos.id
            LEFT JOIN evaluations e ON pr.evaluation_id = e.id
            LEFT JOIN evaluation_periods ep ON pr.period_id = ep.id
            LEFT JOIN users cashier ON pr.cashier_id = cashier.id
            WHERE pr.period_id = ? AND pr.employee_id = ?
        ");
        $stmt->execute([$periodId, $employeeId]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);
        return $row ?: null;
    }

    public static function update(int $id, array $data): bool {
        $db = Database::getConnection();
        $netSalary = round(
            floatval($data['base_salary']) + 
            floatval($data['kpi_bonus']) + 
            floatval($data['allowances']) - 
            floatval($data['deductions'])
        );
        if ($netSalary < 0) $netSalary = 0;

        $stmt = $db->prepare("
            UPDATE payrolls SET
                base_salary = ?,
                kpi_bonus = ?,
                allowances = ?,
                deductions = ?,
                net_salary = ?,
                notes = ?,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = ?
        ");
        return $stmt->execute([
            $data['base_salary'],
            $data['kpi_bonus'],
            $data['allowances'],
            $data['deductions'],
            $netSalary,
            $data['notes'] ?? '',
            $id
        ]);
    }

    public static function markCashPaid(int $id, int $cashierId, string $signature = ''): bool {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            UPDATE payrolls SET
                payment_status = 'Đã chi tiền mặt',
                paid_at = CURRENT_TIMESTAMP,
                cashier_id = ?,
                recipient_signature = ?,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = ?
        ");
        return $stmt->execute([$cashierId, $signature, $id]);
    }

    public static function markAllCashPaid(int $periodId, int $cashierId): int {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            UPDATE payrolls SET
                payment_status = 'Đã chi tiền mặt',
                paid_at = CURRENT_TIMESTAMP,
                cashier_id = ?,
                recipient_signature = 'Đã nhận đủ tiền mặt tại quỹ',
                updated_at = CURRENT_TIMESTAMP
            WHERE period_id = ? AND payment_status != 'Đã chi tiền mặt'
        ");
        $stmt->execute([$cashierId, $periodId]);
        return $stmt->rowCount();
    }

    public static function getStats(int $periodId): array {
        $db = Database::getConnection();
        $stmt = $db->prepare("
            SELECT 
                COUNT(*) as total_employees,
                COALESCE(SUM(base_salary), 0) as total_base_salary,
                COALESCE(SUM(kpi_bonus), 0) as total_kpi_bonus,
                COALESCE(SUM(allowances), 0) as total_allowances,
                COALESCE(SUM(deductions), 0) as total_deductions,
                COALESCE(SUM(net_salary), 0) as total_net_cash,
                COALESCE(SUM(CASE WHEN payment_status = 'Đã chi tiền mặt' THEN net_salary ELSE 0 END), 0) as total_cash_paid,
                COALESCE(SUM(CASE WHEN payment_status != 'Đã chi tiền mặt' THEN net_salary ELSE 0 END), 0) as total_cash_pending,
                COUNT(CASE WHEN payment_status = 'Đã chi tiền mặt' THEN 1 END) as count_paid,
                COUNT(CASE WHEN payment_status != 'Đã chi tiền mặt' THEN 1 END) as count_pending
            FROM payrolls
            WHERE period_id = ?
        ");
        $stmt->execute([$periodId]);
        return $stmt->fetch(PDO::FETCH_ASSOC) ?: [
            'total_employees' => 0,
            'total_base_salary' => 0,
            'total_kpi_bonus' => 0,
            'total_allowances' => 0,
            'total_deductions' => 0,
            'total_net_cash' => 0,
            'total_cash_paid' => 0,
            'total_cash_pending' => 0,
            'count_paid' => 0,
            'count_pending' => 0
        ];
    }
}
