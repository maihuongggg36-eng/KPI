-- =============================================================================
-- HỆ THỐNG QUẢN LÝ VÀ ĐÁNH GIÁ KPI CHO DOANH NGHIỆP VÀ CÁN BỘ CÔNG CHỨC
-- DDL Script: Chuyển đổi Cơ sở Dữ liệu Toàn diện cho Microsoft SQL Server (T-SQL / SSMS)
-- Phiên bản: 2.0 (Phiên 1/12 - Chuyển đổi PostgreSQL sang T-SQL Chuẩn Doanh nghiệp)
-- Hệ cơ quan: Học viện Hành chính và Quản trị công
-- =============================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'KPI_System')
BEGIN
    CREATE DATABASE KPI_System COLLATE Vietnamese_CI_AS;
END
GO

USE KPI_System;
GO

-- =============================================================================
-- PHÂN HỆ 1: QUẢN TRỊ, PHÂN QUYỀN & BẢO MẬT (SECURITY & AUDIT)
-- =============================================================================

-- 1. Bảng tài khoản người dùng
IF OBJECT_ID('dbo.users', 'U') IS NULL
CREATE TABLE dbo.users (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    email NVARCHAR(100) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    salt NVARCHAR(128) NOT NULL,
    is_2fa_enabled BIT DEFAULT 0 NOT NULL,
    two_factor_secret NVARCHAR(64) NULL,
    status NVARCHAR(20) DEFAULT 'Active' NOT NULL CHECK (status IN ('Pending_Activation', 'Active', 'Locked', 'Suspended', 'Deactivated')),
    failed_attempts INT DEFAULT 0 NOT NULL,
    locked_until DATETIMEOFFSET NULL,
    last_login_at DATETIMEOFFSET NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    created_by BIGINT NULL,
    updated_at DATETIMEOFFSET NULL,
    updated_by BIGINT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL
);
GO

-- 2. Bảng vai trò
IF OBJECT_ID('dbo.roles', 'U') IS NULL
CREATE TABLE dbo.roles (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(100) NOT NULL,
    description NVARCHAR(255) NULL,
    is_system_default BIT DEFAULT 0 NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL
);
GO

-- 3. Bảng quyền hạn chi tiết
IF OBJECT_ID('dbo.permissions', 'U') IS NULL
CREATE TABLE dbo.permissions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(50) NOT NULL UNIQUE,
    module NVARCHAR(50) NOT NULL,
    action NVARCHAR(30) NOT NULL,
    description NVARCHAR(255) NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL
);
GO

-- 4. Bảng gán vai trò người dùng (N-N)
IF OBJECT_ID('dbo.user_roles', 'U') IS NULL
CREATE TABLE dbo.user_roles (
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    assigned_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    assigned_by BIGINT NULL,
    CONSTRAINT PK_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT FK_user_roles_users FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE CASCADE,
    CONSTRAINT FK_user_roles_roles FOREIGN KEY (role_id) REFERENCES dbo.roles(id),
    CONSTRAINT FK_user_roles_assigned_by FOREIGN KEY (assigned_by) REFERENCES dbo.users(id)
);
GO

-- 5. Bảng gán quyền cho vai trò (N-N)
IF OBJECT_ID('dbo.role_permissions', 'U') IS NULL
CREATE TABLE dbo.role_permissions (
    role_id BIGINT NOT NULL,
    permission_id BIGINT NOT NULL,
    CONSTRAINT PK_role_permissions PRIMARY KEY (role_id, permission_id),
    CONSTRAINT FK_role_permissions_roles FOREIGN KEY (role_id) REFERENCES dbo.roles(id) ON DELETE CASCADE,
    CONSTRAINT FK_role_permissions_permissions FOREIGN KEY (permission_id) REFERENCES dbo.permissions(id) ON DELETE CASCADE
);
GO

-- 6. Bảng nhật ký kiểm toán hệ thống (Audit Logs - Bất biến)
IF OBJECT_ID('dbo.audit_logs', 'U') IS NULL
CREATE TABLE dbo.audit_logs (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NULL,
    action NVARCHAR(50) NOT NULL,
    entity_name NVARCHAR(50) NOT NULL,
    entity_id BIGINT NOT NULL,
    old_values NVARCHAR(MAX) NULL,
    new_values NVARCHAR(MAX) NULL,
    ip_address NVARCHAR(45) NOT NULL,
    user_agent NVARCHAR(255) NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_audit_logs_users FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE SET NULL
);
GO

-- 7. Bảng tham số cấu hình hệ thống
IF OBJECT_ID('dbo.system_configs', 'U') IS NULL
CREATE TABLE dbo.system_configs (
    config_key NVARCHAR(50) PRIMARY KEY,
    config_value NVARCHAR(MAX) NOT NULL,
    data_type NVARCHAR(20) DEFAULT 'STRING' NOT NULL,
    description NVARCHAR(255) NULL,
    is_encrypted BIT DEFAULT 0 NOT NULL,
    updated_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    updated_by BIGINT NULL,
    CONSTRAINT FK_system_configs_users FOREIGN KEY (updated_by) REFERENCES dbo.users(id)
);
GO

-- 8. Bảng quản lý phiên làm việc & Refresh Token
IF OBJECT_ID('dbo.user_sessions', 'U') IS NULL
CREATE TABLE dbo.user_sessions (
    session_id NVARCHAR(128) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    refresh_token_hash NVARCHAR(255) NOT NULL UNIQUE,
    ip_address NVARCHAR(45) NOT NULL,
    user_agent NVARCHAR(255) NULL,
    expires_at DATETIMEOFFSET NOT NULL,
    is_revoked BIT DEFAULT 0 NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_user_sessions_users FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE CASCADE
);
GO

-- =============================================================================
-- PHÂN HỆ 2: CƠ CẤU TỔ CHỨC & HỒ SƠ NHÂN SỰ / CÔNG CHỨC
-- =============================================================================

-- 9. Bảng cơ quan / doanh nghiệp
IF OBJECT_ID('dbo.organizations', 'U') IS NULL
CREATE TABLE dbo.organizations (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(200) NOT NULL,
    tax_code NVARCHAR(20) NULL,
    address NVARCHAR(255) NULL,
    org_type NVARCHAR(30) NOT NULL CHECK (org_type IN ('Enterprise', 'PublicAdministration', 'PublicService')),
    status NVARCHAR(20) DEFAULT 'Active' NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL
);
GO

-- 10. Bảng phòng ban trực thuộc
IF OBJECT_ID('dbo.departments', 'U') IS NULL
CREATE TABLE dbo.departments (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    org_id BIGINT NOT NULL,
    parent_id BIGINT NULL,
    code NVARCHAR(30) NOT NULL,
    name NVARCHAR(150) NOT NULL,
    tree_path NVARCHAR(255) NOT NULL,
    level INT DEFAULT 1 NOT NULL,
    status NVARCHAR(20) DEFAULT 'Active' NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT UQ_departments_org_code UNIQUE (org_id, code),
    CONSTRAINT FK_departments_org FOREIGN KEY (org_id) REFERENCES dbo.organizations(id),
    CONSTRAINT FK_departments_parent FOREIGN KEY (parent_id) REFERENCES dbo.departments(id)
);
GO

-- 11. Bảng bao đóng phân cấp phòng ban (Closure Table)
IF OBJECT_ID('dbo.department_hierarchy', 'U') IS NULL
CREATE TABLE dbo.department_hierarchy (
    ancestor_id BIGINT NOT NULL,
    descendant_id BIGINT NOT NULL,
    depth INT NOT NULL,
    CONSTRAINT PK_department_hierarchy PRIMARY KEY (ancestor_id, descendant_id),
    CONSTRAINT FK_dept_hier_ancestor FOREIGN KEY (ancestor_id) REFERENCES dbo.departments(id) ON DELETE CASCADE,
    CONSTRAINT FK_dept_hier_descendant FOREIGN KEY (descendant_id) REFERENCES dbo.departments(id)
);
GO

-- 12. Bảng chức danh vị trí việc làm
IF OBJECT_ID('dbo.positions', 'U') IS NULL
CREATE TABLE dbo.positions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(100) NOT NULL,
    rank_level NVARCHAR(30) NULL,
    is_leadership BIT DEFAULT 0 NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL
);
GO

-- 13. Bảng hồ sơ nhân viên / cán bộ
IF OBJECT_ID('dbo.employee_profiles', 'U') IS NULL
CREATE TABLE dbo.employee_profiles (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL UNIQUE,
    department_id BIGINT NOT NULL,
    position_id BIGINT NOT NULL,
    staff_code NVARCHAR(30) NOT NULL UNIQUE,
    full_name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(20) NULL,
    date_of_birth DATE NULL,
    join_date DATE NOT NULL,
    is_concurrent BIT DEFAULT 0 NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    updated_at DATETIMEOFFSET NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT FK_emp_users FOREIGN KEY (user_id) REFERENCES dbo.users(id),
    CONSTRAINT FK_emp_dept FOREIGN KEY (department_id) REFERENCES dbo.departments(id),
    CONSTRAINT FK_emp_position FOREIGN KEY (position_id) REFERENCES dbo.positions(id)
);
GO

-- 14. Bảng danh mục ngạch bậc công chức (Bộ Nội Vụ)
IF OBJECT_ID('dbo.position_ranks', 'U') IS NULL
CREATE TABLE dbo.position_ranks (
    rank_code NVARCHAR(20) PRIMARY KEY,
    rank_name NVARCHAR(100) NOT NULL,
    rank_group NVARCHAR(50) NOT NULL,
    max_grade INT DEFAULT 9 NOT NULL
);
GO

-- 15. Bảng hồ sơ cán bộ công chức chuyên biệt
IF OBJECT_ID('dbo.civil_servant_profiles', 'U') IS NULL
CREATE TABLE dbo.civil_servant_profiles (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    employee_id BIGINT NOT NULL UNIQUE,
    civil_servant_code NVARCHAR(30) NOT NULL UNIQUE,
    official_rank_code NVARCHAR(20) NOT NULL,
    salary_grade INT NOT NULL CHECK (salary_grade BETWEEN 1 AND 12),
    salary_coefficient NUMERIC(5,2) NOT NULL CHECK (salary_coefficient > 0),
    appointment_date DATE NULL,
    party_status NVARCHAR(30) DEFAULT 'None' CHECK (party_status IN ('Official', 'Probationary', 'None')),
    political_theory NVARCHAR(30) DEFAULT 'None' CHECK (political_theory IN ('High', 'Intermediate', 'Primary', 'None')),
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_cs_emp FOREIGN KEY (employee_id) REFERENCES dbo.employee_profiles(id) ON DELETE CASCADE,
    CONSTRAINT FK_cs_rank FOREIGN KEY (official_rank_code) REFERENCES dbo.position_ranks(rank_code)
);
GO

-- =============================================================================
-- PHÂN HỆ 3: DANH MỤC DÙNG CHUNG (MASTER CATALOGS)
-- =============================================================================

-- 16. Bảng nhóm chỉ số KPI
IF OBJECT_ID('dbo.kpi_categories', 'U') IS NULL
CREATE TABLE dbo.kpi_categories (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(100) NOT NULL,
    perspective NVARCHAR(30) NOT NULL CHECK (perspective IN ('Financial', 'Customer', 'InternalProcess', 'LearningGrowth', 'PublicServiceEthics')),
    target_sector NVARCHAR(30) DEFAULT 'All' NOT NULL CHECK (target_sector IN ('Enterprise', 'PublicSector', 'All')),
    display_order INT DEFAULT 0 NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL
);
GO

-- 17. Bảng đơn vị đo lường
IF OBJECT_ID('dbo.measurement_units', 'U') IS NULL
CREATE TABLE dbo.measurement_units (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(50) NOT NULL,
    symbol NVARCHAR(15) NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL
);
GO

-- 18. Bảng phương pháp đo lường
IF OBJECT_ID('dbo.measurement_methods', 'U') IS NULL
CREATE TABLE dbo.measurement_methods (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(100) NOT NULL,
    data_source NVARCHAR(100) NULL,
    is_deleted BIT DEFAULT 0 NOT NULL
);
GO

-- 19. Bảng ngân hàng chỉ số KPI chuẩn (Master KPI Bank)
IF OBJECT_ID('dbo.kpi_master_items', 'U') IS NULL
CREATE TABLE dbo.kpi_master_items (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    category_id BIGINT NOT NULL,
    unit_id BIGINT NOT NULL,
    method_id BIGINT NOT NULL,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NULL,
    scoring_formula NVARCHAR(30) DEFAULT 'HigherIsBetter' NOT NULL CHECK (scoring_formula IN ('HigherIsBetter', 'LowerIsBetter', 'TargetRange')),
    is_civil_servant_only BIT DEFAULT 0 NOT NULL,
    is_active BIT DEFAULT 1 NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT FK_kpi_master_category FOREIGN KEY (category_id) REFERENCES dbo.kpi_categories(id),
    CONSTRAINT FK_kpi_master_unit FOREIGN KEY (unit_id) REFERENCES dbo.measurement_units(id),
    CONSTRAINT FK_kpi_master_method FOREIGN KEY (method_id) REFERENCES dbo.measurement_methods(id)
);
GO

-- 20. Bảng thang điểm
IF OBJECT_ID('dbo.score_scales', 'U') IS NULL
CREATE TABLE dbo.score_scales (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(100) NOT NULL,
    min_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    max_score NUMERIC(5,2) DEFAULT 100.0 NOT NULL,
    is_default BIT DEFAULT 0 NOT NULL
);
GO

-- 21. Bảng mức xếp loại chất lượng (Nghị định 90/2020)
IF OBJECT_ID('dbo.classification_levels', 'U') IS NULL
CREATE TABLE dbo.classification_levels (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    scale_id BIGINT NOT NULL,
    code NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    min_percentage NUMERIC(5,2) NOT NULL,
    max_percentage NUMERIC(5,2) NOT NULL,
    requires_initiative BIT DEFAULT 0 NOT NULL,
    is_capped_at_20_percent BIT DEFAULT 0 NOT NULL,
    CONSTRAINT FK_classification_scale FOREIGN KEY (scale_id) REFERENCES dbo.score_scales(id) ON DELETE CASCADE
);
GO

-- 22. Bảng kỳ đánh giá hiệu suất
IF OBJECT_ID('dbo.evaluation_periods', 'U') IS NULL
CREATE TABLE dbo.evaluation_periods (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    scale_id BIGINT NOT NULL,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(150) NOT NULL,
    period_type NVARCHAR(20) NOT NULL CHECK (period_type IN ('Monthly', 'Quarterly', 'SemiAnnual', 'Annual')),
    year INT NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    self_deadline DATETIMEOFFSET NOT NULL,
    manager_deadline DATETIMEOFFSET NOT NULL,
    closing_date DATETIMEOFFSET NOT NULL,
    status NVARCHAR(20) DEFAULT 'Planning' NOT NULL CHECK (status IN ('Planning', 'Open', 'InEvaluation', 'Closing', 'Closed', 'Archived')),
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT FK_period_scale FOREIGN KEY (scale_id) REFERENCES dbo.score_scales(id)
);
GO

-- =============================================================================
-- PHÂN HỆ 4: THIẾT LẬP & GIAO CHỈ TIÊU KPI
-- =============================================================================

-- 23. Bảng bộ KPI mẫu theo chức danh
IF OBJECT_ID('dbo.kpi_templates', 'U') IS NULL
CREATE TABLE dbo.kpi_templates (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    position_id BIGINT NOT NULL,
    department_id BIGINT NULL,
    code NVARCHAR(30) NOT NULL UNIQUE,
    name NVARCHAR(150) NOT NULL,
    description NVARCHAR(MAX) NULL,
    status NVARCHAR(20) DEFAULT 'Active' NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT FK_template_pos FOREIGN KEY (position_id) REFERENCES dbo.positions(id) ON DELETE CASCADE,
    CONSTRAINT FK_template_dept FOREIGN KEY (department_id) REFERENCES dbo.departments(id)
);
GO

-- 24. Bảng chi tiết chỉ tiêu trong bộ mẫu
IF OBJECT_ID('dbo.kpi_template_details', 'U') IS NULL
CREATE TABLE dbo.kpi_template_details (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    template_id BIGINT NOT NULL,
    kpi_item_id BIGINT NOT NULL,
    target_value NUMERIC(15,2) DEFAULT 100.0 NOT NULL,
    min_threshold NUMERIC(15,2) DEFAULT 0.0 NULL,
    weight NUMERIC(5,2) NOT NULL CHECK (weight >= 5.0 AND weight <= 50.0),
    guidance_notes NVARCHAR(MAX) NULL,
    CONSTRAINT FK_templatedetail_template FOREIGN KEY (template_id) REFERENCES dbo.kpi_templates(id) ON DELETE CASCADE,
    CONSTRAINT FK_templatedetail_item FOREIGN KEY (kpi_item_id) REFERENCES dbo.kpi_master_items(id)
);
GO

-- 25. Bảng bản giao KPI cá nhân
IF OBJECT_ID('dbo.kpi_assignments', 'U') IS NULL
CREATE TABLE dbo.kpi_assignments (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    period_id BIGINT NOT NULL,
    employee_id BIGINT NOT NULL,
    approver_id BIGINT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    total_weight NUMERIC(5,2) NOT NULL CHECK (total_weight = 100.00),
    status NVARCHAR(30) DEFAULT 'Draft' NOT NULL CHECK (status IN ('Draft', 'Pending_Staff_Confirm', 'Pending_Approval', 'Approved_Locked', 'In_Evaluation', 'Completed', 'Archived')),
    committed_at DATETIMEOFFSET NULL,
    approved_at DATETIMEOFFSET NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    updated_at DATETIMEOFFSET NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT UQ_assignments_period_emp UNIQUE (period_id, employee_id),
    CONSTRAINT FK_assignments_period FOREIGN KEY (period_id) REFERENCES dbo.evaluation_periods(id),
    CONSTRAINT FK_assignments_emp FOREIGN KEY (employee_id) REFERENCES dbo.employee_profiles(id),
    CONSTRAINT FK_assignments_approver FOREIGN KEY (approver_id) REFERENCES dbo.users(id)
);
GO

-- 26. Bảng chi tiết chỉ tiêu được giao
IF OBJECT_ID('dbo.kpi_assignment_details', 'U') IS NULL
CREATE TABLE dbo.kpi_assignment_details (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    assignment_id BIGINT NOT NULL,
    kpi_item_id BIGINT NOT NULL,
    kpi_name NVARCHAR(255) NOT NULL,
    unit NVARCHAR(30) NOT NULL,
    target_value NUMERIC(15,2) NOT NULL,
    min_threshold NUMERIC(15,2) DEFAULT 0.0 NULL,
    max_cap NUMERIC(5,2) DEFAULT 150.0 NOT NULL,
    weight NUMERIC(5,2) NOT NULL CHECK (weight >= 5.0 AND weight <= 50.0),
    scoring_formula NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_assign_detail_assignment FOREIGN KEY (assignment_id) REFERENCES dbo.kpi_assignments(id) ON DELETE CASCADE,
    CONSTRAINT FK_assign_detail_item FOREIGN KEY (kpi_item_id) REFERENCES dbo.kpi_master_items(id)
);
GO

-- 27. Bảng tài liệu minh chứng số
IF OBJECT_ID('dbo.kpi_evidences', 'U') IS NULL
CREATE TABLE dbo.kpi_evidences (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    assignment_detail_id BIGINT NOT NULL,
    file_name NVARCHAR(255) NOT NULL,
    file_path NVARCHAR(500) NOT NULL,
    file_type NVARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL CHECK (file_size <= 26214400),
    description NVARCHAR(255) NULL,
    uploaded_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_evidence_detail FOREIGN KEY (assignment_detail_id) REFERENCES dbo.kpi_assignment_details(id) ON DELETE CASCADE
);
GO

-- =============================================================================
-- PHÂN HỆ 5: ĐÁNH GIÁ, CÔNG CHỨC & WORKFLOW
-- =============================================================================

-- 28. Bảng phiếu đánh giá hiệu suất tổng hợp
IF OBJECT_ID('dbo.evaluations', 'U') IS NULL
CREATE TABLE dbo.evaluations (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    assignment_id BIGINT NOT NULL UNIQUE,
    employee_id BIGINT NOT NULL,
    period_id BIGINT NOT NULL,
    self_total_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    manager_total_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    final_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    final_classification NVARCHAR(50) NULL,
    status NVARCHAR(30) DEFAULT 'Not_Started' NOT NULL CHECK (status IN ('Not_Started', 'Self_Assessing', 'Self_Submitted', 'Manager_Reviewing', 'Pending_Director_Approval', 'Final_Approved', 'Locked')),
    is_disputed BIT DEFAULT 0 NOT NULL,
    is_locked BIT DEFAULT 0 NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    updated_at DATETIMEOFFSET NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT FK_eval_assignment FOREIGN KEY (assignment_id) REFERENCES dbo.kpi_assignments(id),
    CONSTRAINT FK_eval_emp FOREIGN KEY (employee_id) REFERENCES dbo.employee_profiles(id),
    CONSTRAINT FK_eval_period FOREIGN KEY (period_id) REFERENCES dbo.evaluation_periods(id)
);
GO

-- 29. Bảng chi tiết điểm số từng chỉ tiêu
IF OBJECT_ID('dbo.evaluation_details', 'U') IS NULL
CREATE TABLE dbo.evaluation_details (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    evaluation_id BIGINT NOT NULL,
    assignment_detail_id BIGINT NOT NULL UNIQUE,
    actual_value NUMERIC(15,2) DEFAULT 0.0 NOT NULL,
    completion_rate NUMERIC(7,2) DEFAULT 0.0 NOT NULL,
    self_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    manager_score NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    score_gap NUMERIC(5,2) DEFAULT 0.0 NOT NULL,
    self_remarks NVARCHAR(MAX) NULL,
    manager_remarks NVARCHAR(MAX) NULL,
    score_gap_reason NVARCHAR(MAX) NULL,
    CONSTRAINT FK_eval_detail_eval FOREIGN KEY (evaluation_id) REFERENCES dbo.evaluations(id) ON DELETE CASCADE,
    CONSTRAINT FK_eval_detail_assign FOREIGN KEY (assignment_detail_id) REFERENCES dbo.kpi_assignment_details(id)
);
GO

-- 30. Bảng phân loại cán bộ công chức theo Nghị định 90/2020 & Nghị định 48/2023
IF OBJECT_ID('dbo.civil_servant_classifications', 'U') IS NULL
CREATE TABLE dbo.civil_servant_classifications (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    evaluation_id BIGINT NOT NULL UNIQUE,
    civil_servant_id BIGINT NOT NULL,
    political_ethics_score INT NOT NULL CHECK (political_ethics_score BETWEEN 0 AND 100),
    work_discipline_score INT NOT NULL CHECK (work_discipline_score BETWEEN 0 AND 100),
    duty_execution_score INT NOT NULL CHECK (duty_execution_score BETWEEN 0 AND 100),
    has_disciplinary_action BIT DEFAULT 0 NOT NULL,
    has_approved_initiative BIT DEFAULT 0 NOT NULL,
    collective_comments NVARCHAR(MAX) NULL,
    party_opinion NVARCHAR(MAX) NULL,
    proposed_rank NVARCHAR(50) NOT NULL,
    finalized_rank NVARCHAR(50) NOT NULL,
    approved_by BIGINT NULL,
    approved_at DATETIMEOFFSET NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_cs_class_eval FOREIGN KEY (evaluation_id) REFERENCES dbo.evaluations(id) ON DELETE CASCADE,
    CONSTRAINT FK_cs_class_profile FOREIGN KEY (civil_servant_id) REFERENCES dbo.civil_servant_profiles(id),
    CONSTRAINT FK_cs_class_approved_by FOREIGN KEY (approved_by) REFERENCES dbo.users(id)
);
GO

-- 31. Bảng đánh giá ngang hàng 360 độ
IF OBJECT_ID('dbo.peer_reviews', 'U') IS NULL
CREATE TABLE dbo.peer_reviews (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    evaluation_id BIGINT NOT NULL,
    reviewer_id BIGINT NOT NULL,
    behavior_score NUMERIC(5,2) NOT NULL,
    feedback_comments NVARCHAR(MAX) NULL,
    is_anonymous BIT DEFAULT 1 NOT NULL,
    submitted_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_peer_eval FOREIGN KEY (evaluation_id) REFERENCES dbo.evaluations(id) ON DELETE CASCADE,
    CONSTRAINT FK_peer_reviewer FOREIGN KEY (reviewer_id) REFERENCES dbo.employee_profiles(id)
);
GO

-- 32. Bảng quản lý tiến trình luồng duyệt (Workflow Engine)
IF OBJECT_ID('dbo.evaluation_workflows', 'U') IS NULL
CREATE TABLE dbo.evaluation_workflows (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    evaluation_id BIGINT NOT NULL UNIQUE,
    current_step NVARCHAR(50) NOT NULL,
    status NVARCHAR(30) DEFAULT 'Running' NOT NULL,
    CONSTRAINT FK_workflow_eval FOREIGN KEY (evaluation_id) REFERENCES dbo.evaluations(id) ON DELETE CASCADE
);
GO

-- 33. Bảng các bước phê duyệt định tuyến
IF OBJECT_ID('dbo.workflow_steps', 'U') IS NULL
CREATE TABLE dbo.workflow_steps (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    workflow_id BIGINT NOT NULL,
    step_order INT NOT NULL,
    step_name NVARCHAR(100) NOT NULL,
    required_role_id BIGINT NOT NULL,
    assigned_actor_id BIGINT NULL,
    action_taken NVARCHAR(30) DEFAULT 'Pending' NULL,
    comments NVARCHAR(MAX) NULL,
    completed_at DATETIMEOFFSET NULL,
    CONSTRAINT FK_step_workflow FOREIGN KEY (workflow_id) REFERENCES dbo.evaluation_workflows(id) ON DELETE CASCADE,
    CONSTRAINT FK_step_role FOREIGN KEY (required_role_id) REFERENCES dbo.roles(id),
    CONSTRAINT FK_step_actor FOREIGN KEY (assigned_actor_id) REFERENCES dbo.users(id)
);
GO

-- 34. Bảng nhật ký luân chuyển hồ sơ đánh giá
IF OBJECT_ID('dbo.workflow_history', 'U') IS NULL
CREATE TABLE dbo.workflow_history (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    evaluation_id BIGINT NOT NULL,
    actor_id BIGINT NOT NULL,
    from_state NVARCHAR(50) NOT NULL,
    to_state NVARCHAR(50) NOT NULL,
    comments NVARCHAR(MAX) NULL,
    action_time DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_history_eval FOREIGN KEY (evaluation_id) REFERENCES dbo.evaluations(id) ON DELETE CASCADE,
    CONSTRAINT FK_history_actor FOREIGN KEY (actor_id) REFERENCES dbo.users(id)
);
GO

-- 35. Bảng giải quyết khiếu nại kết quả đánh giá (BR-14)
IF OBJECT_ID('dbo.evaluation_disputes', 'U') IS NULL
CREATE TABLE dbo.evaluation_disputes (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    evaluation_id BIGINT NOT NULL,
    appellant_id BIGINT NOT NULL,
    reason NVARCHAR(MAX) NOT NULL,
    proposed_adjustment NVARCHAR(MAX) NULL,
    resolver_id BIGINT NULL,
    resolution_notes NVARCHAR(MAX) NULL,
    status NVARCHAR(20) DEFAULT 'Open' NOT NULL CHECK (status IN ('Open', 'UnderReview', 'Resolved', 'Rejected')),
    submitted_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    resolved_at DATETIMEOFFSET NULL,
    CONSTRAINT FK_dispute_eval FOREIGN KEY (evaluation_id) REFERENCES dbo.evaluations(id) ON DELETE CASCADE,
    CONSTRAINT FK_dispute_appellant FOREIGN KEY (appellant_id) REFERENCES dbo.employee_profiles(id),
    CONSTRAINT FK_dispute_resolver FOREIGN KEY (resolver_id) REFERENCES dbo.users(id)
);
GO

-- =============================================================================
-- PHÂN HỆ 6: BÁO CÁO, DASHBOARD & THÔNG BÁO
-- =============================================================================

-- 36. Bảng cấu hình bảng điều khiển người dùng
IF OBJECT_ID('dbo.dashboard_configs', 'U') IS NULL
CREATE TABLE dbo.dashboard_configs (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    widget_key NVARCHAR(50) NOT NULL,
    grid_x INT DEFAULT 0 NOT NULL,
    grid_y INT DEFAULT 0 NOT NULL,
    grid_width INT DEFAULT 4 NOT NULL,
    grid_height INT DEFAULT 3 NOT NULL,
    CONSTRAINT UQ_dashboard_user_widget UNIQUE (user_id, widget_key),
    CONSTRAINT FK_dash_users FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE CASCADE
);
GO

-- 37. Bảng kết xuất tài liệu báo cáo
IF OBJECT_ID('dbo.reports', 'U') IS NULL
CREATE TABLE dbo.reports (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    period_id BIGINT NOT NULL,
    generated_by BIGINT NOT NULL,
    report_type NVARCHAR(50) NOT NULL,
    title NVARCHAR(200) NOT NULL,
    parameters_json NVARCHAR(MAX) NULL,
    export_format NVARCHAR(10) NOT NULL CHECK (export_format IN ('XLSX', 'PDF', 'CSV')),
    file_url NVARCHAR(500) NOT NULL,
    generated_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_reports_period FOREIGN KEY (period_id) REFERENCES dbo.evaluation_periods(id),
    CONSTRAINT FK_reports_users FOREIGN KEY (generated_by) REFERENCES dbo.users(id)
);
GO

-- 38. Bảng thông báo chuông In-App
IF OBJECT_ID('dbo.notifications', 'U') IS NULL
CREATE TABLE dbo.notifications (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    recipient_id BIGINT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    type NVARCHAR(30) DEFAULT 'Info' NOT NULL,
    target_url NVARCHAR(255) NULL,
    is_read BIT DEFAULT 0 NOT NULL,
    read_at DATETIMEOFFSET NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_notif_recipient FOREIGN KEY (recipient_id) REFERENCES dbo.users(id) ON DELETE CASCADE
);
GO

-- 39. Bảng mẫu thông báo & mẫu email
IF OBJECT_ID('dbo.notification_templates', 'U') IS NULL
CREATE TABLE dbo.notification_templates (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    event_code NVARCHAR(50) NOT NULL UNIQUE,
    subject_template NVARCHAR(255) NOT NULL,
    body_template NVARCHAR(MAX) NOT NULL,
    channels NVARCHAR(50) DEFAULT 'IN_APP,EMAIL' NOT NULL,
    is_active BIT DEFAULT 1 NOT NULL
);
GO

-- 40. Bảng hàng đợi gửi email tự động
IF OBJECT_ID('dbo.email_queues', 'U') IS NULL
CREATE TABLE dbo.email_queues (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    to_email NVARCHAR(100) NOT NULL,
    subject NVARCHAR(255) NOT NULL,
    html_body NVARCHAR(MAX) NOT NULL,
    status NVARCHAR(20) DEFAULT 'Pending' NOT NULL CHECK (status IN ('Pending', 'Sent', 'Failed')),
    retry_count INT DEFAULT 0 NOT NULL,
    error_message NVARCHAR(MAX) NULL,
    scheduled_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    sent_at DATETIMEOFFSET NULL
);
GO

-- =============================================================================
-- PHÂN HỆ 7: QUẢN TRỊ NÂNG CAO, CHẤM CÔNG, MỤC TIÊU BGĐ & LƯƠNG 3P CÔNG CHỨC
-- =============================================================================

-- 41. Bảng menu điều hướng hệ thống (Menu Items - Dynamic Navigation do Admin quản lý)
IF OBJECT_ID('dbo.menu_items', 'U') IS NULL
CREATE TABLE dbo.menu_items (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    parent_id BIGINT NULL,
    title NVARCHAR(100) NOT NULL,
    icon NVARCHAR(50) NULL,
    url NVARCHAR(255) NULL,
    permission_code NVARCHAR(100) NULL,
    sort_order INT DEFAULT 0 NOT NULL,
    is_active BIT DEFAULT 1 NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    section_title NVARCHAR(100) NULL,
    CONSTRAINT FK_menu_items_parent FOREIGN KEY (parent_id) REFERENCES dbo.menu_items(id)
);
GO

-- 42. Bảng mục tiêu kéo giãn do Ban Giám đốc chỉ đạo (Director Targets - Stretch Goals)
IF OBJECT_ID('dbo.director_targets', 'U') IS NULL
CREATE TABLE dbo.director_targets (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    period_id BIGINT NOT NULL,
    department_id BIGINT NULL,
    target_name NVARCHAR(200) NOT NULL,
    target_value DECIMAL(18,2) NOT NULL,
    challenge_value DECIMAL(18,2) NOT NULL,
    target_unit NVARCHAR(50) NOT NULL,
    description NVARCHAR(MAX) NULL,
    created_by BIGINT NOT NULL,
    is_active BIT DEFAULT 1 NOT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    CONSTRAINT FK_director_targets_period FOREIGN KEY (period_id) REFERENCES dbo.evaluation_periods(id),
    CONSTRAINT FK_director_targets_department FOREIGN KEY (department_id) REFERENCES dbo.departments(id) ON DELETE SET NULL,
    CONSTRAINT FK_director_targets_user FOREIGN KEY (created_by) REFERENCES dbo.users(id)
);
GO

-- 43. Bảng chấm công & kỷ luật lao động hàng tháng (Attendance Records - Điểm chuyên cần 20%)
IF OBJECT_ID('dbo.attendance_records', 'U') IS NULL
CREATE TABLE dbo.attendance_records (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    employee_id BIGINT NOT NULL,
    [year] INT NOT NULL,
    [month] INT NOT NULL,
    standard_work_days INT DEFAULT 22 NOT NULL,
    actual_work_days INT DEFAULT 22 NOT NULL,
    paid_leave INT DEFAULT 0 NOT NULL,
    unpaid_leave INT DEFAULT 0 NOT NULL,
    sick_leave INT DEFAULT 0 NOT NULL,
    late_count INT DEFAULT 0 NOT NULL,
    early_leave_count INT DEFAULT 0 NOT NULL,
    unauthorized_absence INT DEFAULT 0 NOT NULL,
    missed_meetings INT DEFAULT 0 NOT NULL,
    discipline_violation BIT DEFAULT 0 NOT NULL,
    attendance_score DECIMAL(5,2) DEFAULT 20.00 NOT NULL,
    work_day_ratio DECIMAL(5,4) DEFAULT 1.0000 NOT NULL,
    notes NVARCHAR(MAX) NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    updated_at DATETIMEOFFSET NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT FK_attendance_records_employee FOREIGN KEY (employee_id) REFERENCES dbo.employee_profiles(id)
);
GO

-- 44. Bảng tính lương viên chức theo NĐ 204 & NĐ 73 + Hiệu quả KPI + Phiếu chi tiền mặt
IF OBJECT_ID('dbo.payrolls', 'U') IS NULL
CREATE TABLE dbo.payrolls (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    period_id BIGINT NOT NULL,
    employee_id BIGINT NOT NULL,
    evaluation_id BIGINT NULL,
    standard_work_days INT DEFAULT 22 NOT NULL,
    actual_work_days INT DEFAULT 22 NOT NULL,
    work_day_ratio DECIMAL(5,4) DEFAULT 1.0000 NOT NULL,
    attendance_score DECIMAL(5,2) DEFAULT 20.00 NOT NULL,
    base_salary_unit DECIMAL(18,2) DEFAULT 2340000.00 NOT NULL,
    salary_coefficient DECIMAL(5,2) DEFAULT 2.34 NOT NULL,
    base_salary DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    position_allowance DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    seniority_allowance DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    profession_allowance DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    other_allowance DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    allowances DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    kpi_score DECIMAL(5,2) DEFAULT 0.00 NOT NULL,
    kpi_classification NVARCHAR(50) DEFAULT '' NOT NULL,
    kpi_multiplier DECIMAL(5,2) DEFAULT 1.00 NOT NULL,
    p3_salary DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    bonus_amount DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    insurance_salary DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    bhxh_deduction DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    bhyt_deduction DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    bhtn_deduction DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    union_fee DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    deductions DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    taxable_income DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    personal_income_tax DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    gross_income DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    net_salary DECIMAL(18,2) DEFAULT 0.00 NOT NULL,
    voucher_code NVARCHAR(50) DEFAULT '' NOT NULL,
    payment_method NVARCHAR(50) DEFAULT N'Tiền mặt' NOT NULL,
    payment_status NVARCHAR(50) DEFAULT N'Chưa thanh toán' NOT NULL,
    paid_at DATETIMEOFFSET NULL,
    cashier_id BIGINT NULL,
    recipient_signature NVARCHAR(MAX) NULL,
    notes NVARCHAR(MAX) NULL,
    verification_hash NVARCHAR(255) NULL,
    created_at DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET() NOT NULL,
    created_by BIGINT NULL,
    updated_at DATETIMEOFFSET NULL,
    updated_by BIGINT NULL,
    is_deleted BIT DEFAULT 0 NOT NULL,
    CONSTRAINT FK_payrolls_period FOREIGN KEY (period_id) REFERENCES dbo.evaluation_periods(id),
    CONSTRAINT FK_payrolls_employee FOREIGN KEY (employee_id) REFERENCES dbo.employee_profiles(id),
    CONSTRAINT FK_payrolls_eval FOREIGN KEY (evaluation_id) REFERENCES dbo.evaluations(id) ON DELETE SET NULL,
    CONSTRAINT FK_payrolls_cashier FOREIGN KEY (cashier_id) REFERENCES dbo.users(id) ON DELETE SET NULL
);
GO

-- =============================================================================
-- THIẾT LẬP CHỈ MỤC TỐI ƯU HÓA HIỆU NĂNG TRUY VẤN (INDEXES)
-- =============================================================================

CREATE NONCLUSTERED INDEX idx_users_status ON dbo.users(status);
CREATE NONCLUSTERED INDEX idx_audit_time ON dbo.audit_logs(created_at);
CREATE NONCLUSTERED INDEX idx_audit_entity ON dbo.audit_logs(entity_name, entity_id);
CREATE NONCLUSTERED INDEX idx_departments_tree ON dbo.departments(tree_path);
CREATE NONCLUSTERED INDEX idx_emp_dept ON dbo.employee_profiles(department_id);
CREATE NONCLUSTERED INDEX idx_assign_emp ON dbo.kpi_assignments(employee_id);
CREATE NONCLUSTERED INDEX idx_assign_period ON dbo.kpi_assignments(period_id);
CREATE NONCLUSTERED INDEX idx_eval_emp ON dbo.evaluations(employee_id);
CREATE NONCLUSTERED INDEX idx_eval_period ON dbo.evaluations(period_id);
CREATE NONCLUSTERED INDEX idx_eval_status ON dbo.evaluations(status);
CREATE NONCLUSTERED INDEX idx_notif_recipient ON dbo.notifications(recipient_id, is_read);
CREATE NONCLUSTERED INDEX idx_email_status ON dbo.email_queues(status, scheduled_at);
CREATE NONCLUSTERED INDEX idx_menu_parent ON dbo.menu_items(parent_id, sort_order);
CREATE NONCLUSTERED INDEX idx_director_targets_period ON dbo.director_targets(period_id, department_id);
CREATE NONCLUSTERED INDEX idx_attendance_emp_time ON dbo.attendance_records(employee_id, [year], [month]);
CREATE NONCLUSTERED INDEX idx_payrolls_period_emp ON dbo.payrolls(period_id, employee_id);
GO

-- =============================================================================
-- TRIGGER NGHIỆP VỤ BẢO VỆ DỮ LIỆU BR-05 (T-SQL SYNTAX)
-- =============================================================================

IF OBJECT_ID('dbo.trg_check_score_gap', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_check_score_gap;
GO

CREATE TRIGGER dbo.trg_check_score_gap
ON dbo.evaluation_details
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 
        FROM inserted i
        WHERE ABS(i.self_score - i.manager_score) > 10.00
          AND (i.score_gap_reason IS NULL OR LEN(LTRIM(RTRIM(i.score_gap_reason))) < 50)
    )
    BEGIN
        RAISERROR(N'BR-05 Vi phạm: Quản lý chấm lệch trên 10 điểm so với tự chấm bắt buộc nhập lý do giải trình >= 50 ký tự.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO
