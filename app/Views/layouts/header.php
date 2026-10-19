<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= htmlspecialchars($pageTitle ?? 'Hệ thống Quản lý & Đánh giá KPI') ?> | KPI System</title>
    <meta name="description" content="Hệ thống đánh giá hiệu suất KPI cho Doanh nghiệp và Cán bộ Công chức theo Nghị định 90/2020/NĐ-CP và Nghị định 48/2023/NĐ-CP">
    <link rel="stylesheet" href="/css/style.css">
</head>
<body>
<div class="app-container">
    <?php require BASE_PATH . '/app/Views/layouts/sidebar.php'; ?>
    <div class="main-content">
        <header class="topbar">
            <div class="topbar-left">
                <div class="period-badge">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="12" cy="12" r="10"></circle>
                        <polyline points="12 6 12 12 16 14"></polyline>
                    </svg>
                    <span><?= htmlspecialchars($_SESSION['period_name'] ?? 'Kỳ đánh giá Quý 1/2026') ?></span>
                </div>
            </div>
            <div class="topbar-right">
                <div class="role-switcher">
                    <span>Đổi vai trò test:</span>
                    <select id="role-quick-switcher">
                        <option value="director" <?= ($_SESSION['username'] ?? '') === 'director' ? 'selected' : '' ?>>Trần Lãnh Đạo (Giám đốc)</option>
                        <option value="manager" <?= ($_SESSION['username'] ?? '') === 'manager' ? 'selected' : '' ?>>Lê Trưởng Phòng (Quản lý)</option>
                        <option value="officer1" <?= ($_SESSION['username'] ?? '') === 'officer1' ? 'selected' : '' ?>>Nguyễn Chuyên Viên (Nhân viên)</option>
                        <option value="hr_admin" <?= ($_SESSION['username'] ?? '') === 'hr_admin' ? 'selected' : '' ?>>Vũ Thị Nhân Sự (Cán bộ TCCB)</option>
                        <option value="accountant" <?= ($_SESSION['username'] ?? '') === 'accountant' ? 'selected' : '' ?>>Hoàng Kế Toán (Kế toán viên)</option>
                        <option value="admin" <?= ($_SESSION['username'] ?? '') === 'admin' ? 'selected' : '' ?>>Admin Quản Trị Hệ Thống</option>
                    </select>
                </div>
                <a href="/logout" class="btn btn-secondary btn-sm" title="Đăng xuất">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
                        <polyline points="16 17 21 12 16 7"></polyline>
                        <line x1="21" y1="12" x2="9" y2="12"></line>
                    </svg>
                    <span>Thoát</span>
                </a>
            </div>
        </header>

        <main class="page-body">
            <?php if (!empty($_SESSION['flash_message'])): ?>
                <div class="alert alert-success">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
                    <div><?= $_SESSION['flash_message'] ?></div>
                </div>
                <?php unset($_SESSION['flash_message']); ?>
            <?php endif; ?>

            <?php if (!empty($_SESSION['error'])): ?>
                <div class="alert alert-danger">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
                    <div><?= $_SESSION['error'] ?></div>
                </div>
                <?php unset($_SESSION['error']); ?>
            <?php endif; ?>
