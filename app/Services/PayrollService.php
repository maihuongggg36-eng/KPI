<?php
namespace App\Services;

use App\Models\Database;
use App\Models\Evaluation;
use App\Models\Payroll;
use App\Models\User;
use PDO;

class PayrollService {
    /**
     * Mức lương cơ bản ngạch bậc mặc định theo mã chức danh (VNĐ)
     */
    const DEFAULT_BASE_SALARIES = [
        'GIAM_DOC' => 16000000,
        'TRUONG_PHONG' => 12500000,
        'KE_TOAN' => 9500000,
        'CHUYEN_VIEN_CC' => 10000000,
        'CHUYEN_VIEN' => 8000000,
        'DEV_LEAD' => 20000000,
        'DEV_SENIOR' => 15000000,
    ];

    /**
     * Mức phụ cấp trách nhiệm/công vụ mặc định theo mã chức danh (VNĐ)
     */
    const DEFAULT_ALLOWANCES = [
        'GIAM_DOC' => 2500000,
        'TRUONG_PHONG' => 1800000,
        'KE_TOAN' => 1200000,
        'CHUYEN_VIEN_CC' => 1200000,
        'CHUYEN_VIEN' => 900000,
        'DEV_LEAD' => 2000000,
        'DEV_SENIOR' => 1200000,
    ];

    /**
     * Tính toán chi tiết lương gắn liền với KPI cho một nhân viên
     *
     * Quy tắc gắn KPI với Lương (P3 - Performance Pay):
     * - KPI >= 90 (Hoàn thành xuất sắc): Hệ số thưởng KPI = 1.25 (+25% lương hiệu quả)
     * - KPI 75 - 89.9 (Hoàn thành tốt): Hệ số thưởng KPI = 1.00 (100% lương hiệu quả)
     * - KPI 50 - 74.9 (Hoàn thành nhiệm vụ): Hệ số thưởng KPI = 0.75 (75% lương hiệu quả)
     * - KPI < 50 (Không hoàn thành): Hệ số thưởng KPI = 0.00 (Không có thưởng KPI)
     *
     * Hình thức thanh toán: 100% Tiền mặt (Cash)
     */
    public static function calculateEmployeeSalary(array $user, ?array $evaluation, array $custom = []): array {
        $posCode = $user['position_code'] ?? '';
        
        // 1. Lương cơ bản (P1)
        $baseSalary = isset($custom['base_salary']) 
            ? floatval($custom['base_salary']) 
            : (self::DEFAULT_BASE_SALARIES[$posCode] ?? 8000000);

        // Hệ số lương công chức (nếu có)
        $coefficient = isset($custom['salary_coefficient']) 
            ? floatval($custom['salary_coefficient']) 
            : ($user['is_civil_servant'] ? 2.34 : 1.0);

        // 2. Điểm KPI và Đánh giá hiệu suất (P3)
        $kpiScore = 0.0;
        $kpiClassification = 'Chưa đánh giá';
        if ($evaluation) {
            $kpiScore = floatval($evaluation['final_score'] ?: ($evaluation['manager_total_score'] ?: $evaluation['self_total_score']));
            $kpiClassification = $evaluation['final_classification'] ?? 'Đang đánh giá';
        }

        // Xác định hệ số thưởng KPI
        $kpiMultiplier = 1.0;
        if ($kpiScore >= 90.0) {
            $kpiMultiplier = 1.25; // Xuất sắc: +25%
        } elseif ($kpiScore >= 75.0) {
            $kpiMultiplier = 1.00; // Tốt: 100%
        } elseif ($kpiScore >= 50.0) {
            $kpiMultiplier = 0.75; // Đạt: 75%
        } else {
            $kpiMultiplier = $kpiScore > 0 ? 0.0 : 1.0; // Không hoàn thành: 0%
        }

        // Định mức lương mềm hiệu quả KPI (40% lương cơ bản)
        $standardKpiBudget = round($baseSalary * 0.40);
        $kpiBonus = isset($custom['kpi_bonus']) 
            ? floatval($custom['kpi_bonus']) 
            : round($standardKpiBudget * $kpiMultiplier);

        // 3. Phụ cấp (P2)
        $allowances = isset($custom['allowances']) 
            ? floatval($custom['allowances']) 
            : (self::DEFAULT_ALLOWANCES[$posCode] ?? 900000);

        // 4. Giảm trừ (BHXH 8%, BHYT 1.5%, BHTN 1% = 10.5% lương cơ sở)
        // Mức lương cơ sở nhà nước 2.340.000 x Hệ số hoặc theo thỏa thuận
        $insurableSalary = $user['is_civil_servant'] ? (2340000 * $coefficient) : ($baseSalary * 0.7);
        $deductions = isset($custom['deductions']) 
            ? floatval($custom['deductions']) 
            : round($insurableSalary * 0.105);

        // 5. Lương thực lĩnh (Net Salary) chi trả bằng TIỀN MẶT
        $netSalary = round($baseSalary + $kpiBonus + $allowances - $deductions);
        if ($netSalary < 0) $netSalary = 0;

        return [
            'employee_id' => $user['id'],
            'evaluation_id' => $evaluation['id'] ?? null,
            'base_salary' => $baseSalary,
            'salary_coefficient' => $coefficient,
            'kpi_score' => $kpiScore,
            'kpi_classification' => $kpiClassification,
            'kpi_multiplier' => $kpiMultiplier,
            'kpi_bonus' => $kpiBonus,
            'allowances' => $allowances,
            'deductions' => $deductions,
            'net_salary' => $netSalary,
            'payment_method' => 'Tiền mặt', // Bắt buộc Tiền mặt
            'payment_status' => $custom['payment_status'] ?? 'Chờ chi tiền mặt',
            'notes' => $custom['notes'] ?? "Thanh toán lương & thưởng KPI bằng tiền mặt tại quỹ."
        ];
    }

    /**
     * Tính toán và lưu bảng lương toàn bộ nhân viên cho một kỳ
     */
    public static function calculateAndSaveAll(int $periodId, int $cashierId): array {
        $db = Database::getConnection();

        // Lấy danh sách toàn bộ cán bộ / nhân viên (trừ Super Admin nếu muốn)
        $stmtUsers = $db->query("
            SELECT u.*, p.code as position_code, p.name as position_name, d.name as department_name
            FROM users u
            LEFT JOIN positions p ON u.position_id = p.id
            LEFT JOIN departments d ON u.department_id = d.id
            WHERE u.status = 'Active' AND u.role != 'Super Admin'
            ORDER BY u.id ASC
        ");
        $users = $stmtUsers->fetchAll(PDO::FETCH_ASSOC);

        $results = [];
        $voucherIndex = 1;

        foreach ($users as $user) {
            // Lấy kết quả đánh giá KPI nếu có
            $stmtEval = $db->prepare("
                SELECT e.* 
                FROM evaluations e
                JOIN kpi_assignments ka ON e.assignment_id = ka.id
                WHERE ka.employee_id = ? AND ka.period_id = ?
            ");
            $stmtEval->execute([$user['id'], $periodId]);
            $evaluation = $stmtEval->fetch(PDO::FETCH_ASSOC) ?: null;

            // Kiểm tra xem đã có bản ghi lương chưa
            $stmtCheck = $db->prepare("SELECT * FROM payrolls WHERE period_id = ? AND employee_id = ?");
            $stmtCheck->execute([$periodId, $user['id']]);
            $existing = $stmtCheck->fetch(PDO::FETCH_ASSOC);

            // Tính lương
            $calc = self::calculateEmployeeSalary($user, $evaluation);

            // Sinh mã phiếu chi tiền mặt
            $voucherCode = sprintf("PC-%04d-%03d", $periodId, $user['id']);

            if ($existing) {
                // Giữ lại trạng thái chi tiền nếu đã chi
                $status = $existing['payment_status'];
                $paidAt = $existing['paid_at'];
                $sig = $existing['recipient_signature'];

                $stmtUpdate = $db->prepare("
                    UPDATE payrolls SET 
                        evaluation_id = ?,
                        base_salary = ?,
                        salary_coefficient = ?,
                        kpi_score = ?,
                        kpi_multiplier = ?,
                        kpi_bonus = ?,
                        allowances = ?,
                        deductions = ?,
                        net_salary = ?,
                        payment_method = 'Tiền mặt',
                        voucher_code = ?,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE id = ?
                ");
                $stmtUpdate->execute([
                    $calc['evaluation_id'],
                    $calc['base_salary'],
                    $calc['salary_coefficient'],
                    $calc['kpi_score'],
                    $calc['kpi_multiplier'],
                    $calc['kpi_bonus'],
                    $calc['allowances'],
                    $calc['deductions'],
                    $calc['net_salary'],
                    $voucherCode,
                    $existing['id']
                ]);
                $results[] = $existing['id'];
            } else {
                $stmtInsert = $db->prepare("
                    INSERT INTO payrolls (
                        period_id, employee_id, evaluation_id, base_salary, salary_coefficient,
                        kpi_score, kpi_multiplier, kpi_bonus, allowances, deductions,
                        net_salary, payment_method, payment_status, voucher_code, notes, cashier_id
                    ) VALUES (
                        ?, ?, ?, ?, ?,
                        ?, ?, ?, ?, ?,
                        ?, 'Tiền mặt', 'Chờ chi tiền mặt', ?, ?, ?
                    )
                ");
                $stmtInsert->execute([
                    $periodId,
                    $user['id'],
                    $calc['evaluation_id'],
                    $calc['base_salary'],
                    $calc['salary_coefficient'],
                    $calc['kpi_score'],
                    $calc['kpi_multiplier'],
                    $calc['kpi_bonus'],
                    $calc['allowances'],
                    $calc['deductions'],
                    $calc['net_salary'],
                    $voucherCode,
                    $calc['notes'],
                    $cashierId
                ]);
                $results[] = $db->lastInsertId();
            }
        }

        return $results;
    }

    /**
     * Chuyển đổi số tiền VNĐ thành chữ tiếng Việt chuẩn (dùng cho Phiếu Chi tiền mặt kế toán)
     */
    public static function vietnameseNumberToWords(float $number): string {
        $number = round($number);
        if ($number <= 0) return 'Không đồng';

        $words = [
            0 => 'không', 1 => 'một', 2 => 'hai', 3 => 'ba', 4 => 'bốn',
            5 => 'năm', 6 => 'sáu', 7 => 'bảy', 8 => 'tám', 9 => 'chín'
        ];
        $units = ['', 'nghìn', 'triệu', 'tỷ', 'nghìn tỷ', 'triệu tỷ'];

        $numStr = (string)$number;
        $len = strlen($numStr);
        $groups = [];

        while ($len > 0) {
            $take = min($len, 3);
            $start = $len - $take;
            $groups[] = substr($numStr, $start, $take);
            $len -= $take;
        }

        $resultGroups = [];
        foreach ($groups as $idx => $group) {
            $groupVal = intval($group);
            if ($groupVal === 0) continue;

            $groupStr = str_pad($group, 3, '0', STR_PAD_LEFT);
            $hundreds = intval($groupStr[0]);
            $tens = intval($groupStr[1]);
            $ones = intval($groupStr[2]);

            $groupWords = [];

            // Hàng trăm
            $groupWords[] = $words[$hundreds] . ' trăm';

            // Hàng chục
            if ($tens === 0) {
                if ($ones > 0) {
                    $groupWords[] = 'lẻ';
                }
            } elseif ($tens === 1) {
                $groupWords[] = 'mười';
            } else {
                $groupWords[] = $words[$tens] . ' mươi';
            }

            // Hàng đơn vị
            if ($ones === 1) {
                if ($tens > 1) {
                    $groupWords[] = 'mốt';
                } else {
                    $groupWords[] = 'một';
                }
            } elseif ($ones === 5) {
                if ($tens > 0) {
                    $groupWords[] = 'lăm';
                } else {
                    $groupWords[] = 'năm';
                }
            } elseif ($ones > 0) {
                $groupWords[] = $words[$ones];
            }

            $unitName = $units[$idx] ?? '';
            $subResult = implode(' ', $groupWords);
            if ($unitName !== '') {
                $subResult .= ' ' . $unitName;
            }
            $resultGroups[] = $subResult;
        }

        $text = implode(' ', array_reverse($resultGroups));
        // Chuẩn hóa
        $text = preg_replace('/\s+/', ' ', trim($text));
        // Viết hoa chữ cái đầu
        $text = mb_strtoupper(mb_substr($text, 0, 1)) . mb_substr($text, 1);

        return $text . ' đồng chẵn.';
    }
}
