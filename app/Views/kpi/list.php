<?php
$pageTitle = 'Danh sách bản giao KPI | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Danh sách Bản giao chỉ tiêu KPI</h1>
        <p>Kỳ: <strong><?= htmlspecialchars($period['name']) ?></strong></p>
    </div>
    <div class="page-actions" style="display: flex; gap: 8px;">
        <?php $userRole = $_SESSION['role'] ?? 'Employee'; ?>
        <?php if (in_array($userRole, ['Accountant', 'Director', 'Super Admin'])): ?>
            <a href="/payroll" class="btn btn-primary" style="background: linear-gradient(135deg, #059669 0%, #10b981 100%); border-color: #10b981;">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="4" width="20" height="16" rx="2"></rect><line x1="12" y1="11" x2="12" y2="17"></line><line x1="9" y1="14" x2="15" y2="14"></line></svg>
                <span>💵 Quản lý Lương Tiền Mặt</span>
            </a>
        <?php endif; ?>
        <?php if (in_array($userRole, ['Manager', 'Director', 'HR Admin', 'Super Admin'])): ?>
            <a href="/kpi/assign" class="btn btn-secondary">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
                <span>+ Thiết lập bản giao mới</span>
            </a>
        <?php endif; ?>
    </div>
</div>

<div class="card">
    <div class="card-body" style="padding: 0;">
        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th>Cán bộ / Nhân viên</th>
                        <th>Phòng ban</th>
                        <th>Chức danh</th>
                        <th>Tiêu đề Bản KPI</th>
                        <th>Tổng trọng số</th>
                        <th>Kết quả KPI</th>
                        <th>Trạng thái</th>
                        <th style="text-align: right;">Thao tác</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (!empty($assignments)): ?>
                        <?php foreach ($assignments as $a): ?>
                            <tr>
                                <td>
                                    <strong><?= htmlspecialchars($a['employee_name'] ?? '') ?></strong>
                                    <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($a['employee_code'] ?? '') ?></div>
                                </td>
                                <td><?= htmlspecialchars($a['department_name'] ?? '') ?></td>
                                <td><?= htmlspecialchars($a['position_name'] ?? '') ?></td>
                                <td>
                                    <a href="/kpi/view?assignment_id=<?= $a['id'] ?>" style="color: var(--primary); font-weight: 600; text-decoration: none;">
                                        <?= htmlspecialchars($a['title'] ?? '') ?>
                                    </a>
                                </td>
                                <td>
                                    <span class="badge badge-success"><?= $a['total_weight'] ?? 100 ?>%</span>
                                </td>
                                <td>
                                    <?php if (!empty($a['final_score'])): ?>
                                        <div style="font-weight: 700; color: var(--primary); font-size: 13.5px;">
                                            <?= number_format($a['final_score'], 1) ?> điểm
                                        </div>
                                        <div style="font-size: 11px; color: var(--text-secondary);"><?= htmlspecialchars($a['final_classification'] ?? '') ?></div>
                                    <?php elseif (!empty($a['self_total_score'])): ?>
                                        <span class="badge badge-warning">Tự chấm: <?= number_format($a['self_total_score'], 1) ?></span>
                                    <?php else: ?>
                                        <span style="color: var(--text-muted); font-size: 12px;">Chưa nộp</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <span class="badge badge-primary"><?= htmlspecialchars($a['status']) ?></span>
                                </td>
                                <td style="text-align: right;">
                                    <div style="display: inline-flex; gap: 6px;">
                                        <a href="/kpi/view?assignment_id=<?= $a['id'] ?>" class="btn btn-secondary btn-sm" title="Xem chi tiết chỉ tiêu KPI">
                                            Xem KPI
                                        </a>
                                        <?php if (in_array($userRole, ['Manager', 'Director', 'Super Admin'])): ?>
                                            <a href="/evaluation/manager-review?assignment_id=<?= $a['id'] ?>" class="btn btn-primary btn-sm">
                                                Thẩm định
                                            </a>
                                        <?php endif; ?>
                                        <?php if (in_array($userRole, ['Accountant', 'Director', 'Super Admin'])): ?>
                                            <a href="/payroll?period_id=<?= $a['period_id'] ?>" class="btn btn-sm" style="background: #10b981; color: white;" title="Tính lương tiền mặt theo kết quả KPI">
                                                💵 Tính lương
                                            </a>
                                        <?php endif; ?>
                                    </div>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    <?php else: ?>
                        <tr><td colspan="8" style="text-align: center; padding: 32px; color: var(--text-muted);">Chưa có bản giao KPI nào trong kỳ này.</td></tr>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
