<?php
$pageTitle = 'Quản lý thẩm định KPI | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Thẩm Định & Chấm Điểm KPI Cấp Dưới</h1>
        <p>Hồ sơ đánh giá của cán bộ: <strong><?= htmlspecialchars($evaluation['employee_name'] ?? 'Cán bộ') ?></strong></p>
    </div>
</div>

<form method="POST" action="/evaluation/manager-review">
    <input type="hidden" name="evaluation_id" value="<?= $evaluation['id'] ?>">
    <input type="hidden" name="assignment_id" value="<?= $evaluation['assignment_id'] ?>">

    <div class="card">
        <div class="card-header">
            <h2>Bảng chấm điểm Thẩm định</h2>
            <div style="display: flex; align-items: center; gap: 20px;">
                <div>
                    <span>Điểm nhân viên tự chấm:</span>
                    <strong style="font-size: 18px; color: var(--text-secondary);"><?= $evaluation['self_total_score'] ?></strong>
                </div>
                <div>
                    <span>Tổng điểm QL thẩm định:</span>
                    <span id="grand-total-mgr-score" style="font-size: 24px; font-weight: 800; color: var(--primary);">
                        <?= $evaluation['manager_total_score'] ?? $evaluation['self_total_score'] ?>
                    </span>
                    <span style="font-size: 13px; color: var(--text-muted);">/ 100 điểm</span>
                </div>
            </div>
        </div>

        <div class="card-body" style="padding: 0;">
            <div class="table-responsive">
                <table class="table">
                    <thead>
                        <tr>
                            <th style="width: 40px;">STT</th>
                            <th>Chỉ số KPI & Trọng số</th>
                            <th>Mục tiêu & Thực hiện</th>
                            <th>Điểm Nhân viên</th>
                            <th style="width: 140px;">Điểm Quản lý chấm</th>
                            <th>Chênh lệch</th>
                            <th>Nhận xét của Quản lý & Giải trình lệch điểm</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php $idx = 1; foreach ($evaluation['details'] as $item): 
                            $detailId = $item['assignment_detail_id'] ?? $item['id'];
                            $weight = $item['weight'] ?? 100;
                            $selfScore = $item['self_score'] ?? 0;
                            $mgrScore = $item['manager_score'] ?? $selfScore;
                            $gap = round($mgrScore - $selfScore, 2);
                            $gapPercent = $weight > 0 ? abs($gap) / $weight * 100 : 0;
                        ?>
                            <tr class="manager-review-row"
                                data-self-score="<?= $selfScore ?>"
                                data-weight="<?= $weight ?>">
                                <td><?= $idx++ ?></td>
                                <td>
                                    <strong><?= htmlspecialchars($item['kpi_name']) ?></strong>
                                    <div style="font-size: 12px; color: var(--text-muted);">
                                        Trọng số: <strong style="color: var(--primary);"><?= $weight ?>%</strong>
                                    </div>
                                    <input type="hidden" name="items[<?= $detailId ?>][assignment_detail_id]" value="<?= $detailId ?>">
                                    <input type="hidden" name="items[<?= $detailId ?>][id]" value="<?= $detailId ?>">
                                    <input type="hidden" name="items[<?= $detailId ?>][self_score]" value="<?= $selfScore ?>">
                                </td>
                                <td>
                                    <div>Mục tiêu: <strong><?= $item['target_value'] ?></strong> <?= htmlspecialchars($item['unit']) ?></div>
                                    <div>Thực tế: <strong style="color: var(--primary);"><?= $item['actual_value'] ?></strong> (<?= $item['completion_rate'] ?>%)</div>
                                    <?php if (!empty($item['self_remarks'])): ?>
                                        <div style="font-size: 11.5px; color: var(--text-secondary); margin-top: 4px; font-style: italic;">
                                            NV tự nhận xét: "<?= htmlspecialchars($item['self_remarks']) ?>"
                                        </div>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <span class="badge badge-secondary" style="font-size: 14px;"><?= $selfScore ?></span>
                                </td>
                                <td>
                                    <input type="number" step="0.1" name="items[<?= $detailId ?>][manager_score]" value="<?= $mgrScore ?>" class="form-control input-mgr-score" style="font-weight: 700; font-size: 15px;" required>
                                </td>
                                <td>
                                    <span class="display-gap badge <?= abs($gap) > 3 ? 'badge-danger' : 'badge-secondary' ?>">
                                        <?= ($gap > 0 ? '+' : '') . $gap ?>
                                    </span>
                                </td>
                                <td>
                                    <textarea name="items[<?= $detailId ?>][manager_remarks]" rows="2" class="form-control" placeholder="Nhận xét của cấp quản lý..."><?= htmlspecialchars($item['manager_remarks'] ?? '') ?></textarea>

                                    <!-- Vùng giải trình bắt buộc nếu lệch điểm > 10% (BR-05) -->
                                    <div class="reason-container" style="display: <?= $gapPercent > 10 ? 'block' : 'none' ?>; margin-top: 8px;">
                                        <div class="alert alert-danger" style="padding: 6px 10px; font-size: 12px; margin-bottom: 4px;">
                                            ⚠ Điểm quản lý chấm lệch > 10% so với điểm tự chấm. Bắt buộc phải giải trình lý do rõ ràng (tối thiểu 50 ký tự) theo quy định BR-05!
                                        </div>
                                        <textarea name="items[<?= $detailId ?>][score_gap_reason]" class="form-control input-gap-reason" rows="2" placeholder="Nhập lý do điều chỉnh điểm (ít nhất 50 ký tự)..."><?= htmlspecialchars($item['score_gap_reason'] ?? '') ?></textarea>
                                    </div>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        </div>

        <div class="card-footer" style="padding: 20px 24px; display: flex; justify-content: flex-end; gap: 12px; background: #f8fafc; border-top: 1px solid var(--border);">
            <a href="/dashboard" class="btn btn-secondary">Quay lại</a>
            <button type="submit" class="btn btn-primary">
                Hoàn Tất Thẩm Định & Trình Cấp Trên
            </button>
        </div>
    </div>
</form>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
