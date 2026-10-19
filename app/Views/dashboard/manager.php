<?php
$pageTitle = 'Bàn làm việc Quản lý / Trưởng phòng | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Bàn làm việc Trưởng phòng / Quản lý</h1>
        <p>Quản lý giao chỉ tiêu KPI, theo dõi tiến độ và chấm thẩm định nhân viên cấp dưới</p>
    </div>
    <div class="page-actions">
        <a href="/kpi/assign" class="btn btn-primary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
            <span>Giao KPI Mới</span>
        </a>
    </div>
</div>

<div class="stats-grid">
    <div class="stat-card">
        <div class="stat-info">
            <p>Nhân sự phòng ban</p>
            <h3><?= count($deptStaff ?? []) ?></h3>
            <span>Cán bộ, nhân viên trực thuộc</span>
        </div>
        <div class="stat-icon primary">👥</div>
    </div>

    <div class="stat-card">
        <div class="stat-info">
            <p>Hồ sơ đã giao KPI</p>
            <h3><?= count($recentAssignments ?? []) ?></h3>
            <span>Quý 1/2026</span>
        </div>
        <div class="stat-icon success">📑</div>
    </div>

    <div class="stat-card">
        <div class="stat-info">
            <p>Cần chấm thẩm định</p>
            <h3>
                <?php
                    $pendingReview = 0;
                    foreach ($recentAssignments ?? [] as $ra) {
                        if (($ra['eval_status'] ?? '') === 'Submitted') $pendingReview++;
                    }
                    echo $pendingReview;
                ?>
            </h3>
            <span style="color: var(--warning); font-weight: 600;">Nhân viên đã nộp</span>
        </div>
        <div class="stat-icon warning">✍</div>
    </div>

    <div class="stat-card">
        <div class="stat-info">
            <p>Đã hoàn tất thẩm định</p>
            <h3>
                <?php
                    $reviewed = 0;
                    foreach ($recentAssignments ?? [] as $ra) {
                        if (in_array($ra['eval_status'] ?? '', ['Manager_Reviewed', 'Completed'])) $reviewed++;
                    }
                    echo $reviewed;
                ?>
            </h3>
            <span>Chờ cấp trên phê chuẩn</span>
        </div>
        <div class="stat-icon success">✓</div>
    </div>
</div>

<div class="card">
    <div class="card-header">
        <h2>Danh sách Nhân viên và Bản KPI trong kỳ</h2>
        <a href="/kpi/assign" class="btn btn-secondary btn-sm">+ Giao chỉ tiêu</a>
    </div>
    <div class="card-body" style="padding: 0;">
        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th>Họ tên Nhân viên</th>
                        <th>Chức danh</th>
                        <th>Bản KPI</th>
                        <th>Trọng số</th>
                        <th>Điểm Tự chấm</th>
                        <th>Trạng thái Đánh giá</th>
                        <th>Thao tác</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (!empty($recentAssignments)): ?>
                        <?php foreach ($recentAssignments as $row): ?>
                            <tr>
                                <td>
                                    <strong><?= htmlspecialchars($row['employee_name'] ?? '') ?></strong>
                                    <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($row['employee_code'] ?? '') ?></div>
                                </td>
                                <td><?= htmlspecialchars($row['position_name'] ?? '') ?></td>
                                <td><?= htmlspecialchars($row['title'] ?? '') ?></td>
                                <td>
                                    <span class="badge badge-success"><?= $row['total_weight'] ?? 100 ?>%</span>
                                </td>
                                <td>
                                    <?= !empty($row['self_total_score']) ? '<strong>' . $row['self_total_score'] . '</strong>' : '<span style="color: var(--text-muted);">Chưa nộp</span>' ?>
                                </td>
                                <td>
                                    <?php 
                                    $eStatus = $row['eval_status'] ?? 'Chưa đánh giá';
                                    if ($eStatus === 'Completed'): ?>
                                        <span class="badge badge-success">Đã hoàn tất</span>
                                    <?php elseif ($eStatus === 'Manager_Reviewed'): ?>
                                        <span class="badge badge-primary">Đã thẩm định (<?= $row['manager_total_score'] ?? 0 ?> đ)</span>
                                    <?php elseif ($eStatus === 'Submitted'): ?>
                                        <span class="badge badge-warning">Cần thẩm định</span>
                                    <?php else: ?>
                                        <span class="badge badge-secondary"><?= htmlspecialchars($eStatus) ?></span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <a href="/evaluation/manager-review?assignment_id=<?= $row['id'] ?>" class="btn btn-primary btn-sm">
                                        Thẩm định điểm
                                    </a>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    <?php else: ?>
                        <tr><td colspan="7" style="text-align: center; color: var(--text-muted); padding: 32px;">Chưa có bản giao KPI nào.</td></tr>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
