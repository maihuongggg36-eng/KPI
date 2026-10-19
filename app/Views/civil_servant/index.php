<?php
$pageTitle = 'Đánh giá Cán bộ Công chức (Nghị định 90 & 48) | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Phân loại Cán bộ Công chức theo Nghị định 90/2020 & 48/2023</h1>
        <p>Kiểm soát tỷ lệ khống chế 20% Hoàn thành xuất sắc nhiệm vụ (BR-06) và điều kiện kỷ luật, sáng kiến (BR-07, BR-08)</p>
    </div>
</div>

<!-- Banner kiểm tra khống chế tỷ lệ 20% (BR-06) -->
<div class="compliance-banner <?= $capValidation['isValid'] ? 'valid' : 'invalid' ?>">
    <div>
        <h3 style="font-size: 16px; font-weight: 800; color: <?= $capValidation['isValid'] ? '#065f46' : '#991b1b' ?>; margin-bottom: 4px;">
            <?= $capValidation['isValid'] ? '✓ TỶ LỆ XẾP LOẠI HỢP LỆ THEO NGHỊ ĐỊNH 90/2020/NĐ-CP' : '⚠ CẢNH BÁO VI PHẠM TỶ LỆ KHỐNG CHẾ NGHỊ ĐỊNH 90/2020/NĐ-CP' ?>
        </h3>
        <p style="font-size: 13.5px; color: <?= $capValidation['isValid'] ? '#047857' : '#b91c1c' ?>;">
            <?= htmlspecialchars($capValidation['message']) ?>
        </p>
    </div>

    <div class="compliance-meter">
        <div class="meter-stat">
            <div class="num" style="color: var(--primary);"><?= $countGood ?></div>
            <div class="lbl">Hoàn thành tốt</div>
        </div>
        <div class="meter-stat">
            <div class="num" style="color: <?= $capValidation['isValid'] ? 'var(--success)' : 'var(--danger)' ?>;"><?= $countExcellent ?></div>
            <div class="lbl">Xuất sắc (Hiện tại)</div>
        </div>
        <div class="meter-stat">
            <div class="num" style="color: var(--text-secondary);"><?= $capValidation['maxAllowed'] ?></div>
            <div class="lbl">Trần tối đa (20%)</div>
        </div>
    </div>
</div>

<div class="card">
    <div class="card-header">
        <h2>Danh sách Cán bộ Công chức & Kết quả Đánh giá</h2>
        <span style="font-size: 13px; color: var(--text-muted);">Tổng số: <?= count($civilServants) ?> cán bộ</span>
    </div>

    <div class="card-body" style="padding: 0;">
        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th>Họ và tên</th>
                        <th>Phòng ban</th>
                        <th>Chức danh</th>
                        <th>Điểm KPI (70%)</th>
                        <th>Điểm Đạo đức (30%)</th>
                        <th>Sáng kiến (BR-08)</th>
                        <th>Kỷ luật (BR-07)</th>
                        <th>Mức đề xuất</th>
                        <th>Xếp loại chính thức</th>
                        <th>Thao tác</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (!empty($civilServants)): ?>
                        <?php foreach ($civilServants as $cs): 
                            $isEx = str_contains($cs['finalized_rank'] ?? '', 'xuất sắc');
                            $isGood = str_contains($cs['finalized_rank'] ?? '', 'tốt');
                            $isFail = str_contains($cs['finalized_rank'] ?? '', 'Không hoàn thành');
                        ?>
                            <tr>
                                <td>
                                    <strong><?= htmlspecialchars($cs['full_name']) ?></strong>
                                    <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($cs['username']) ?></div>
                                </td>
                                <td><?= htmlspecialchars($cs['department_name']) ?></td>
                                <td>
                                    <?= htmlspecialchars($cs['position_name']) ?>
                                    <?php if ($cs['is_leadership']): ?>
                                        <span class="badge badge-primary" style="font-size: 10px; padding: 2px 6px;">Lãnh đạo</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <strong><?= $cs['final_score'] ?? $cs['manager_total_score'] ?? '---' ?></strong>
                                </td>
                                <td><?= $cs['political_ethics_score'] ?? 90 ?> đ</td>
                                <td>
                                    <?php if (!empty($cs['has_approved_initiative'])): ?>
                                        <span class="badge badge-success">Có đề tài</span>
                                    <?php else: ?>
                                        <span class="badge badge-secondary">Không</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <?php if (!empty($cs['has_disciplinary_action'])): ?>
                                        <span class="badge badge-danger">Có kỷ luật</span>
                                    <?php else: ?>
                                        <span class="badge badge-success">Không</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <span style="font-size: 12.5px;"><?= htmlspecialchars($cs['proposed_rank'] ?? 'Chưa đề xuất') ?></span>
                                </td>
                                <td>
                                    <span class="badge <?= $isEx ? 'badge-success' : ($isGood ? 'badge-primary' : ($isFail ? 'badge-danger' : 'badge-warning')) ?>" style="font-size: 12.5px;">
                                        <?= htmlspecialchars($cs['finalized_rank'] ?? 'Chưa xếp loại') ?>
                                    </span>
                                </td>
                                <td>
                                    <div style="display: flex; gap: 6px;">
                                        <a href="/civil-servant/form?user_id=<?= $cs['user_id'] ?>" class="btn btn-secondary btn-sm" title="In phiếu đánh giá theo mẫu Nghị định 90">
                                            <?= $cs['is_leadership'] ? 'Mẫu 01' : 'Mẫu 02' ?>
                                        </a>
                                        <?php if (in_array($_SESSION['role'] ?? '', ['Director', 'HR Admin', 'Super Admin'])): ?>
                                            <button class="btn btn-primary btn-sm" onclick='openRankModal(<?= json_encode($cs) ?>)'>
                                                Sửa
                                            </button>
                                        <?php endif; ?>
                                    </div>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    <?php else: ?>
                        <tr><td colspan="10" style="text-align: center; padding: 32px; color: var(--text-muted);">Chưa có dữ liệu cán bộ công chức.</td></tr>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- Modal cập nhật xếp loại CBCC -->
<div id="rankModal" style="display: none; position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 1000; align-items: center; justify-content: center;">
    <div style="background: white; border-radius: var(--radius-lg); padding: 32px; width: 100%; max-width: 540px; box-shadow: var(--shadow-xl); max-height: 90vh; overflow-y: auto;">
        <h3 style="font-size: 18px; font-weight: 700; margin-bottom: 8px;">Điều chỉnh Xếp loại Cán bộ Công chức</h3>
        <p style="font-size: 13px; color: var(--text-secondary); margin-bottom: 20px;">
            Cán bộ: <strong id="modal-cs-name"></strong>
        </p>

        <form method="POST" action="/civil-servant/update">
            <input type="hidden" name="evaluation_id" id="modal-cs-eval-id">
            <input type="hidden" name="civil_servant_id" id="modal-cs-user-id">

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                <div class="form-group">
                    <label>Điểm Phẩm chất & Đạo đức (thang 100)</label>
                    <input type="number" name="political_ethics_score" id="modal-cs-ethics" class="form-control" min="0" max="100" required>
                </div>
                <div class="form-group">
                    <label>Điểm Thực hiện Chức trách (thang 100)</label>
                    <input type="number" name="duty_execution_score" id="modal-cs-duty" class="form-control" min="0" max="100" required>
                </div>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                <div class="form-group">
                    <label>Sáng kiến cải tiến (BR-08)</label>
                    <select name="has_approved_initiative" id="modal-cs-initiative" class="form-control">
                        <option value="1">Có đề tài / sáng kiến</option>
                        <option value="0">Không có</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Kỷ luật Đảng / CQ (BR-07)</label>
                    <select name="has_disciplinary_action" id="modal-cs-discipline" class="form-control">
                        <option value="0">Không bị kỷ luật</option>
                        <option value="1">Bị xử lý kỷ luật (Điểm liệt)</option>
                    </select>
                </div>
            </div>

            <div class="form-group">
                <label>Xếp loại chất lượng chính thức</label>
                <select name="finalized_rank" id="modal-cs-rank" class="form-control" required>
                    <option value="Hoàn thành xuất sắc nhiệm vụ">Hoàn thành xuất sắc nhiệm vụ</option>
                    <option value="Hoàn thành tốt nhiệm vụ">Hoàn thành tốt nhiệm vụ</option>
                    <option value="Hoàn thành nhiệm vụ">Hoàn thành nhiệm vụ</option>
                    <option value="Không hoàn thành nhiệm vụ">Không hoàn thành nhiệm vụ</option>
                </select>
            </div>

            <div class="form-group">
                <label>Ý kiến nhận xét của Chi bộ / Cấp ủy</label>
                <textarea name="party_opinion" id="modal-cs-party" class="form-control" rows="2" placeholder="Ghi nhận của tổ chức Đảng..."></textarea>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 12px; margin-top: 24px;">
                <button type="button" class="btn btn-secondary" onclick="closeRankModal()">Đóng</button>
                <button type="submit" class="btn btn-primary">Lưu Xếp loại</button>
            </div>
        </form>
    </div>
</div>

<script>
function openRankModal(cs) {
    document.getElementById('modal-cs-eval-id').value = cs.evaluation_id || 1;
    document.getElementById('modal-cs-user-id').value = cs.user_id;
    document.getElementById('modal-cs-name').textContent = cs.full_name;
    document.getElementById('modal-cs-ethics').value = cs.political_ethics_score || 90;
    document.getElementById('modal-cs-duty').value = cs.duty_execution_score || 90;
    document.getElementById('modal-cs-initiative').value = cs.has_approved_initiative ? "1" : "0";
    document.getElementById('modal-cs-discipline').value = cs.has_disciplinary_action ? "1" : "0";
    document.getElementById('modal-cs-rank').value = cs.finalized_rank || 'Hoàn thành tốt nhiệm vụ';
    document.getElementById('modal-cs-party').value = cs.party_opinion || '';
    
    document.getElementById('rankModal').style.display = 'flex';
}
function closeRankModal() {
    document.getElementById('rankModal').style.display = 'none';
}
</script>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
