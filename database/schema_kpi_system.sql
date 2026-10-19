-- =============================================================================
-- HỆ THỐNG QUẢN LÝ VÀ ĐÁNH GIÁ KPI CHO DOANH NGHIỆP VÀ CÁN BỘ CÔNG CHỨC
-- DDL Script: Khởi tạo Cấu trúc Cơ sở Dữ liệu Toàn diện (PostgreSQL 15+ Compatible)
-- Phiên bản: 1.0 (Phiên 6 / 10 - Database Physical Architecture)
-- =============================================================================

-- Bật extension hỗ trợ UUID và mã hóa nếu cần
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================================================
-- PHÂN HỆ 1: QUẢN TRỊ, PHÂN QUYỀN & BẢO MẬT (SECURITY & AUDIT)
-- =============================================================================

-- 1. Bảng tài khoản người dùng
CREATE TABLE IF NOT EXISTS users (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    salt VARCHAR(128) NOT NULL,
    is_2fa_enabled BOOLEAN DEFAULT FALSE NOT NULL,
    two_factor_secret VARCHAR(64),
    status VARCHAR(20) DEFAULT 'Pending_Activation' NOT NULL CHECK (status IN ('Pending_Activation', 'Active', 'Locked', 'Suspended', 'Deactivated')),
    failed_attempts INT DEFAULT 0 NOT NULL,
    locked_until TIMESTAMP WITH TIME ZONE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by BIGINT,
    updated_at TIMESTAMP WITH TIME ZONE,
    updated_by BIGINT,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 2. Bảng vai trò
CREATE TABLE IF NOT EXISTS roles (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    is_system_default BOOLEAN DEFAULT FALSE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 3. Bảng quyền hạn chi tiết
CREATE TABLE IF NOT EXISTS permissions (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    module VARCHAR(50) NOT NULL,
    action VARCHAR(30) NOT NULL,
    description VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 4. Bảng gán vai trò người dùng (N-N)
CREATE TABLE IF NOT EXISTS user_roles (
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    role_id BIGINT REFERENCES roles(id) ON DELETE RESTRICT,
    assigned_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    assigned_by BIGINT REFERENCES users(id),
    PRIMARY KEY (user_id, role_id)
);

-- 5. Bảng gán quyền cho vai trò (N-N)
CREATE TABLE IF NOT EXISTS role_permissions (
    role_id BIGINT REFERENCES roles(id) ON DELETE CASCADE,
    permission_id BIGINT REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

-- 6. Bảng nhật ký kiểm toán hệ thống (Audit Logs - Bất biến)
CREATE TABLE IF NOT EXISTS audit_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE SET NULL,
    action VARCHAR(50) NOT NULL,
    entity_name VARCHAR(50) NOT NULL,
    entity_id BIGINT NOT NULL,
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45) NOT NULL,
    user_agent VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 7. Bảng tham số cấu hình hệ thống
CREATE TABLE IF NOT EXISTS system_configs (
    config_key VARCHAR(50) PRIMARY KEY,
    config_value TEXT NOT NULL,
    data_type VARCHAR(20) DEFAULT 'STRING' NOT NULL,
    description VARCHAR(255),
    is_encrypted BOOLEAN DEFAULT FALSE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_by BIGINT REFERENCES users(id)
);

-- 8. Bảng quản lý phiên làm việc & Refresh Token
CREATE TABLE IF NOT EXISTS user_sessions (
    session_id VARCHAR(128) PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE NOT NULL,
    refresh_token_hash VARCHAR(255) NOT NULL UNIQUE,
    ip_address VARCHAR(45) NOT NULL,
    user_agent VARCHAR(255),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked BOOLEAN DEFAULT FALSE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- =============================================================================
-- PHÂN HỆ 2: CƠ CẤU TỔ CHỨC & HỒ SƠ NHÂN SỰ / CÔNG CHỨC
-- =============================================================================

-- 9. Bảng cơ quan / doanh nghiệp
CREATE TABLE IF NOT EXISTS organizations (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    tax_code VARCHAR(20),
    address VARCHAR(255),
    org_type VARCHAR(30) NOT NULL CHECK (org_type IN ('Enterprise', 'PublicAdministration', 'PublicService')),
    status VARCHAR(20) DEFAULT 'Active' NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 10. Bảng phòng ban trực thuộc
CREATE TABLE IF NOT EXISTS departments (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    org_id BIGINT REFERENCES organizations(id) ON DELETE RESTRICT NOT NULL,
    parent_id BIGINT REFERENCES departments(id) ON DELETE RESTRICT,
    code VARCHAR(30) NOT NULL,
    name VARCHAR(150) NOT NULL,
    tree_path VARCHAR(255) NOT NULL,
    level INT DEFAULT 1 NOT NULL,
    status VARCHAR(20) DEFAULT 'Active' NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL,
    UNIQUE (org_id, code)
);

-- 11. Bảng bao đóng phân cấp phòng ban (Closure Table)
CREATE TABLE IF NOT EXISTS department_hierarchy (
    ancestor_id BIGINT REFERENCES departments(id) ON DELETE CASCADE,
    descendant_id BIGINT REFERENCES departments(id) ON DELETE CASCADE,
    depth INT NOT NULL,
    PRIMARY KEY (ancestor_id, descendant_id)
);

-- 12. Bảng chức danh vị trí việc làm
CREATE TABLE IF NOT EXISTS positions (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    rank_level VARCHAR(30),
    is_leadership BOOLEAN DEFAULT FALSE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 13. Bảng hồ sơ nhân viên / cán bộ
CREATE TABLE IF NOT EXISTS employee_profiles (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE RESTRICT UNIQUE NOT NULL,
    department_id BIGINT REFERENCES departments(id) ON DELETE RESTRICT NOT NULL,
    position_id BIGINT REFERENCES positions(id) ON DELETE RESTRICT NOT NULL,
    staff_code VARCHAR(30) NOT NULL UNIQUE,
    full_name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    date_of_birth DATE,
    join_date DATE NOT NULL,
    is_concurrent BOOLEAN DEFAULT FALSE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 14. Bảng danh mục ngạch bậc công chức (Nghị định Bộ Nội Vụ)
CREATE TABLE IF NOT EXISTS position_ranks (
    rank_code VARCHAR(20) PRIMARY KEY,
    rank_name VARCHAR(100) NOT NULL,
    rank_group VARCHAR(50) NOT NULL,
    max_grade INT DEFAULT 9 NOT NULL
);

-- 15. Bảng hồ sơ cán bộ công chức chuyên biệt (Kế thừa từ employee_profiles)
CREATE TABLE IF NOT EXISTS civil_servant_profiles (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id BIGINT REFERENCES employee_profiles(id) ON DELETE CASCADE UNIQUE NOT NULL,
    civil_servant_code VARCHAR(30) NOT NULL UNIQUE,
    official_rank_code VARCHAR(20) REFERENCES position_ranks(rank_code) NOT NULL,
    salary_grade INT NOT NULL CHECK (salary_grade BETWEEN 1 AND 12),
    salary_coefficient NUMERIC(5,2) NOT NULL CHECK (salary_coefficient > 0),
    appointment_date DATE,
    party_status VARCHAR(30) DEFAULT 'None' CHECK (party_status IN ('Official', 'Probationary', 'None')),
    political_theory VARCHAR(30) DEFAULT 'None' CHECK (political_theory IN ('High', 'Intermediate', 'Primary', 'None')),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- =============================================================================
-- PHÂN HỆ 3: DANH MỤC DÙNG CHUNG (MASTER CATALOGS)
-- =============================================================================

-- 16. Bảng nhóm chỉ số KPI
CREATE TABLE IF NOT EXISTS kpi_categories (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    perspective VARCHAR(30) NOT NULL CHECK (perspective IN ('Financial', 'Customer', 'InternalProcess', 'LearningGrowth', 'PublicServiceEthics')),
    target_sector VARCHAR(30) DEFAULT 'All' NOT NULL CHECK (target_sector IN ('Enterprise', 'PublicSector', 'All')),
    display_order INT DEFAULT 0 NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 17. Bảng đơn vị đo lường
CREATE TABLE IF NOT EXISTS measurement_units (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(50) NOT NULL,
    symbol VARCHAR(15) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 18. Bảng phương pháp đo lường
CREATE TABLE IF NOT EXISTS measurement_methods (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    data_source VARCHAR(100),
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 19. Bảng ngân hàng chỉ số KPI chuẩn (Master KPI Bank)
CREATE TABLE IF NOT EXISTS kpi_master_items (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    category_id BIGINT REFERENCES kpi_categories(id) ON DELETE RESTRICT NOT NULL,
    unit_id BIGINT REFERENCES measurement_units(id) ON DELETE RESTRICT NOT NULL,
    method_id BIGINT REFERENCES measurement_methods(id) ON DELETE RESTRICT NOT NULL,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    scoring_formula VARCHAR(30) DEFAULT 'HigherIsBetter' NOT NULL CHECK (scoring_formula IN ('HigherIsBetter', 'LowerIsBetter', 'TargetRange')),
    is_civil_servant_only BOOLEAN DEFAULT FALSE NOT NULL,
    is_active BOOLEAN DEFAULT TRUE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 20. Bảng thang điểm
CREATE TABLE IF NOT EXISTS score_scales (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    min_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    max_score NUMERIC(5,2) DEFAULT 100.0 NOT NULL,
    is_default BOOLEAN DEFAULT FALSE NOT NULL
);

-- 21. Bảng mức xếp loại chất lượng
CREATE TABLE IF NOT EXISTS classification_levels (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    scale_id BIGINT REFERENCES score_scales(id) ON DELETE CASCADE NOT NULL,
    code VARCHAR(30) NOT NULL,
    name VARCHAR(100) NOT NULL,
    min_percentage NUMERIC(5,2) NOT NULL,
    max_percentage NUMERIC(5,2) NOT NULL,
    requires_initiative BOOLEAN DEFAULT FALSE NOT NULL,
    is_capped_at_20_percent BOOLEAN DEFAULT FALSE NOT NULL
);

-- 22. Bảng kỳ đánh giá hiệu suất
CREATE TABLE IF NOT EXISTS evaluation_periods (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    scale_id BIGINT REFERENCES score_scales(id) ON DELETE RESTRICT NOT NULL,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    period_type VARCHAR(20) NOT NULL CHECK (period_type IN ('Monthly', 'Quarterly', 'SemiAnnual', 'Annual')),
    year INT NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    self_deadline TIMESTAMP WITH TIME ZONE NOT NULL,
    manager_deadline TIMESTAMP WITH TIME ZONE NOT NULL,
    closing_date TIMESTAMP WITH TIME ZONE NOT NULL,
    status VARCHAR(20) DEFAULT 'Planning' NOT NULL CHECK (status IN ('Planning', 'Open', 'InEvaluation', 'Closing', 'Closed', 'Archived')),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- =============================================================================
-- PHÂN HỆ 4: THIẾT LẬP & GIAO CHỈ TIÊU KPI
-- =============================================================================

-- 23. Bảng bộ KPI mẫu theo chức danh
CREATE TABLE IF NOT EXISTS kpi_templates (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    position_id BIGINT REFERENCES positions(id) ON DELETE CASCADE NOT NULL,
    department_id BIGINT REFERENCES departments(id) ON DELETE CASCADE,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    description TEXT,
    status VARCHAR(20) DEFAULT 'Active' NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 24. Bảng chi tiết chỉ tiêu trong bộ mẫu
CREATE TABLE IF NOT EXISTS kpi_template_details (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    template_id BIGINT REFERENCES kpi_templates(id) ON DELETE CASCADE NOT NULL,
    kpi_item_id BIGINT REFERENCES kpi_master_items(id) ON DELETE RESTRICT NOT NULL,
    target_value NUMERIC(15,2) DEFAULT 100.0 NOT NULL,
    min_threshold NUMERIC(15,2) DEFAULT 0.0,
    weight NUMERIC(5,2) NOT NULL CHECK (weight >= 5.0 AND weight <= 50.0),
    guidance_notes TEXT
);

-- 25. Bảng bản giao KPI cá nhân
CREATE TABLE IF NOT EXISTS kpi_assignments (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    period_id BIGINT REFERENCES evaluation_periods(id) ON DELETE RESTRICT NOT NULL,
    employee_id BIGINT REFERENCES employee_profiles(id) ON DELETE RESTRICT NOT NULL,
    approver_id BIGINT REFERENCES users(id) ON DELETE RESTRICT NOT NULL,
    title VARCHAR(200) NOT NULL,
    total_weight NUMERIC(5,2) NOT NULL CHECK (total_weight = 100.00),
    status VARCHAR(30) DEFAULT 'Draft' NOT NULL CHECK (status IN ('Draft', 'Pending_Staff_Confirm', 'Pending_Approval', 'Approved_Locked', 'In_Evaluation', 'Completed', 'Archived')),
    committed_at TIMESTAMP WITH TIME ZONE,
    approved_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL,
    UNIQUE (period_id, employee_id)
);

-- 26. Bảng chi tiết chỉ tiêu được giao (Snapshot tên & đơn vị)
CREATE TABLE IF NOT EXISTS kpi_assignment_details (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    assignment_id BIGINT REFERENCES kpi_assignments(id) ON DELETE CASCADE NOT NULL,
    kpi_item_id BIGINT REFERENCES kpi_master_items(id) ON DELETE RESTRICT NOT NULL,
    kpi_name VARCHAR(255) NOT NULL,
    unit VARCHAR(30) NOT NULL,
    target_value NUMERIC(15,2) NOT NULL,
    min_threshold NUMERIC(15,2) DEFAULT 0.0,
    max_cap NUMERIC(5,2) DEFAULT 150.0 NOT NULL,
    weight NUMERIC(5,2) NOT NULL CHECK (weight >= 5.0 AND weight <= 50.0),
    scoring_formula VARCHAR(30) NOT NULL
);

-- 27. Bảng tài liệu minh chứng số
CREATE TABLE IF NOT EXISTS kpi_evidences (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    assignment_detail_id BIGINT REFERENCES kpi_assignment_details(id) ON DELETE CASCADE NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL CHECK (file_size <= 26214400),
    description VARCHAR(255),
    uploaded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- =============================================================================
-- PHÂN HỆ 5: ĐÁNH GIÁ, CÔNG CHỨC & WORKFLOW
-- =============================================================================

-- 28. Bảng phiếu đánh giá hiệu suất tổng hợp
CREATE TABLE IF NOT EXISTS evaluations (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    assignment_id BIGINT REFERENCES kpi_assignments(id) ON DELETE RESTRICT UNIQUE NOT NULL,
    employee_id BIGINT REFERENCES employee_profiles(id) ON DELETE RESTRICT NOT NULL,
    period_id BIGINT REFERENCES evaluation_periods(id) ON DELETE RESTRICT NOT NULL,
    self_total_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    manager_total_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    final_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    final_classification VARCHAR(50),
    status VARCHAR(30) DEFAULT 'Not_Started' NOT NULL CHECK (status IN ('Not_Started', 'Self_Assessing', 'Self_Submitted', 'Manager_Reviewing', 'Pending_Director_Approval', 'Final_Approved', 'Locked')),
    is_disputed BOOLEAN DEFAULT FALSE NOT NULL,
    is_locked BOOLEAN DEFAULT FALSE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL
);

-- 29. Bảng chi tiết điểm số từng chỉ tiêu
CREATE TABLE IF NOT EXISTS evaluation_details (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    evaluation_id BIGINT REFERENCES evaluations(id) ON DELETE CASCADE NOT NULL,
    assignment_detail_id BIGINT REFERENCES kpi_assignment_details(id) ON DELETE RESTRICT UNIQUE NOT NULL,
    actual_value NUMERIC(15,2) DEFAULT 0.0 NOT NULL,
    completion_rate NUMERIC(7,2) DEFAULT 0.0 NOT NULL,
    self_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    manager_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    score_gap NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    self_remarks TEXT,
    manager_remarks TEXT,
    score_gap_reason TEXT
);

-- 30. Bảng phân loại cán bộ công chức theo Nghị định 90/2020/NĐ-CP
CREATE TABLE IF NOT EXISTS civil_servant_classifications (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    evaluation_id BIGINT REFERENCES evaluations(id) ON DELETE CASCADE UNIQUE NOT NULL,
    civil_servant_id BIGINT REFERENCES civil_servant_profiles(id) ON DELETE RESTRICT NOT NULL,
    political_ethics_score INT NOT NULL CHECK (political_ethics_score BETWEEN 0 AND 100),
    work_discipline_score INT NOT NULL CHECK (work_discipline_score BETWEEN 0 AND 100),
    duty_execution_score INT NOT NULL CHECK (duty_execution_score BETWEEN 0 AND 100),
    has_disciplinary_action BOOLEAN DEFAULT FALSE NOT NULL,
    has_approved_initiative BOOLEAN DEFAULT FALSE NOT NULL,
    collective_comments TEXT,
    party_opinion TEXT,
    proposed_rank VARCHAR(50) NOT NULL,
    finalized_rank VARCHAR(50) NOT NULL,
    approved_by BIGINT REFERENCES users(id),
    approved_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 31. Bảng đánh giá ngang hàng 360 độ (Tùy chọn)
CREATE TABLE IF NOT EXISTS peer_reviews (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    evaluation_id BIGINT REFERENCES evaluations(id) ON DELETE CASCADE NOT NULL,
    reviewer_id BIGINT REFERENCES employee_profiles(id) ON DELETE RESTRICT NOT NULL,
    behavior_score NUMERIC(5,2) NOT NULL,
    feedback_comments TEXT,
    is_anonymous BOOLEAN DEFAULT TRUE NOT NULL,
    submitted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 32. Bảng quản lý tiến trình luồng duyệt (Workflow Engine)
CREATE TABLE IF NOT EXISTS evaluation_workflows (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    evaluation_id BIGINT REFERENCES evaluations(id) ON DELETE CASCADE UNIQUE NOT NULL,
    current_step VARCHAR(50) NOT NULL,
    status VARCHAR(30) DEFAULT 'Running' NOT NULL
);

-- 33. Bảng các bước phê duyệt định tuyến
CREATE TABLE IF NOT EXISTS workflow_steps (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    workflow_id BIGINT REFERENCES evaluation_workflows(id) ON DELETE CASCADE NOT NULL,
    step_order INT NOT NULL,
    step_name VARCHAR(100) NOT NULL,
    required_role_id BIGINT REFERENCES roles(id) NOT NULL,
    assigned_actor_id BIGINT REFERENCES users(id),
    action_taken VARCHAR(30) DEFAULT 'Pending',
    comments TEXT,
    completed_at TIMESTAMP WITH TIME ZONE
);

-- 34. Bảng nhật ký luân chuyển hồ sơ đánh giá
CREATE TABLE IF NOT EXISTS workflow_history (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    evaluation_id BIGINT REFERENCES evaluations(id) ON DELETE CASCADE NOT NULL,
    actor_id BIGINT REFERENCES users(id) NOT NULL,
    from_state VARCHAR(50) NOT NULL,
    to_state VARCHAR(50) NOT NULL,
    comments TEXT,
    action_time TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 35. Bảng giải quyết khiếu nại kết quả đánh giá (BR-14)
CREATE TABLE IF NOT EXISTS evaluation_disputes (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    evaluation_id BIGINT REFERENCES evaluations(id) ON DELETE CASCADE NOT NULL,
    appellant_id BIGINT REFERENCES employee_profiles(id) ON DELETE RESTRICT NOT NULL,
    reason TEXT NOT NULL,
    proposed_adjustment TEXT,
    resolver_id BIGINT REFERENCES users(id),
    resolution_notes TEXT,
    status VARCHAR(20) DEFAULT 'Open' NOT NULL CHECK (status IN ('Open', 'UnderReview', 'Resolved', 'Rejected')),
    submitted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    resolved_at TIMESTAMP WITH TIME ZONE
);

-- =============================================================================
-- PHÂN HỆ 6: BÁO CÁO, DASHBOARD & THÔNG BÁO
-- =============================================================================

-- 36. Bảng cấu hình bảng điều khiển người dùng
CREATE TABLE IF NOT EXISTS dashboard_configs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE NOT NULL,
    widget_key VARCHAR(50) NOT NULL,
    grid_x INT DEFAULT 0 NOT NULL,
    grid_y INT DEFAULT 0 NOT NULL,
    grid_width INT DEFAULT 4 NOT NULL,
    grid_height INT DEFAULT 3 NOT NULL,
    UNIQUE (user_id, widget_key)
);

-- 37. Bảng kết xuất tài liệu báo cáo
CREATE TABLE IF NOT EXISTS reports (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    period_id BIGINT REFERENCES evaluation_periods(id) NOT NULL,
    generated_by BIGINT REFERENCES users(id) NOT NULL,
    report_type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    parameters_json JSONB,
    export_format VARCHAR(10) NOT NULL CHECK (export_format IN ('XLSX', 'PDF', 'CSV')),
    file_url VARCHAR(500) NOT NULL,
    generated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 38. Bảng thông báo chuông In-App
CREATE TABLE IF NOT EXISTS notifications (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    recipient_id BIGINT REFERENCES users(id) ON DELETE CASCADE NOT NULL,
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    type VARCHAR(30) DEFAULT 'Info' NOT NULL,
    target_url VARCHAR(255),
    is_read BOOLEAN DEFAULT FALSE NOT NULL,
    read_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 39. Bảng mẫu thông báo & mẫu email
CREATE TABLE IF NOT EXISTS notification_templates (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_code VARCHAR(50) NOT NULL UNIQUE,
    subject_template VARCHAR(255) NOT NULL,
    body_template TEXT NOT NULL,
    channels VARCHAR(50) DEFAULT 'IN_APP,EMAIL' NOT NULL,
    is_active BOOLEAN DEFAULT TRUE NOT NULL
);

-- 40. Bảng hàng đợi gửi email tự động
CREATE TABLE IF NOT EXISTS email_queues (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    to_email VARCHAR(100) NOT NULL,
    subject VARCHAR(255) NOT NULL,
    html_body TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'Pending' NOT NULL CHECK (status IN ('Pending', 'Sent', 'Failed')),
    retry_count INT DEFAULT 0 NOT NULL,
    error_message TEXT,
    scheduled_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    sent_at TIMESTAMP WITH TIME ZONE
);

-- =============================================================================
-- THIẾT LẬP CHỈ MỤC TỐI ƯU HÓA HIỆU NĂNG (INDEXES)
-- =============================================================================

CREATE INDEX IF NOT EXISTS idx_users_status ON users(status);
CREATE INDEX IF NOT EXISTS idx_audit_time ON audit_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_audit_entity ON audit_logs(entity_name, entity_id);
CREATE INDEX IF NOT EXISTS idx_departments_tree ON departments(tree_path);
CREATE INDEX IF NOT EXISTS idx_emp_dept ON employee_profiles(department_id);
CREATE INDEX IF NOT EXISTS idx_assign_emp ON kpi_assignments(employee_id);
CREATE INDEX IF NOT EXISTS idx_assign_period ON kpi_assignments(period_id);
CREATE INDEX IF NOT EXISTS idx_eval_emp ON evaluations(employee_id);
CREATE INDEX IF NOT EXISTS idx_eval_period ON evaluations(period_id);
CREATE INDEX IF NOT EXISTS idx_eval_status ON evaluations(status);
CREATE INDEX IF NOT EXISTS idx_notif_recipient ON notifications(recipient_id, is_read);
CREATE INDEX IF NOT EXISTS idx_email_status ON email_queues(status, scheduled_at);

-- =============================================================================
-- TRIGGERS NGHIỆP VỤ BẢO VỆ DỮ LIỆU
-- =============================================================================

CREATE OR REPLACE FUNCTION fn_calculate_score_gap()
RETURNS TRIGGER AS $$
BEGIN
    NEW.score_gap := ABS(NEW.self_score - NEW.manager_score);
    IF NEW.score_gap > 10.00 AND (NEW.score_gap_reason IS NULL OR LENGTH(TRIM(NEW.score_gap_reason)) < 50) THEN
        RAISE EXCEPTION 'BR-05 Violation: Quản lý chấm lệch trên 10%% so với tự chấm bắt buộc nhập lý do giải trình >= 50 ký tự.';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_check_score_gap ON evaluation_details;
CREATE TRIGGER trg_check_score_gap
BEFORE INSERT OR UPDATE ON evaluation_details
FOR EACH ROW EXECUTE FUNCTION fn_calculate_score_gap();
