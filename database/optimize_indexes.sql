-- =========================================================================================
-- HỌC VIỆN HÀNH CHÍNH VÀ QUẢN TRỊ CÔNG
-- HỆ THỐNG QUẢN LÝ VÀ ĐÁNH GIÁ KPI CÁN BỘ, GIẢNG VIÊN
-- TỐI ƯU HÓA HIỆU NĂNG TRUY VẤN CƠ SỞ DỮ LIỆU (INDEX OPTIMIZATION & TUNING)
-- =========================================================================================

-- 1. Bảng evaluations (Truy vấn theo Kỳ đánh giá, Nhân viên, và Trạng thái)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Evaluations_Period_Employee_Status' AND object_id = OBJECT_ID('evaluations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Evaluations_Period_Employee_Status 
    ON evaluations (period_id, employee_id, status)
    INCLUDE (total_kpi_score, final_score, classification, is_disputed);
END
GO

-- 2. Bảng kpi_assignments (Truy vấn danh sách KPI được giao theo phiếu đánh giá)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KpiAssignments_Evaluation_Status' AND object_id = OBJECT_ID('kpi_assignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_KpiAssignments_Evaluation_Status
    ON kpi_assignments (evaluation_id, status)
    INCLUDE (weight, target_value, actual_value, score, is_deleted);
END
GO

-- 3. Bảng civil_servant_classifications (Truy vấn theo dõi Nghị định 90 & Trần 20%)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CivilServant_Period_Classification' AND object_id = OBJECT_ID('civil_servant_classifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CivilServant_Period_Classification
    ON civil_servant_classifications (period_id, classification_level_id, department_id)
    INCLUDE (employee_id, final_score, is_approved, council_decision_number);
END
GO

-- 4. Bảng payrolls (Truy vấn tính lương P3 và chi trả tiền mặt)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payrolls_Period_Employee_Status' AND object_id = OBJECT_ID('payrolls'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payrolls_Period_Employee_Status
    ON payrolls (period_id, employee_id, payment_status)
    INCLUDE (base_salary, salary_coefficient, kpi_score, kpi_coefficient, performance_salary_p3, net_income);
END
GO

-- 5. Bảng notifications (Truy vấn chuông thông báo Real-time và trạng thái đã đọc)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_User_IsRead_CreatedAt' AND object_id = OBJECT_ID('notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_User_IsRead_CreatedAt
    ON notifications (user_id, is_read, created_at DESC)
    INCLUDE (title, type, target_url);
END
GO

-- 6. Bảng email_queues (Truy vấn quét hàng đợi gửi thư nền)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EmailQueues_Status_ScheduledAt' AND object_id = OBJECT_ID('email_queues'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmailQueues_Status_ScheduledAt
    ON email_queues (status, scheduled_at)
    INCLUDE (retry_count, max_retries, recipient_email, subject);
END
GO

-- 7. Bảng audit_logs (Truy vấn tra cứu lịch sử đăng nhập & kiểm toán an toàn)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_User_Timestamp' AND object_id = OBJECT_ID('audit_logs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_User_Timestamp
    ON audit_logs (user_id, timestamp DESC)
    INCLUDE (action, entity_name, ip_address, user_agent);
END
GO

-- 8. Bảng kpi_master_items (Truy vấn ngân hàng chỉ tiêu chuẩn)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KpiMasterItems_Category_Code' AND object_id = OBJECT_ID('kpi_master_items'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_KpiMasterItems_Category_Code
    ON kpi_master_items (category_id, is_deleted)
    INCLUDE (code, name, unit_id, method_id, is_civil_servant_only, is_active);
END
GO

-- 9. Bảng employee_profiles (Truy vấn theo phòng ban và mã nhân sự)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EmployeeProfiles_Dept_StaffCode' AND object_id = OBJECT_ID('employee_profiles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeProfiles_Dept_StaffCode
    ON employee_profiles (department_id, is_deleted)
    INCLUDE (staff_code, full_name, email, position_id, user_id);
END
GO

-- 10. Bảng departments (Truy vấn cây phòng ban theo TreePath & Parent)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Departments_Parent_TreePath' AND object_id = OBJECT_ID('departments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Departments_Parent_TreePath
    ON departments (parent_id, is_deleted)
    INCLUDE (code, name, level, tree_path);
END
GO

PRINT 'Tất cả các chỉ mục Non-clustered đã được tối ưu hóa thành công trên cơ sở dữ liệu KPI_System!'
