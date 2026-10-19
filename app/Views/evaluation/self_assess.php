<?php
$pageTitle = 'Phiếu Tự chấm điểm KPI | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Phiếu Tự Đánh Giá Hiệu Suất KPI</h1>
        <p>Kỳ: <strong><?= htmlspecialchars($period['name']) ?></strong> - Cán bộ: <strong><?= htmlspecialchars($_SESSION['full_name']) ?></strong></p>
    </div>
</div>

<form method="POST" action="/evaluation/self-assess" enctype="multipart/form-data">
    <input type="hidden" name="evaluation_id" value="<?= $evaluation['id'] ?>">

    <div class="card">
        <div class="card-header">
            <h2>Chỉ số KPI & Nhập kết quả thực tế</h2>
            <div style="display: flex; align-items: center; gap: 12px;">
                <span>Tổng điểm tự chấm:</span>
                <span id="grand-total-self-score" style="font-size: 24px; font-weight: 800; color: var(--primary);">
                    <?= $evaluation['self_total_score'] ?? '0.0' ?>
                </span>
                <span style="font-size: 13px; color: var(--text-muted);">/ 100 điểm</span>
            </div>
        </div>
        <div class="card-body" style="padding: 0;">
            <div class="table-responsive">
                <table class="table">
                    <thead>
                        <tr>
                            <th style="width: 40px;">STT</th>
                            <th>Chỉ số KPI & Trọng số</th>
                            <th>Mục tiêu</th>
                            <th style="width: 140px;">Kết quả thực tế</th>
                            <th>Tỷ lệ hoàn thành</th>
                            <th>Điểm tự chấm</th>
                            <th>Ý kiến tự nhận xét & Tệp minh chứng</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php 
                        $idx = 1;
                        $details = !empty($evaluation['details']) ? $evaluation['details'] : $assignment['details'];
                        foreach ($details as $item): 
                            $actual = $item['actual_value'] ?? 0;
                            $rate = $item['completion_rate'] ?? 0;
                            $selfScore = $item['self_score'] ?? 0;
                            $detailId = $item['assignment_detail_id'] ?? $item['id'];
                        ?>
                            <tr class="self-assess-row" 
                                data-target="<?= $item['target_value'] ?>" 
                                data-weight="<?= $item['weight'] ?>" 
                                data-maxcap="<?= $item['max_cap'] ?? 150 ?>" 
                                data-formula="<?= $item['scoring_formula'] ?? 'HigherIsBetter' ?>">
                                <td><?= $idx++ ?></td>
                                <td>
                                    <strong><?= htmlspecialchars($item['kpi_name']) ?></strong>
                                    <div style="font-size: 12px; color: var(--text-secondary);">
                                        Trọng số: <strong style="color: var(--primary);"><?= $item['weight'] ?>%</strong> |
                                        Trần MaxCap: <?= $item['max_cap'] ?? 150 ?>%
                                    </div>
                                    <input type="hidden" name="items[<?= $detailId ?>][assignment_detail_id]" value="<?= $detailId ?>">
                                    <input type="hidden" name="items[<?= $detailId ?>][kpi_name]" value="<?= htmlspecialchars($item['kpi_name']) ?>">
                                    <input type="hidden" name="items[<?= $detailId ?>][target_value]" value="<?= $item['target_value'] ?>">
                                    <input type="hidden" name="items[<?= $detailId ?>][weight]" value="<?= $item['weight'] ?>">
                                    <input type="hidden" name="items[<?= $detailId ?>][max_cap]" value="<?= $item['max_cap'] ?? 150 ?>">
                                    <input type="hidden" name="items[<?= $detailId ?>][scoring_formula]" value="<?= $item['scoring_formula'] ?? 'HigherIsBetter' ?>">
                                </td>
                                <td>
                                    <strong><?= $item['target_value'] ?></strong> <?= htmlspecialchars($item['unit']) ?>
                                </td>
                                <td>
                                    <input type="number" step="0.1" name="items[<?= $detailId ?>][actual_value]" value="<?= $actual ?>" class="form-control input-actual" required>
                                </td>
                                <td>
                                    <span class="display-rate" style="font-weight: 700; color: var(--primary);"><?= $rate ?>%</span>
                                </td>
                                <td>
                                    <span class="display-score" style="font-size: 16px; font-weight: 800; color: var(--success);"><?= $selfScore ?></span>
                                </td>
                                <td>
                                    <textarea name="items[<?= $detailId ?>][self_remarks]" rows="2" class="form-control" placeholder="Mô tả kết quả đạt được..."><?= htmlspecialchars($item['self_remarks'] ?? '') ?></textarea>
                                    
                                    <div class="evidence-alert" style="display: <?= $rate >= 120 ? 'block' : 'none' ?>; margin-top: 6px;">
                                        <div class="alert alert-warning" style="padding: 6px 10px; font-size: 12px; margin-bottom: 6px;">
                                            ⚠ Đạt ≥ 120%: Bắt buộc đính kèm minh chứng chứng minh vượt trần (BR-09)!
                                        </div>
                                    </div>

                                    <div style="margin-top: 6px; font-size: 12px;">
                                        <label style="font-weight: 600; display: block; margin-bottom: 2px;">Tệp minh chứng (PDF/Ảnh/Word):</label>
                                        <input type="file" name="evidence_<?= $detailId ?>" class="form-control input-evidence" style="padding: 4px;">
                                        <?php if (!empty($item['evidence_file'])): ?>
                                            <input type="hidden" name="items[<?= $detailId ?>][existing_evidence]" value="<?= htmlspecialchars($item['evidence_file']) ?>">
                                            <div style="margin-top: 4px;">
                                                <a href="/uploads/<?= htmlspecialchars($item['evidence_file']) ?>" target="_blank" style="color: var(--primary); font-weight: 600;">
                                                    📎 Xem minh chứng đã đính kèm
                                                </a>
                                            </div>
                                        <?php endif; ?>
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
                Nộp Phiếu Tự Chấm Cho Quản Lý
            </button>
        </div>
    </div>
</form>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
