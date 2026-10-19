<?php
$pageTitle = 'Chi tiết Bản giao & Đánh giá KPI | ' . ($assignment['employee_name'] ?? '');
require BASE_PATH . '/app/Views/layouts/header.php';
$userRole = $_SESSION['role'] ?? 'Employee';
?>

<div class="page-header">
    <div class="page-title">
        <div style="margin-bottom: 6px;">
            <a href="/kpi/assignments" style="color: var(--text-secondary); text-decoration: none; font-size: 13px; display: inline-flex; align-items: center; gap: 4px;">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"></polyline></svg>
                Quay lại Danh sách Bản giao KPI
            </a>
        </div>
        <h1>Chi tiết Chỉ tiêu & Đánh giá Hiệu suất KPI</h1>
        <p>Cán bộ: <strong><?= htmlspecialchars($assignment['employee_name']) ?></strong> — Kỳ: <strong><?= htmlspecialchars($assignment['period_name'] ?? $period['name']) ?></strong></p>
    </div>
    <div class="page-actions" style="display: flex; gap: 10px;">
        <?php if (in_array($userRole, ['Accountant', 'Director', 'Super Admin'])): ?>
            <a href="/payroll?period_id=<?= $assignment['period_id'] ?>" class="btn btn-primary" style="background: linear-gradient(135deg, #059669 0%, #10b981 100%); border-color: #10b981;">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="4" width="20" height="16" rx="2"></rect><line x1="12" y1="11" x2="12" y2="17"></line><line x1="9" y1="14" x2="15" y2="14"></line></svg>
                <span>💵 Sang Bảng Tính Lương Tiền Mặt</span>
            </a>
        <?php endif; ?>
        <button onclick="window.print()" class="btn btn-secondary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"></polyline><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
            <span>In Bản KPI</span>
        </button>
    </div>
</div>

<!-- Thẻ tổng quan điểm số & xếp loại -->
<div class="stats-grid" style="grid-template-columns: repeat(4, 1fr); margin-bottom: 24px;">
    <div class="stat-card">
        <div class="stat-icon icon-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><path d="m9 12 2 2 4-4"></path></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value"><?= $assignment['total_weight'] ?? 100 ?>%</div>
            <div class="stat-label">Tổng trọng số KPI</div>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon icon-warning">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 20h9"></path><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value"><?= number_format($evaluation['self_total_score'] ?? 0, 1) ?></div>
            <div class="stat-label">Điểm Tự chấm</div>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon icon-info">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value"><?= number_format($evaluation['manager_total_score'] ?? 0, 1) ?></div>
            <div class="stat-label">Điểm Quản lý thẩm định</div>
        </div>
    </div>

    <div class="stat-card" style="background: linear-gradient(135deg, rgba(37,99,235,0.06) 0%, rgba(59,130,246,0.12) 100%); border-color: rgba(37,99,235,0.3);">
        <div class="stat-icon icon-success">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6"></path><path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18"></path><path d="M4 22h16"></path><path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22"></path><path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22"></path><path d="M18 2H6v7a6 6 0 0 0 12 0V2Z"></path></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value" style="color: var(--primary);"><?= number_format($evaluation['final_score'] ?? ($evaluation['manager_total_score'] ?? 0), 1) ?></div>
            <div class="stat-label">
                <strong><?= htmlspecialchars($evaluation['final_classification'] ?? 'Đang hoàn tất') ?></strong>
            </div>
        </div>
    </div>
</div>

<div class="card" style="margin-bottom: 24px;">
    <div class="card-header" style="display: flex; justify-content: space-between; align-items: center;">
        <h3 class="card-title"><?= htmlspecialchars($assignment['title']) ?></h3>
        <div>
            <span class="badge badge-success"><?= htmlspecialchars($assignment['status']) ?></span>
            <span class="badge badge-info" style="margin-left: 6px;"><?= htmlspecialchars($evaluation['status'] ?? 'Chưa đánh giá') ?></span>
        </div>
    </div>
    <div class="card-body" style="padding: 0;">
        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th style="width: 40px;">STT</th>
                        <th>Chỉ tiêu KPI giao</th>
                        <th style="text-align: center;">ĐVT</th>
                        <th style="text-align: right;">Chỉ tiêu (Target)</th>
                        <th style="text-align: right;">Thực hiện (Actual)</th>
                        <th style="text-align: center;">Tỷ lệ (%)</th>
                        <th style="text-align: center;">Trọng số (%)</th>
                        <th style="text-align: right;">Tự chấm</th>
                        <th style="text-align: right;">QL chấm</th>
                        <th>Ghi chú / Đánh giá</th>
                    </tr>
                </thead>
                <tbody>
                    <?php 
                    $details = $evaluation['details'] ?? $assignment['details'] ?? [];
                    if (!empty($details)): 
                        foreach ($details as $idx => $d):
                            $rate = isset($d['completion_rate']) ? floatval($d['completion_rate']) : 0;
                    ?>
                        <tr>
                            <td><?= $idx + 1 ?></td>
                            <td>
                                <strong><?= htmlspecialchars($d['kpi_name']) ?></strong>
                                <div style="font-size: 11px; color: var(--text-muted);">
                                    Công thức: <?= htmlspecialchars($d['scoring_formula'] ?? 'HigherIsBetter') ?> | Max cap: <?= $d['max_cap'] ?? 150 ?>%
                                </div>
                            </td>
                            <td style="text-align: center;"><span class="badge badge-secondary"><?= htmlspecialchars($d['unit']) ?></span></td>
                            <td style="text-align: right;"><strong><?= number_format($d['target_value'], 1) ?></strong></td>
                            <td style="text-align: right;"><?= isset($d['actual_value']) ? number_format($d['actual_value'], 1) : '-' ?></td>
                            <td style="text-align: center;">
                                <?php if ($rate >= 100): ?>
                                    <span class="badge badge-success"><?= number_format($rate, 1) ?>%</span>
                                <?php elseif ($rate >= 80): ?>
                                    <span class="badge badge-info"><?= number_format($rate, 1) ?>%</span>
                                <?php else: ?>
                                    <span class="badge badge-warning"><?= number_format($rate, 1) ?>%</span>
                                <?php endif; ?>
                            </td>
                            <td style="text-align: center;"><strong><?= $d['weight'] ?>%</strong></td>
                            <td style="text-align: right; color: var(--primary); font-weight: 600;"><?= isset($d['self_score']) ? number_format($d['self_score'], 1) : '-' ?></td>
                            <td style="text-align: right; color: var(--success); font-weight: 700;"><?= isset($d['manager_score']) ? number_format($d['manager_score'], 1) : '-' ?></td>
                            <td>
                                <?php if (!empty($d['manager_remarks'])): ?>
                                    <div style="font-size: 12px; color: var(--text-primary);">QL: <?= htmlspecialchars($d['manager_remarks']) ?></div>
                                <?php endif; ?>
                                <?php if (!empty($d['self_remarks'])): ?>
                                    <div style="font-size: 11px; color: var(--text-muted);">Tự nhận: <?= htmlspecialchars($d['self_remarks']) ?></div>
                                <?php endif; ?>
                            </td>
                        </tr>
                    <?php endforeach; else: ?>
                        <tr><td colspan="10" style="text-align: center; padding: 24px; color: var(--text-muted);">Không có dữ liệu chi tiết chỉ tiêu KPI.</td></tr>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
