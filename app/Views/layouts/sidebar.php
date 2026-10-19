<?php
$currentUri = $_SERVER['REQUEST_URI'] ?? '/dashboard';
$userRole = $_SESSION['role'] ?? 'Employee';
?>
<aside class="sidebar">
    <div class="sidebar-header">
        <div class="logo-badge">KPI</div>
        <div class="logo-text">
            <h2>KPI MASTER</h2>
            <span>Doanh nghiệp & CBCC</span>
        </div>
    </div>

    <nav class="sidebar-nav">
        <div class="nav-section-title">Hệ thống</div>
        <a href="/dashboard" class="nav-link <?= str_starts_with($currentUri, '/dashboard') ? 'active' : '' ?>">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="3" width="7" height="7"></rect>
                <rect x="14" y="3" width="7" height="7"></rect>
                <rect x="14" y="14" width="7" height="7"></rect>
                <rect x="3" y="14" width="7" height="7"></rect>
            </svg>
            <span>Bàn làm việc</span>
        </a>

        <div class="nav-section-title">Quản lý Đánh giá KPI</div>
        <a href="/kpi/my" class="nav-link <?= str_starts_with($currentUri, '/kpi/my') ? 'active' : '' ?>">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                <polyline points="14 2 14 8 20 8"></polyline>
                <line x1="16" y1="13" x2="8" y2="13"></line>
                <line x1="16" y1="17" x2="8" y2="17"></line>
            </svg>
            <span>Bản KPI của tôi</span>
        </a>

        <a href="/evaluation/self-assess" class="nav-link <?= str_starts_with($currentUri, '/evaluation/self-assess') ? 'active' : '' ?>">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 20h9"></path>
                <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path>
            </svg>
            <span>Tự chấm điểm KPI</span>
        </a>

        <?php if (in_array($userRole, ['Manager', 'Director', 'HR Admin', 'Super Admin'])): ?>
            <a href="/kpi/assign" class="nav-link <?= str_starts_with($currentUri, '/kpi/assign') ? 'active' : '' ?>">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon>
                </svg>
                <span>Thiết lập & Giao KPI</span>
            </a>
        <?php endif; ?>

        <?php if (in_array($userRole, ['Manager', 'Director', 'HR Admin', 'Super Admin', 'Accountant'])): ?>
            <a href="/kpi/assignments" class="nav-link <?= str_starts_with($currentUri, '/kpi/assignments') || str_starts_with($currentUri, '/kpi/view') ? 'active' : '' ?>">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"></path>
                    <rect x="8" y="2" width="8" height="4" rx="1" ry="1"></rect>
                </svg>
                <span>Danh sách bản giao KPI</span>
            </a>
        <?php endif; ?>

        <div class="nav-section-title">Tài chính & Tiền lương</div>
        <?php if (in_array($userRole, ['Accountant', 'Director', 'Super Admin'])): ?>
            <a href="/payroll" class="nav-link <?= (str_starts_with($currentUri, '/payroll') && !str_starts_with($currentUri, '/payroll/my-payslip')) ? 'active' : '' ?>">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="2" y="4" width="20" height="16" rx="2"></rect>
                    <line x1="12" y1="11" x2="12" y2="17"></line>
                    <line x1="9" y1="14" x2="15" y2="14"></line>
                </svg>
                <span>Bảng tính lương KPI (TM)</span>
            </a>

            <a href="/payroll/cash-sheet" target="_blank" class="nav-link">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                    <polyline points="14 2 14 8 20 8"></polyline>
                    <line x1="16" y1="13" x2="8" y2="13"></line>
                    <line x1="16" y1="17" x2="8" y2="17"></line>
                </svg>
                <span>Bảng ký nhận tiền mặt</span>
            </a>
        <?php endif; ?>

        <a href="/payroll/my-payslip" class="nav-link <?= str_starts_with($currentUri, '/payroll/my-payslip') ? 'active' : '' ?>">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"></circle>
                <path d="M12 6v6l4 2"></path>
            </svg>
            <span>Phiếu lương TM của tôi</span>
        </a>

        <div class="nav-section-title">Công chức & Nhà nước</div>
        <a href="/civil-servant" class="nav-link <?= str_starts_with($currentUri, '/civil-servant') ? 'active' : '' ?>">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                <circle cx="9" cy="7" r="4"></circle>
                <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
            </svg>
            <span>Nghị định 90 & 48/CP</span>
        </a>

        <div class="nav-section-title">Quản trị & Báo cáo</div>
        <a href="/catalogs" class="nav-link <?= str_starts_with($currentUri, '/catalogs') ? 'active' : '' ?>">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="8" y1="6" x2="21" y2="6"></line>
                <line x1="8" y1="12" x2="21" y2="12"></line>
                <line x1="8" y1="18" x2="21" y2="18"></line>
                <line x1="3" y1="6" x2="3.01" y2="6"></line>
                <line x1="3" y1="12" x2="3.01" y2="12"></line>
                <line x1="3" y1="18" x2="3.01" y2="18"></line>
            </svg>
            <span>Ngân hàng Chỉ số KPI</span>
        </a>

        <a href="/reports" class="nav-link <?= str_starts_with($currentUri, '/reports') ? 'active' : '' ?>">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="18" y1="20" x2="18" y2="10"></line>
                <line x1="12" y1="20" x2="12" y2="4"></line>
                <line x1="6" y1="20" x2="6" y2="14"></line>
            </svg>
            <span>Báo cáo & Phân bổ Gauss</span>
        </a>
    </nav>

    <div class="sidebar-user">
        <div class="avatar">
            <?= strtoupper(substr($_SESSION['full_name'] ?? 'U', 0, 1)) ?>
        </div>
        <div class="user-info">
            <div class="user-name"><?= htmlspecialchars($_SESSION['full_name'] ?? 'Khách') ?></div>
            <div class="user-role"><?= htmlspecialchars($_SESSION['role'] ?? 'Employee') ?></div>
        </div>
    </div>
</aside>
