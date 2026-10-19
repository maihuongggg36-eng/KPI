<?php
namespace App\Services;

class Decree90Service {
    /**
     * BR-06: Kiểm tra tỷ lệ khống chế Xuất sắc <= 20% số Hoàn thành tốt theo Nghị định 90/2020/NĐ-CP
     */
    public static function validateExcellenceCap(int $countGood, int $countExcellent): array {
        $maxAllowed = (int)floor($countGood * DECREE_90_EXCELLENCE_CAP);
        $isValid = ($countExcellent <= $maxAllowed);

        return [
            'isValid' => $isValid,
            'countGood' => $countGood,
            'countExcellent' => $countExcellent,
            'maxAllowed' => $maxAllowed,
            'rate' => $countGood > 0 ? round(($countExcellent / $countGood) * 100, 1) : 0,
            'message' => $isValid 
                ? "Tỷ lệ Hoàn thành xuất sắc ({$countExcellent}/{$countGood}) hợp lệ theo Nghị định 90 (Tối đa cho phép: {$maxAllowed} cán bộ)."
                : "Vi phạm Nghị định 90/2020/NĐ-CP (BR-06): Số cán bộ Hoàn thành xuất sắc ({$countExcellent}) vượt quá 20% số cán bộ Hoàn thành tốt ({$countGood}). Số lượng tối đa cho phép là {$maxAllowed} cán bộ. Vui lòng điều chỉnh lại danh sách!"
        ];
    }

    /**
     * Xác định mức xếp loại cán bộ công chức theo Nghị định 90/2020 & 48/2023
     */
    public static function determineRank(float $kpiScore, int $ethicsScore, bool $hasDiscipline, bool $hasInitiative): array {
        // BR-07: Điểm liệt kỷ luật
        if ($hasDiscipline) {
            return [
                'rank' => 'Không hoàn thành nhiệm vụ',
                'code' => 'UNSATISFACTORY',
                'reason' => 'Bị xử lý kỷ luật Đảng hoặc chính quyền trong năm (Điểm liệt - BR-07).'
            ];
        }

        $totalAverage = ($kpiScore * 0.7) + ($ethicsScore * 0.3);

        if ($totalAverage >= 90.0) {
            // BR-08: Điều kiện bắt buộc có sáng kiến
            if ($hasInitiative) {
                return [
                    'rank' => 'Hoàn thành xuất sắc nhiệm vụ',
                    'code' => 'EXCELLENT',
                    'reason' => 'Đạt điểm xuất sắc (>= 90 điểm) và có đề tài/sáng kiến cải tiến được công nhận (BR-08).'
                ];
            } else {
                return [
                    'rank' => 'Hoàn thành tốt nhiệm vụ',
                    'code' => 'GOOD',
                    'reason' => 'Đạt điểm >= 90 nhưng thiếu đề tài/sáng kiến cải tiến, hạ về mức Hoàn thành tốt theo Nghị định 90.'
                ];
            }
        } elseif ($totalAverage >= 75.0) {
            return [
                'rank' => 'Hoàn thành tốt nhiệm vụ',
                'code' => 'GOOD',
                'reason' => 'Tổng điểm đạt từ 75 đến dưới 90 điểm.'
            ];
        } elseif ($totalAverage >= 50.0) {
            return [
                'rank' => 'Hoàn thành nhiệm vụ',
                'code' => 'SATISFACTORY',
                'reason' => 'Tổng điểm đạt từ 50 đến dưới 75 điểm.'
            ];
        } else {
            return [
                'rank' => 'Không hoàn thành nhiệm vụ',
                'code' => 'UNSATISFACTORY',
                'reason' => 'Tổng điểm dưới 50 điểm.'
            ];
        }
    }
}
