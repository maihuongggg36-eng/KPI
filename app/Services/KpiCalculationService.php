<?php
namespace App\Services;

class KpiCalculationService {
    /**
     * BR-10 & BR-11: Tính tỷ lệ % hoàn thành chỉ tiêu
     */
    public static function calculateCompletionRate(float $actual, float $target, string $formula = 'HigherIsBetter', float $maxCap = 150.0): float {
        if ($target <= 0) return 0.0;

        if ($formula === 'HigherIsBetter') {
            // BR-10: Càng cao càng tốt, chặn trần maxCap
            $rate = ($actual / $target) * 100.0;
            return round(min($rate, $maxCap), 2);
        } elseif ($formula === 'LowerIsBetter') {
            // BR-11: Càng thấp càng tốt (chi phí, lỗi, sự cố)
            $rate = (2.0 - ($actual / $target)) * 100.0;
            return round(max($rate, 0.0), 2);
        } else {
            // TargetRange
            $rate = 100.0 - abs($actual - $target) / $target * 100.0;
            return round(max($rate, 0.0), 2);
        }
    }

    /**
     * BR-01 & BR-02: Kiểm tra tính hợp lệ của trọng số bản giao KPI
     */
    public static function validateWeights(array $items): array {
        $totalWeight = 0.0;
        $errors = [];

        foreach ($items as $idx => $item) {
            $w = floatval($item['weight'] ?? 0);
            $totalWeight += $w;

            if ($w < KPI_MIN_WEIGHT || $w > KPI_MAX_WEIGHT) {
                $name = $item['kpi_name'] ?? ("Chỉ tiêu #" . ($idx + 1));
                $errors[] = "Chỉ tiêu '{$name}' có trọng số {$w}%, không nằm trong biên độ quy định [" . KPI_MIN_WEIGHT . "%, " . KPI_MAX_WEIGHT . "%] (BR-02).";
            }
        }

        if (abs($totalWeight - KPI_TOTAL_WEIGHT) > 0.01) {
            $errors[] = "Tổng trọng số toàn bộ các chỉ tiêu phải bằng chính xác " . KPI_TOTAL_WEIGHT . "%. Hiện tại là {$totalWeight}% (BR-01).";
        }

        return [
            'isValid' => empty($errors),
            'totalWeight' => $totalWeight,
            'errors' => $errors
        ];
    }

    /**
     * BR-05: Kiểm tra độ lệch điểm giữa tự chấm và Quản lý chấm
     */
    public static function checkScoreGap(float $selfScore, float $managerScore, ?string $reason): array {
        $gap = abs($selfScore - $managerScore);
        if ($gap > KPI_GAP_REASON_THRESHOLD) {
            if (empty(trim($reason ?? '')) || mb_strlen(trim($reason)) < 50) {
                return [
                    'isValid' => false,
                    'gap' => $gap,
                    'error' => "Độ lệch điểm giữa tự chấm ({$selfScore}) và Quản lý chấm ({$managerScore}) là {$gap} điểm (> 10%). Quản lý bắt buộc phải nhập lý do giải trình tối thiểu 50 ký tự (BR-05)."
                ];
            }
        }
        return ['isValid' => true, 'gap' => $gap, 'error' => null];
    }

    /**
     * BR-20: Tính điểm tổng hợp theo thang điểm 100
     */
    public static function calculateTotalScore(array $details): float {
        $totalScore = 0.0;
        foreach ($details as $d) {
            $score = floatval($d['score'] ?? 0.0);
            $weight = floatval($d['weight'] ?? 0.0);
            $totalScore += ($score * ($weight / 100.0));
        }
        return round($totalScore, 2);
    }
}
