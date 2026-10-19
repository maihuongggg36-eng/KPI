<?php
namespace App\Controllers;

use App\Models\KpiAssignment;
use App\Models\KpiCatalog;
use App\Models\User;
use App\Services\KpiCalculationService;
use App\Services\AuditService;

class KpiPlanningController {
    public function myKpi(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $userId = $_SESSION['user_id'];
        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026'];
        $assignment = KpiAssignment::getByPeriodAndEmployee($period['id'], $userId);

        require BASE_PATH . '/app/Views/kpi/my_kpi.php';
    }

    public function listAssignments(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026'];
        $assignments = KpiAssignment::getAllByPeriod($period['id']);

        require BASE_PATH . '/app/Views/kpi/list.php';
    }

    public function showAssignForm(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026'];
        $deptId = $_SESSION['department_id'] ?? 3;
        $employees = User::getEmployeesByDepartment($deptId);
        $masterKpis = KpiCatalog::getMasterItems();

        require BASE_PATH . '/app/Views/kpi/assign.php';
    }

    public function storeAssign(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $employeeId = intval($_POST['employee_id'] ?? 0);
        $periodId = intval($_POST['period_id'] ?? 1);
        $title = trim($_POST['title'] ?? 'Bản giao KPI');
        $kpiItems = $_POST['kpis'] ?? [];

        if (empty($employeeId) || empty($kpiItems)) {
            $_SESSION['error'] = 'Vui lòng chọn nhân viên và cấu hình ít nhất 01 chỉ tiêu KPI.';
            header('Location: /kpi/assign');
            exit;
        }

        // Kiểm tra trọng số qua Service (BR-01 & BR-02)
        $valResult = KpiCalculationService::validateWeights($kpiItems);
        if (!$valResult['isValid']) {
            $_SESSION['error'] = implode('<br>', $valResult['errors']);
            header('Location: /kpi/assign');
            exit;
        }

        try {
            $data = [
                'period_id' => $periodId,
                'employee_id' => $employeeId,
                'approver_id' => $_SESSION['user_id'],
                'title' => $title,
                'total_weight' => $valResult['totalWeight'],
                'status' => 'Approved_Locked'
            ];

            $assignmentId = KpiAssignment::create($data, $kpiItems);
            AuditService::log($_SESSION['user_id'], 'CREATE_ASSIGNMENT', 'kpi_assignments', $assignmentId, "Giao KPI cho nhân viên ID: $employeeId");

            $_SESSION['flash_message'] = 'Giao KPI thành công! Kế hoạch đã được khóa và kích hoạt sẵn sàng cho kỳ đánh giá.';
            header('Location: /kpi/assignments');
            exit;
        } catch (\Exception $e) {
            $_SESSION['error'] = 'Lỗi hệ thống: ' . $e->getMessage();
            header('Location: /kpi/assign');
            exit;
        }
    }

    public function viewKpi(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $assignmentId = intval($_GET['id'] ?? $_GET['assignment_id'] ?? 0);
        $assignment = KpiAssignment::getById($assignmentId);

        if (!$assignment) {
            $_SESSION['error'] = 'Không tìm thấy bản giao KPI.';
            header('Location: /kpi/assignments');
            exit;
        }

        $evaluation = \App\Models\Evaluation::getByAssignmentId($assignmentId);
        $period = KpiCatalog::getActivePeriod() ?? ['id' => $assignment['period_id'], 'name' => $assignment['period_name']];

        require BASE_PATH . '/app/Views/kpi/view_detail.php';
    }
}

