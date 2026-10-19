<?php
/**
 * Application Configuration
 * Hệ thống Đánh giá KPI Doanh nghiệp & Cán bộ Công chức
 */

define('APP_NAME', 'Hệ thống Quản lý & Đánh giá KPI');
define('APP_VERSION', '1.0.0');
define('APP_URL', 'http://localhost:8000');
define('BASE_PATH', dirname(__DIR__));

// Cấu hình quy chuẩn nghiệp vụ
define('KPI_TOTAL_WEIGHT', 100.0);       // BR-01: Tổng trọng số bắt buộc = 100%
define('KPI_MIN_WEIGHT', 5.0);           // BR-02: Trọng số tối thiểu 5%
define('KPI_MAX_WEIGHT', 50.0);          // BR-02: Trọng số tối đa 50%
define('KPI_MAX_CAP', 150.0);            // BR-10: Trần hoàn thành tối đa 150%
define('KPI_EVIDENCE_THRESHOLD', 120.0); // BR-09: Bắt buộc minh chứng khi đạt >= 120%
define('KPI_GAP_REASON_THRESHOLD', 10.0);// BR-05: Lệch điểm > 10% bắt buộc giải trình >= 50 ký tự
define('DECREE_90_EXCELLENCE_CAP', 0.20);// BR-06: Tỷ lệ khống chế Hoàn thành xuất sắc <= 20% số Hoàn thành tốt

// Khởi tạo session nếu chưa có
if (session_status() === PHP_SESSION_NONE) {
    session_start();
}
