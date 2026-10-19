<?php
namespace App\Models;

use PDO;
use PDOException;

class Database {
    private static ?PDO $instance = null;

    public static function getConnection(): PDO {
        if (self::$instance === null) {
            $config = require dirname(dirname(__DIR__)) . '/config/database.php';
            $driver = $config['driver'] ?? 'sqlite';

            try {
                if ($driver === 'sqlite') {
                    $dbFile = $config['sqlite']['database'];
                    $isNew = !file_exists($dbFile) || filesize($dbFile) === 0;
                    
                    self::$instance = new PDO("sqlite:" . $dbFile);
                    self::$instance->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
                    self::$instance->setAttribute(PDO::ATTR_DEFAULT_FETCH_MODE, PDO::FETCH_ASSOC);
                    self::$instance->exec("PRAGMA foreign_keys = ON;");

                    if ($isNew) {
                        self::initDatabase(self::$instance);
                    }
                } else {
                    $c = $config['mysql'];
                    $dsn = "mysql:host={$c['host']};port={$c['port']};dbname={$c['database']};charset={$c['charset']}";
                    self::$instance = new PDO($dsn, $c['username'], $c['password']);
                    self::$instance->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
                    self::$instance->setAttribute(PDO::ATTR_DEFAULT_FETCH_MODE, PDO::FETCH_ASSOC);
                }
            } catch (PDOException $e) {
                die("Lỗi kết nối Cơ sở dữ liệu: " . $e->getMessage());
            }
        }
        return self::$instance;
    }

    public static function initDatabase(PDO $db): void {
        // Tạo bảng SQLite
        $sql = "
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT NOT NULL UNIQUE,
            email TEXT NOT NULL UNIQUE,
            password TEXT NOT NULL,
            full_name TEXT NOT NULL,
            role TEXT NOT NULL,
            department_id INTEGER,
            position_id INTEGER,
            is_civil_servant INTEGER DEFAULT 0,
            status TEXT DEFAULT 'Active',
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS organizations (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            tax_code TEXT,
            org_type TEXT NOT NULL,
            status TEXT DEFAULT 'Active'
        );

        CREATE TABLE IF NOT EXISTS departments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            org_id INTEGER NOT NULL,
            parent_id INTEGER,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            tree_path TEXT NOT NULL,
            level INTEGER DEFAULT 1,
            manager_id INTEGER
        );

        CREATE TABLE IF NOT EXISTS positions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            is_leadership INTEGER DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS kpi_categories (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            perspective TEXT NOT NULL,
            target_sector TEXT DEFAULT 'All'
        );

        CREATE TABLE IF NOT EXISTS measurement_units (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            symbol TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS kpi_master_items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            category_id INTEGER NOT NULL,
            unit_id INTEGER NOT NULL,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            description TEXT,
            scoring_formula TEXT DEFAULT 'HigherIsBetter',
            is_civil_servant_only INTEGER DEFAULT 0,
            is_active INTEGER DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS evaluation_periods (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            period_type TEXT NOT NULL,
            year INTEGER NOT NULL,
            start_date DATE NOT NULL,
            end_date DATE NOT NULL,
            self_deadline DATE NOT NULL,
            manager_deadline DATE NOT NULL,
            status TEXT DEFAULT 'Open'
        );

        CREATE TABLE IF NOT EXISTS kpi_templates (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            position_id INTEGER NOT NULL,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            description TEXT
        );

        CREATE TABLE IF NOT EXISTS kpi_template_details (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            template_id INTEGER NOT NULL,
            kpi_item_id INTEGER NOT NULL,
            target_value REAL NOT NULL,
            min_threshold REAL DEFAULT 0.0,
            weight REAL NOT NULL
        );

        CREATE TABLE IF NOT EXISTS kpi_assignments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            period_id INTEGER NOT NULL,
            employee_id INTEGER NOT NULL,
            approver_id INTEGER NOT NULL,
            title TEXT NOT NULL,
            total_weight REAL DEFAULT 100.0,
            status TEXT DEFAULT 'Draft',
            committed_at DATETIME,
            approved_at DATETIME,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            UNIQUE(period_id, employee_id)
        );

        CREATE TABLE IF NOT EXISTS kpi_assignment_details (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            assignment_id INTEGER NOT NULL,
            kpi_item_id INTEGER NOT NULL,
            kpi_name TEXT NOT NULL,
            unit TEXT NOT NULL,
            target_value REAL NOT NULL,
            min_threshold REAL DEFAULT 0.0,
            max_cap REAL DEFAULT 150.0,
            weight REAL NOT NULL,
            scoring_formula TEXT DEFAULT 'HigherIsBetter'
        );

        CREATE TABLE IF NOT EXISTS kpi_evidences (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            assignment_detail_id INTEGER NOT NULL,
            file_name TEXT NOT NULL,
            file_path TEXT NOT NULL,
            file_size INTEGER NOT NULL,
            uploaded_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS evaluations (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            assignment_id INTEGER NOT NULL UNIQUE,
            employee_id INTEGER NOT NULL,
            period_id INTEGER NOT NULL,
            self_total_score REAL DEFAULT 0.0,
            manager_total_score REAL DEFAULT 0.0,
            final_score REAL DEFAULT 0.0,
            final_classification TEXT,
            status TEXT DEFAULT 'Not_Started',
            is_locked INTEGER DEFAULT 0,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS evaluation_details (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            evaluation_id INTEGER NOT NULL,
            assignment_detail_id INTEGER NOT NULL,
            actual_value REAL DEFAULT 0.0,
            completion_rate REAL DEFAULT 0.0,
            self_score REAL DEFAULT 0.0,
            manager_score REAL DEFAULT 0.0,
            score_gap REAL DEFAULT 0.0,
            self_remarks TEXT,
            manager_remarks TEXT,
            score_gap_reason TEXT,
            UNIQUE(evaluation_id, assignment_detail_id)
        );

        CREATE TABLE IF NOT EXISTS civil_servant_classifications (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            evaluation_id INTEGER NOT NULL UNIQUE,
            civil_servant_id INTEGER NOT NULL,
            political_ethics_score INTEGER DEFAULT 90,
            work_discipline_score INTEGER DEFAULT 90,
            duty_execution_score INTEGER DEFAULT 90,
            has_disciplinary_action INTEGER DEFAULT 0,
            has_approved_initiative INTEGER DEFAULT 1,
            collective_comments TEXT,
            party_opinion TEXT,
            proposed_rank TEXT,
            finalized_rank TEXT,
            approved_at DATETIME
        );

        CREATE TABLE IF NOT EXISTS notifications (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            recipient_id INTEGER NOT NULL,
            title TEXT NOT NULL,
            content TEXT NOT NULL,
            type TEXT DEFAULT 'Info',
            is_read INTEGER DEFAULT 0,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS audit_logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER,
            action TEXT NOT NULL,
            entity_name TEXT NOT NULL,
            entity_id INTEGER,
            details TEXT,
            ip_address TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS payrolls (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            period_id INTEGER NOT NULL,
            employee_id INTEGER NOT NULL,
            evaluation_id INTEGER,
            base_salary REAL NOT NULL DEFAULT 5000000,
            salary_coefficient REAL DEFAULT 2.34,
            kpi_score REAL DEFAULT 0.0,
            kpi_multiplier REAL DEFAULT 1.0,
            kpi_bonus REAL DEFAULT 0.0,
            allowances REAL DEFAULT 0.0,
            deductions REAL DEFAULT 0.0,
            net_salary REAL NOT NULL DEFAULT 0.0,
            payment_method TEXT NOT NULL DEFAULT 'Tiền mặt',
            payment_status TEXT NOT NULL DEFAULT 'Chờ chi tiền mặt',
            voucher_code TEXT,
            paid_at DATETIME,
            cashier_id INTEGER,
            recipient_signature TEXT,
            notes TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            UNIQUE(period_id, employee_id)
        );
        ";

        $db->exec($sql);
        self::seedSampleData($db);
    }

    public static function seedSampleData(PDO $db): void {
        // 1. Thêm Cơ quan / Doanh nghiệp
        $db->exec("
            INSERT INTO organizations (code, name, tax_code, org_type) VALUES 
            ('SNGV_HN', 'Sở Ngoại vụ Thành phố Hà Nội', '0101234567', 'PublicAdministration'),
            ('TECH_CORP', 'Công ty Cổ phần Công nghệ Tiên Phong', '0109988776', 'Enterprise');
        ");

        // 2. Thêm Phòng ban
        $db->exec("
            INSERT INTO departments (org_id, parent_id, code, name, tree_path, level) VALUES
            (1, NULL, 'BGD', 'Ban Giám đốc Sở', '/1/', 1),
            (1, 1, 'PHONG_TCCB', 'Phòng Tổ chức Cán bộ', '/1/2/', 2),
            (1, 1, 'PHONG_HTQT', 'Phòng Hợp tác Quốc tế', '/1/3/', 2),
            (1, 1, 'VAN_PHONG', 'Văn phòng Sở', '/1/4/', 2),
            (1, 1, 'PHONG_KHTC', 'Phòng Kế hoạch - Tài chính', '/1/6/', 2),
            (2, NULL, 'KHOI_DEV', 'Khối Phát triển Phần mềm', '/5/', 1);
        ");

        // 3. Thêm Chức danh
        $db->exec("
            INSERT INTO positions (code, name, is_leadership) VALUES
            ('GIAM_DOC', 'Giám đốc Sở / Thủ trưởng', 1),
            ('TRUONG_PHONG', 'Trưởng phòng / Quản lý', 1),
            ('CHUYEN_VIEN_CC', 'Chuyên viên cao cấp', 0),
            ('CHUYEN_VIEN', 'Chuyên viên nghiệp vụ', 0),
            ('KE_TOAN', 'Kế toán viên', 0),
            ('DEV_LEAD', 'Trưởng nhóm Lập trình', 1),
            ('DEV_SENIOR', 'Kỹ sư Phần mềm', 0);
        ");

        // 4. Thêm Tài khoản & Cán bộ người dùng
        // Mật khẩu mặc định: 123456
        $pass = password_hash('123456', PASSWORD_DEFAULT);
        $db->exec("
            INSERT INTO users (username, email, password, full_name, role, department_id, position_id, is_civil_servant) VALUES
            ('admin', 'admin@kpi.gov.vn', '$pass', 'Quản trị viên Hệ thống', 'Super Admin', 1, 1, 0),
            ('director', 'director@sngv.gov.vn', '$pass', 'Trần Lãnh Đạo (Giám đốc Sở)', 'Director', 1, 1, 1),
            ('manager', 'manager@sngv.gov.vn', '$pass', 'Lê Trưởng Phòng (TP Hợp tác Quốc tế)', 'Manager', 3, 2, 1),
            ('officer1', 'officer1@sngv.gov.vn', '$pass', 'Nguyễn Chuyên Viên (Công chức HTQT)', 'Employee', 3, 4, 1),
            ('officer2', 'officer2@sngv.gov.vn', '$pass', 'Phạm Công Chức (Công chức TCCB)', 'Employee', 2, 4, 1),
            ('hr_admin', 'hr@sngv.gov.vn', '$pass', 'Vũ Thị Nhân Sự (Cán bộ TCCB)', 'HR Admin', 2, 4, 1),
            ('accountant', 'accountant@sngv.gov.vn', '$pass', 'Hoàng Kế Toán (Kế toán viên)', 'Accountant', 5, 5, 1);
        ");

        // 5. Thêm Nhóm KPI
        $db->exec("
            INSERT INTO kpi_categories (code, name, perspective, target_sector) VALUES
            ('CAT_ETH', 'Phẩm chất Chính trị & Đạo đức Công vụ', 'PublicServiceEthics', 'PublicSector'),
            ('CAT_DUTY', 'Chức trách & Nhiệm vụ Chuyên môn', 'InternalProcess', 'All'),
            ('CAT_INIT', 'Sáng kiến Cải tiến & Đổi mới', 'LearningGrowth', 'All'),
            ('CAT_FIN', 'Hiệu quả Kinh tế & Tài chính', 'Financial', 'Enterprise');
        ");

        // 6. Thêm Đơn vị tính
        $db->exec("
            INSERT INTO measurement_units (code, name, symbol) VALUES
            ('PERCENT', 'Tỷ lệ phần trăm', '%'),
            ('DOC', 'Hồ sơ / Văn bản', 'Hồ sơ'),
            ('PROJECT', 'Đề án / Dự án', 'Đề án'),
            ('HOURS', 'Giờ làm việc', 'Giờ');
        ");

        // 7. Thêm Ngân hàng Chỉ số KPI chuẩn
        $db->exec("
            INSERT INTO kpi_master_items (category_id, unit_id, code, name, description, scoring_formula, is_civil_servant_only) VALUES
            (2, 2, 'KPI_HTQT_01', 'Tiến độ thẩm định hồ sơ ngoại giao', 'Xử lý văn bản đối ngoại đúng và trước hạn quy định', 'HigherIsBetter', 1),
            (2, 1, 'KPI_HTQT_02', 'Tỷ lệ hài lòng của các đoàn khách quốc tế', 'Đo lường chất lượng công tác lễ tân và hậu cần', 'HigherIsBetter', 1),
            (2, 3, 'KPI_HTQT_03', 'Tổ chức hội nghị quốc tế thành công', 'Chủ trì tổ chức các sự kiện đối ngoại lớn của Thủ đô', 'HigherIsBetter', 1),
            (3, 3, 'KPI_INIT_01', 'Sáng kiến cải tiến quy trình công vụ', 'Đề xuất giải pháp rút ngắn thời gian giải quyết TTHC', 'HigherIsBetter', 1),
            (1, 1, 'KPI_ETH_01', 'Chấp hành kỷ cương hành chính và văn hóa công sở', 'Không vi phạm quy chế giờ giấc, lễ tiết tác phong', 'HigherIsBetter', 1);
        ");

        // 8. Thêm Kỳ đánh giá năm 2026
        $db->exec("
            INSERT INTO evaluation_periods (code, name, period_type, year, start_date, end_date, self_deadline, manager_deadline, status) VALUES
            ('KPI_2026_Q1', 'Đánh giá Hiệu suất Quý 1/2026', 'Quarterly', 2026, '2026-01-01', '2026-03-31', '2026-03-31', '2026-04-05', 'Open'),
            ('KPI_2026_YEAR', 'Đánh giá & Phân loại Cán bộ Công chức Năm 2026', 'Annual', 2026, '2026-01-01', '2026-12-31', '2026-12-15', '2026-12-22', 'Planning');
        ");

        // 9. Thêm Bản giao KPI mẫu cho Nguyễn Chuyên Viên (officer1 - id=4)
        $db->exec("
            INSERT INTO kpi_assignments (period_id, employee_id, approver_id, title, total_weight, status, approved_at) VALUES
            (1, 4, 3, 'Bản giao KPI Quý 1/2026 - Nguyễn Chuyên Viên', 100.0, 'Approved_Locked', '2026-01-10 09:00:00');
        ");

        $db->exec("
            INSERT INTO kpi_assignment_details (assignment_id, kpi_item_id, kpi_name, unit, target_value, max_cap, weight, scoring_formula) VALUES
            (1, 1, 'Tiến độ thẩm định hồ sơ ngoại giao', 'Hồ sơ', 50.0, 150.0, 40.0, 'HigherIsBetter'),
            (1, 2, 'Tỷ lệ hài lòng của các đoàn khách quốc tế', '%', 95.0, 120.0, 30.0, 'HigherIsBetter'),
            (1, 3, 'Tổ chức hội nghị quốc tế thành công', 'Đề án', 2.0, 150.0, 20.0, 'HigherIsBetter'),
            (1, 5, 'Chấp hành kỷ cương hành chính và văn hóa công sở', '%', 100.0, 100.0, 10.0, 'HigherIsBetter');
        ");

        // 10. Tạo sẵn Phiếu tự đánh giá đang ở giai đoạn hoàn thành để sẵn sàng test
        $db->exec("
            INSERT INTO evaluations (assignment_id, employee_id, period_id, self_total_score, manager_total_score, final_score, final_classification, status) VALUES
            (1, 4, 1, 94.5, 91.0, 91.0, 'Hoàn thành tốt nhiệm vụ', 'Manager_Reviewed');
        ");

        $db->exec("
            INSERT INTO evaluation_details (evaluation_id, assignment_detail_id, actual_value, completion_rate, self_score, manager_score, score_gap, self_remarks, manager_remarks, score_gap_reason) VALUES
            (1, 1, 60.0, 120.0, 48.0, 45.0, 3.0, 'Đã hoàn thành 60 hồ sơ đối ngoại vượt tiến độ', 'Ghi nhận đạt 60 hồ sơ nhưng có 2 hồ sơ chỉnh sửa nhỏ', NULL),
            (1, 2, 98.0, 103.1, 30.9, 30.0, 0.9, '100% đoàn khách phản hồi hài lòng', 'Duyệt điểm xuất sắc', NULL),
            (1, 3, 2.0, 100.0, 20.0, 20.0, 0.0, 'Chủ trì thành công 2 hội thảo song phương', 'Đạt kế hoạch', NULL),
            (1, 4, 96.0, 96.0, 9.6, 9.0, 0.6, 'Đi làm đầy đủ đúng giờ', 'Chấp hành tốt', NULL);
        ");

        // 11. Tạo dữ liệu phân loại cán bộ công chức phục vụ test Nghị định 90 (Kiểm tra khống chế 20%)
        $db->exec("
            INSERT INTO civil_servant_classifications (evaluation_id, civil_servant_id, political_ethics_score, work_discipline_score, duty_execution_score, has_disciplinary_action, has_approved_initiative, proposed_rank, finalized_rank) VALUES
            (1, 4, 95, 92, 91, 0, 1, 'Hoàn thành xuất sắc nhiệm vụ', 'Hoàn thành tốt nhiệm vụ');
        ");

        // 12. Thêm thông báo mẫu
        $db->exec("
            INSERT INTO notifications (recipient_id, title, content, type) VALUES
            (4, 'Nhắc nhở nộp tự đánh giá KPI', 'Kỳ đánh giá Quý 1/2026 đã mở cổng tự đánh giá.', 'Info'),
            (3, 'Có phiếu đánh giá cần thẩm định', 'Cán bộ Nguyễn Chuyên Viên đã nộp phiếu tự đánh giá.', 'Task'),
            (2, 'Hồ sơ chờ phê duyệt', 'Có 1 hồ sơ đánh giá phòng HTQT cần Giám đốc phê chuẩn.', 'Approval');
        ");
    }
}
