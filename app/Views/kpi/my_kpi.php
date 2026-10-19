<?php
$pageTitle = 'Bản chỉ tiêu KPI của tôi | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Bản chỉ tiêu KPI của tôi</h1>
        <p>Chi tiết các mục tiêu, trọng số và kế hoạch hành động đã được cam kết cho kỳ đánh giá</p>
    </div>
    <div class="page-actions">
        <a href="/evaluation/self-assess" class="btn btn-primary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 20h9"></path><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path></svg>
            <span>Vào Phiếu Tự chấm</span>
        </a>
    </div>
</div>

<?php if (empty($assignment)): ?>
    <div class="card" style="text-align: center; padding: 48px;">
        <p style="color: var(--text-secondary);">Bạn chưa được phân bổ bản giao KPI cho kỳ hiện tại.</p>
    </div>
<?php else: ?>
    <div class="card">
        <div class="card-header">
            <div>
                <h2><?= htmlspecialchars($assignment['title']) ?></h2>
                <div style="font-size: 13px; color: var(--text-muted); margin-top: 4px;">
                    Tổng trọng số cam kết: <strong style="color: var(--success);"><?= $assignment['total_weight'] ?>%</strong> |
                    Người phê duyệt: <strong><?= htmlspecialchars($assignment['approver_name'] ?? 'Lãnh đạo') ?></strong>
                </div>
            </div>
            <span class="badge badge-success"><?= htmlspecialchars($assignment['status']) ?></span>
        </div>
        <div class="card-body" style="padding: 0;">
            <div class="table-responsive">
                <table class="table">
                    <thead>
                        <tr>
                            <th style="width: 50px;">STT</th>
                            <th>Tên Chỉ số KPI</th>
                            <th>Đơn vị tính</th>
                            <th>Chỉ tiêu kỳ này</th>
                            <th>Mức chặn trần (MaxCap)</th>
                            <th>Trọng số (%)</th>
                            <th>Công thức đo lường</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php $idx = 1; foreach ($assignment['details'] as $item): ?>
                            <tr>
                                <td><?= $idx++ ?></td>
                                <td>
                                    <strong><?= htmlspecialchars($item['kpi_name']) ?></strong>
                                </td>
                                <td><?= htmlspecialchars($item['unit']) ?></td>
                                <td><span style="font-size: 15px; font-weight: 700;"><?= $item['target_value'] ?></span></td>
                                <td><?= $item['max_cap'] ?>%</td>
                                <td>
                                    <span class="badge badge-primary" style="font-size: 13px;"><?= $item['weight'] ?>%</span>
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
