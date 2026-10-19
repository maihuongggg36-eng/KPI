<?php
$pageTitle = 'Bàn làm việc Nhân viên | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Xin chào, <?= htmlspecialchars($_SESSION['full_name'] ?? 'Đồng chí') ?>!</h1>
        <p>Theo dõi chỉ tiêu hiệu suất công tác cá nhân và thực hiện quy trình đánh giá kỳ này</p>
    </div>
    <div class="page-actions">
        <?php if (!empty($myAssignment)): ?>
            <a href="/evaluation/self-assess" class="btn btn-primary">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 20h9"></path><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path></svg>
                <span>Thực hiện Tự đánh giá</span>
            </a>
            <a href="/civil-servant/form?user_id=<?= $_SESSION['user_id'] ?>" class="btn btn-secondary">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 9V2h12v7"></path><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
                <span>Xem Phiếu Mẫu 02 (NĐ 90)</span>
            </a>
        <?php endif; ?>
    </div>
</div>

<?php if (empty($myAssignment)): ?>
    <div class="card" style="text-align: center; padding: 48px;">
        <div style="font-size: 48px; margin-bottom: 16px;">📋</div>
        <h2 style="font-size: 20px; font-weight: 700; margin-bottom: 8px;">Bạn chưa được giao bản chỉ tiêu KPI</h2>
        <p style="color: var(--text-secondary); max-width: 500px; margin: 0 auto 20px;">
            Trưởng phòng hoặc Quản lý trực tiếp chưa tạo bản giao KPI cho kỳ này. Vui lòng liên hệ quản lý để được thiết lập chỉ tiêu.
        </p>
    </div>
<?php else: ?>
    <!-- Tổng kết trạng thái bản KPI -->
    <div class="stats-grid">
        <div class="stat-card">
            <div class="stat-info">
                <p>Tổng trọng số KPI</p>
                <h3><?= $myAssignment['total_weight'] ?>%</h3>
                <span style="color: var(--success); font-weight: 600;">Chuẩn 100.0%</span>
            </div>
            <div class="stat-icon primary">⚖</div>
        </div>

        <div class="stat-card">
            <div class="stat-info">
                <p>Số chỉ tiêu công việc</p>
                <h3><?= count($myAssignment['details'] ?? []) ?></h3>
                <span>Chỉ tiêu định lượng & định tính</span>
            </div>
            <div class="stat-icon primary">🎯</div>
        </div>

        <div class="stat-card">
            <div class="stat-info">
                <p>Điểm tự đánh giá</p>
                <h3><?= $myEvaluation['self_total_score'] ?? '0.0' ?></h3>
                <span>Thang điểm 100</span>
            </div>
            <div class="stat-icon warning">✍</div>
        </div>

        <div class="stat-card">
            <div class="stat-info">
                <p>Kết quả chính thức</p>
                <h3 style="font-size: 20px; color: var(--primary);">
                    <?= !empty($myEvaluation['final_classification']) ? htmlspecialchars($myEvaluation['final_classification']) : 'Chờ phê chuẩn' ?>
                </h3>
                <span>Điểm: <?= $myEvaluation['final_score'] ?? '---' ?></span>
            </div>
            <div class="stat-icon success">🏅</div>
        </div>
    </div>

    <!-- Bảng chi tiết các mục tiêu KPI -->
    <div class="card">
        <div class="card-header">
            <div>
                <h2><?= htmlspecialchars($myAssignment['title']) ?></h2>
                <span style="font-size: 13px; color: var(--text-muted);">
                    Người giao: <?= htmlspecialchars($myAssignment['approver_name'] ?? 'Quản lý') ?> | 
                    Trạng thái bản giao: <span class="badge badge-success"><?= htmlspecialchars($myAssignment['status']) ?></span>
                </span>
            </div>
            <a href="/evaluation/self-assess" class="btn btn-primary btn-sm">Chấm điểm ngay</a>
        </div>
        <div class="card-body" style="padding: 0;">
            <div class="table-responsive">
                <table class="table">
                    <thead>
                        <tr>
                            <th style="width: 50px;">STT</th>
                            <th>Mục tiêu / Chỉ số KPI</th>
                            <th>Đơn vị tính</th>
                            <th>Chỉ tiêu giao</th>
                            <th>Trần tối đa (MaxCap)</th>
                            <th>Trọng số</th>
                            <th>Công thức</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php $idx = 1; foreach ($myAssignment['details'] as $item): ?>
                            <tr>
                                <td><?= $idx++ ?></td>
                                <td>
                                    <strong><?= htmlspecialchars($item['kpi_name']) ?></strong>
                                </td>
                                <td><?= htmlspecialchars($item['unit']) ?></td>
                                <td><strong><?= $item['target_value'] ?></strong></td>
                                <td><?= $item['max_cap'] ?>%</td>
                                <td>
                                    <span class="badge badge-primary"><?= $item['weight'] ?>%</span>
                                </td>
                                <td>
                                    <span class="badge badge-secondary"><?= $item['scoring_formula'] === 'HigherIsBetter' ? 'Càng cao càng tốt' : 'Càng thấp càng tốt' ?></span>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
<?php endif; ?>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
