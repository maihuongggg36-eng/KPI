<?php
namespace App\Controllers;

use App\Models\Database;
use App\Models\Evaluation;
use App\Models\KpiCatalog;

class ReportController {
    public function index(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026', 'year' => 2026];
        $stats = Evaluation::getDashboardStats($period['id']);

        $db = Database::getConnection();
        
        // Lấy danh sách kết quả chi tiết kèm phòng ban
        $sql = "
            SELECT 
                u.id as user_id,
                u.full_name,
                u.username,
                d.name as department_name,
                p.name as position_name,
                ka.title as kpi_title,
                e.id as eval_id,
                e.self_total_score,
                e.manager_total_score,
                e.final_score,
                e.final_classification,
                e.status as eval_status,
                csc.has_disciplinary_action,
                csc.has_approved_initiative
            FROM users u
            JOIN departments d ON u.department_id = d.id
            JOIN positions p ON u.position_id = p.id
            LEFT JOIN kpi_assignments ka ON ka.employee_id = u.id AND ka.period_id = :period_id
            LEFT JOIN evaluations e ON e.assignment_id = ka.id
            LEFT JOIN civil_servant_classifications csc ON csc.evaluation_id = e.id
            ORDER BY d.id ASC, e.final_score DESC
        ";
        $stmt = $db->prepare($sql);
        $stmt->execute(['period_id' => $period['id']]);
        $rows = $stmt->fetchAll();

        // Thống kê theo phòng ban
        $deptStats = [];
        foreach ($rows as $r) {
            $dName = $r['department_name'];
            if (!isset($deptStats[$dName])) {
                $deptStats[$dName] = [
                    'total' => 0,
                    'evaluated' => 0,
                    'total_score' => 0,
                    'excellent' => 0,
                    'good' => 0,
                    'satisfactory' => 0,
                    'unsatisfactory' => 0
                ];
            }
            $deptStats[$dName]['total']++;
            if (!empty($r['final_score'])) {
                $deptStats[$dName]['evaluated']++;
                $deptStats[$dName]['total_score'] += $r['final_score'];
                $rank = $r['final_classification'] ?? '';
                if (str_contains($rank, 'xuất sắc') || str_contains($rank, 'Xuất sắc')) {
                    $deptStats[$dName]['excellent']++;
                } elseif (str_contains($rank, 'tốt') || str_contains($rank, 'Tốt')) {
                    $deptStats[$dName]['good']++;
                } elseif (str_contains($rank, 'Không hoàn thành') || str_contains($rank, 'không hoàn thành')) {
                    $deptStats[$dName]['unsatisfactory']++;
                } else {
                    $deptStats[$dName]['satisfactory']++;
                }
            }
        }

        require BASE_PATH . '/app/Views/reports/summary.php';
    }

    public function exportCsv(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026', 'year' => 2026];
        $db = Database::getConnection();
        
        $sql = "
            SELECT 
                u.full_name,
                u.username,
                d.name as department_name,
                p.name as position_name,
                e.self_total_score,
                e.manager_total_score,
                e.final_score,
                e.final_classification,
                e.status as eval_status
            FROM users u
            JOIN departments d ON u.department_id = d.id
            JOIN positions p ON u.position_id = p.id
            LEFT JOIN kpi_assignments ka ON ka.employee_id = u.id AND ka.period_id = :period_id
            LEFT JOIN evaluations e ON e.assignment_id = ka.id
            ORDER BY d.id ASC, e.final_score DESC
        ";
        $stmt = $db->prepare($sql);
        $stmt->execute(['period_id' => $period['id']]);
        $rows = $stmt->fetchAll();

        header('Content-Type: text/csv; charset=utf-8');
        header('Content-Disposition: attachment; filename=Bao_Cao_Xep_Loai_KPI_' . date('Ymd_His') . '.csv');

        $out = fopen('php://output', 'w');
        // UTF-8 BOM cho Excel tiếng Việt không bị lỗi font
        fprintf($out, chr(0xEF).chr(0xBB).chr(0xBF));

        fputcsv($out, ['STT', 'Họ và tên', 'Mã tài khoản', 'Phòng ban', 'Chức vụ', 'Điểm tự đánh giá', 'Điểm quản lý chấm', 'Điểm phê chuẩn', 'Mức xếp loại', 'Trạng thái']);

        $stt = 1;
        foreach ($rows as $r) {
            fputcsv($out, [
                $stt++,
                $r['full_name'],
                $r['username'],
                $r['department_name'],
                $r['position_name'],
                $r['self_total_score'] ?? '0',
                $r['manager_total_score'] ?? '0',
                $r['final_score'] ?? '0',
                $r['final_classification'] ?? 'Chưa đánh giá',
                $r['eval_status'] ?? 'Chưa tạo'
            ]);
        }

        fclose($out);
        exit;
    }
}
