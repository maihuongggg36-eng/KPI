<?php
namespace App\Controllers;

use App\Models\Database;
use App\Models\KpiCatalog;
use App\Models\Payroll;
use App\Models\User;
use App\Services\AuditService;
use App\Services\PayrollService;
use PDO;

class PayrollController {
    /**
     * Bảng tổng hợp tính lương theo KPI và Quản lý chi trả tiền mặt
     */
    public function index(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $userRole = $_SESSION['role'] ?? 'Employee';
        // Nhân viên thông thường chỉ xem phiếu lương của mình
        if ($userRole === 'Employee') {
            header('Location: /payroll/my-payslip');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026', 'year' => 2026];
        $periodId = intval($_GET['period_id'] ?? $period['id']);
        $deptId = !empty($_GET['department_id']) ? intval($_GET['department_id']) : null;

        $db = Database::getConnection();
        $departments = $db->query("SELECT id, name FROM departments ORDER BY id ASC")->fetchAll(PDO::FETCH_ASSOC);

        // Lấy danh sách bảng lương
        $payrolls = Payroll::getAllByPeriod($periodId, $deptId);
        $stats = Payroll::getStats($periodId);

        // Lấy danh sách kỳ đánh giá
        $periods = $db->query("SELECT id, name, year, status FROM evaluation_periods ORDER BY id DESC")->fetchAll(PDO::FETCH_ASSOC);

        require BASE_PATH . '/app/Views/payroll/index.php';
    }

    /**
     * Kế toán tính lương tự động từ kết quả KPI
     */
    public function calculate(): void {
        if (empty($_SESSION['user_id']) || !in_array($_SESSION['role'], ['Accountant', 'Director', 'Super Admin'])) {
            $_SESSION['error'] = 'Bạn không có quyền thực hiện tính toán lương.';
            header('Location: /dashboard');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1];
        $periodId = intval($_POST['period_id'] ?? $period['id']);

        try {
            $createdIds = PayrollService::calculateAndSaveAll($periodId, $_SESSION['user_id']);
            $count = count($createdIds);

            AuditService::log(
                $_SESSION['user_id'],
                'CALCULATE_PAYROLL',
                'payrolls',
                $periodId,
                "Kế toán đã tính toán lương gắn với KPI cho $count nhân sự. Hình thức: Tiền mặt."
            );

            $_SESSION['flash_message'] = "Đã tính lương thành công cho <strong>$count</strong> cán bộ, nhân viên dựa trên điểm KPI! Tất cả các khoản lương đã sẵn sàng lập phiếu chi tiền mặt.";
        } catch (\Exception $e) {
            $_SESSION['error'] = 'Lỗi trong quá trình tính lương: ' . $e->getMessage();
        }

        header('Location: /payroll?period_id=' . $periodId);
        exit;
    }

    /**
     * Kế toán cập nhật điều chỉnh một phiếu lương
     */
    public function update(): void {
        if (empty($_SESSION['user_id']) || !in_array($_SESSION['role'], ['Accountant', 'Director', 'Super Admin'])) {
            $_SESSION['error'] = 'Bạn không có quyền cập nhật bảng lương.';
            header('Location: /payroll');
            exit;
        }

        $payrollId = intval($_POST['payroll_id'] ?? 0);
        $payroll = Payroll::getById($payrollId);
        if (!$payroll) {
            $_SESSION['error'] = 'Không tìm thấy phiếu lương.';
            header('Location: /payroll');
            exit;
        }

        $data = [
            'base_salary' => floatval($_POST['base_salary'] ?? $payroll['base_salary']),
            'kpi_bonus' => floatval($_POST['kpi_bonus'] ?? $payroll['kpi_bonus']),
            'allowances' => floatval($_POST['allowances'] ?? $payroll['allowances']),
            'deductions' => floatval($_POST['deductions'] ?? $payroll['deductions']),
            'notes' => trim($_POST['notes'] ?? $payroll['notes']),
        ];

        Payroll::update($payrollId, $data);
        AuditService::log($_SESSION['user_id'], 'UPDATE_PAYROLL', 'payrolls', $payrollId, "Cập nhật phiếu lương nhân viên {$payroll['employee_name']}");

        $_SESSION['flash_message'] = "Đã cập nhật phiếu lương cho cán bộ <strong>{$payroll['employee_name']}</strong>.";
        header('Location: /payroll?period_id=' . $payroll['period_id']);
        exit;
    }

    /**
     * Xác nhận đã chi trả tiền mặt cho 1 nhân viên
     */
    public function payCash(): void {
        if (empty($_SESSION['user_id']) || !in_array($_SESSION['role'], ['Accountant', 'Director', 'Super Admin'])) {
            $_SESSION['error'] = 'Bạn không có quyền thực hiện xác nhận chi tiền mặt.';
            header('Location: /payroll');
            exit;
        }

        $payrollId = intval($_POST['payroll_id'] ?? $_GET['id'] ?? 0);
        $payroll = Payroll::getById($payrollId);

        if (!$payroll) {
            $_SESSION['error'] = 'Không tìm thấy phiếu lương.';
            header('Location: /payroll');
            exit;
        }

        $signature = trim($_POST['recipient_signature'] ?? "Ký nhận tiền mặt bởi {$payroll['employee_name']}");
        Payroll::markCashPaid($payrollId, $_SESSION['user_id'], $signature);

        AuditService::log($_SESSION['user_id'], 'PAY_CASH', 'payrolls', $payrollId, "Đã chi tiền mặt " . number_format($payroll['net_salary']) . " VNĐ cho {$payroll['employee_name']}");

        $_SESSION['flash_message'] = "Đã xác nhận chi tiền mặt số tiền <strong>" . number_format($payroll['net_salary']) . " VNĐ</strong> cho <strong>{$payroll['employee_name']}</strong> tại quỹ.";
        header('Location: /payroll?period_id=' . $payroll['period_id']);
        exit;
    }

    /**
     * Xác nhận chi trả tiền mặt cho toàn bộ nhân viên
     */
    public function payAllCash(): void {
        if (empty($_SESSION['user_id']) || !in_array($_SESSION['role'], ['Accountant', 'Director', 'Super Admin'])) {
            $_SESSION['error'] = 'Bạn không có quyền thực hiện thao tác này.';
            header('Location: /payroll');
            exit;
        }

        $periodId = intval($_POST['period_id'] ?? 1);
        $count = Payroll::markAllCashPaid($periodId, $_SESSION['user_id']);

        AuditService::log($_SESSION['user_id'], 'PAY_ALL_CASH', 'payrolls', $periodId, "Đã hoàn tất chi trả tiền mặt hàng loạt cho $count nhân viên.");

        $_SESSION['flash_message'] = "Đã hoàn tất chi trả lương tiền mặt cho <strong>$count</strong> cán bộ, nhân viên.";
        header('Location: /payroll?period_id=' . $periodId);
        exit;
    }

    /**
     * Xem & In Phiếu chi tiền mặt cá nhân (Chuẩn mẫu kế toán 02-TT)
     */
    public function voucher(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $payrollId = intval($_GET['id'] ?? 0);
        $payroll = Payroll::getById($payrollId);

        if (!$payroll) {
            $_SESSION['error'] = 'Không tìm thấy thông tin phiếu chi.';
            header('Location: /payroll');
            exit;
        }

        // Bảo mật: Nhân viên chỉ được xem phiếu của mình
        if ($_SESSION['role'] === 'Employee' && $payroll['employee_id'] != $_SESSION['user_id']) {
            $_SESSION['error'] = 'Bạn không có quyền truy cập phiếu chi của người khác.';
            header('Location: /payroll/my-payslip');
            exit;
        }

        $amountInWords = PayrollService::vietnameseNumberToWords($payroll['net_salary']);

        require BASE_PATH . '/app/Views/payroll/voucher.php';
    }

    /**
     * In Danh sách Ký nhận tiền mặt của toàn bộ cơ quan (Bảng thanh toán tiền lương bằng tiền mặt)
     */
    public function cashSheet(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026', 'year' => 2026];
        $periodId = intval($_GET['period_id'] ?? $period['id']);
        $deptId = !empty($_GET['department_id']) ? intval($_GET['department_id']) : null;

        $payrolls = Payroll::getAllByPeriod($periodId, $deptId);
        $stats = Payroll::getStats($periodId);
        $totalInWords = PayrollService::vietnameseNumberToWords($stats['total_net_cash']);

        require BASE_PATH . '/app/Views/payroll/cash_sheet.php';
    }

    /**
     * Phiếu lương cá nhân cho nhân viên đang đăng nhập
     */
    public function myPayslip(): void {
        if (empty($_SESSION['user_id'])) {
            header('Location: /login');
            exit;
        }

        $period = KpiCatalog::getActivePeriod() ?? ['id' => 1, 'name' => 'Kỳ đánh giá Quý 1/2026', 'year' => 2026];
        $payroll = Payroll::getByPeriodAndEmployee($period['id'], $_SESSION['user_id']);
        $amountInWords = $payroll ? PayrollService::vietnameseNumberToWords($payroll['net_salary']) : '';

        require BASE_PATH . '/app/Views/payroll/my_payslip.php';
    }
}
