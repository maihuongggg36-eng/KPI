<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Đăng nhập Hệ thống Quản lý KPI & Cán bộ Công chức</title>
    <meta name="description" content="Phần mềm Đánh giá KPI Doanh nghiệp và Cán bộ Công chức theo Nghị định 90/2020/NĐ-CP & 48/2023/NĐ-CP">
    <link rel="stylesheet" href="/css/style.css">
    <style>
        body {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            background: radial-gradient(circle at top right, #1e3a8a, #0f172a 70%);
            padding: 24px;
        }
        .login-box {
            width: 100%;
            max-width: 480px;
            background: rgba(255, 255, 255, 0.98);
            backdrop-filter: blur(16px);
            border-radius: var(--radius-xl);
            padding: 40px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.4);
            border: 1px solid rgba(255, 255, 255, 0.2);
        }
        .login-header {
            text-align: center;
            margin-bottom: 32px;
        }
        .login-badge {
            width: 64px;
            height: 64px;
            margin: 0 auto 16px;
            border-radius: var(--radius-lg);
            background: var(--primary-gradient);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 28px;
            font-weight: 800;
            color: white;
            box-shadow: 0 10px 25px rgba(37, 99, 235, 0.4);
        }
        .quick-roles {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 10px;
            margin-top: 24px;
            padding-top: 20px;
            border-top: 1px solid var(--border);
        }
        .btn-role {
            font-size: 11.5px;
            padding: 10px 12px;
            text-align: left;
            border: 1px solid var(--border);
            border-radius: var(--radius-md);
            background: #f8fafc;
            cursor: pointer;
            transition: var(--transition);
        }
        .btn-role:hover {
            background: var(--primary-light);
            border-color: var(--primary);
            color: var(--primary);
        }
        .btn-role strong {
            display: block;
            font-size: 12.5px;
            color: var(--text-primary);
        }
    </style>
</head>
<body>

<div class="login-box">
    <div class="login-header">
        <div class="login-badge">KPI</div>
        <h1 style="font-size: 22px; font-weight: 800; color: var(--text-primary);">HỆ THỐNG ĐÁNH GIÁ KPI</h1>
        <p style="font-size: 13px; color: var(--text-secondary); margin-top: 6px;">Doanh nghiệp & Cán bộ Công chức (NĐ 90/2020 & 48/2023)</p>
    </div>

    <?php if (!empty($error)): ?>
        <div class="alert alert-danger" style="margin-bottom: 20px;">
            <span><?= htmlspecialchars($error) ?></span>
        </div>
    <?php endif; ?>

    <form method="POST" action="/login">
        <div class="form-group">
            <label for="username">Tên đăng nhập / Mã tài khoản</label>
            <input type="text" id="username" name="username" class="form-control" placeholder="Nhập tài khoản..." required autofocus>
        </div>

        <div class="form-group">
            <label for="password">Mật khẩu</label>
            <input type="password" id="password" name="password" class="form-control" placeholder="Mật khẩu mặc định: 123456" required>
        </div>

        <button type="submit" class="btn btn-primary" style="width: 100%; justify-content: center; padding: 12px; font-size: 15px; margin-top: 8px;">
            Đăng nhập Hệ thống
        </button>
    </form>

    <div style="margin-top: 24px; text-align: center;">
        <span style="font-size: 11px; text-transform: uppercase; font-weight: 700; color: var(--text-muted); letter-spacing: 0.05em;">
            Đăng nhập nhanh theo vai trò trải nghiệm:
        </span>
    </div>

    <div class="quick-roles">
        <a href="/login?quick_switch=director" class="btn-role" style="text-decoration: none;">
            <strong>Trần Lãnh Đạo</strong>
            <span>Giám đốc Sở</span>
        </a>
        <a href="/login?quick_switch=manager" class="btn-role" style="text-decoration: none;">
            <strong>Lê Trưởng Phòng</strong>
            <span>TP Hợp tác Quốc tế</span>
        </a>
        <a href="/login?quick_switch=officer1" class="btn-role" style="text-decoration: none;">
            <strong>Nguyễn Chuyên Viên</strong>
            <span>Công chức HTQT</span>
        </a>
        <a href="/login?quick_switch=hr_admin" class="btn-role" style="text-decoration: none;">
            <strong>Vũ Thị Nhân Sự</strong>
            <span>Cán bộ TCCB</span>
        </a>
        <a href="/login?quick_switch=accountant" class="btn-role" style="text-decoration: none; border-color: #86efac; background: #f0fdf4;">
            <strong style="color: #15803d;">Hoàng Kế Toán</strong>
            <span style="color: #166534;">Kế toán viên (Lương & TM)</span>
        </a>
    </div>
</div>

</body>
</html>
