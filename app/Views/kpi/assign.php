<?php
$pageTitle = 'Thiết lập & Giao KPI | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Thiết lập & Giao chỉ tiêu KPI</h1>
        <p>Phân bổ mục tiêu hiệu suất, cấu hình trọng số theo chuẩn BSC và tuân thủ quy tắc BR-01, BR-02</p>
    </div>
</div>

<form method="POST" action="/kpi/assign">
    <input type="hidden" name="period_id" value="<?= $period['id'] ?>">

    <div class="card">
        <div class="card-header">
            <h2>1. Thông tin Bản giao chỉ tiêu</h2>
            <div style="display: flex; align-items: center; gap: 12px;">
                <span>Tổng trọng số:</span>
                <span id="total-weight-display" style="font-size: 20px; font-weight: 800; color: var(--danger);">0%</span>
                <span id="weight-status-bar" class="badge badge-danger">Chưa đủ 100%</span>
            </div>
        </div>
        <div class="card-body">
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
                <div class="form-group">
                    <label for="employee_id">Chọn Nhân viên / Cán bộ nhận KPI *</label>
                    <select name="employee_id" id="employee_id" class="form-control" required>
                        <option value="">-- Chọn cán bộ tiếp nhận --</option>
                        <?php foreach ($employees as $emp): ?>
                            <option value="<?= $emp['id'] ?>">
                                <?= htmlspecialchars($emp['full_name']) ?> (<?= htmlspecialchars($emp['username']) ?>) - <?= htmlspecialchars($emp['position_name']) ?>
                            </option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="form-group">
                    <label for="title">Tiêu đề Bản giao KPI *</label>
                    <input type="text" name="title" id="title" class="form-control" value="Bản giao KPI <?= htmlspecialchars($period['name']) ?>" required>
                </div>
            </div>

            <div class="alert alert-info" style="margin-top: 10px;">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>
                <div>
                    <strong>Quy tắc hệ thống (Business Rules):</strong><br>
                    • <strong>BR-01:</strong> Tổng trọng số của tất cả chỉ tiêu bắt buộc phải bằng <strong>100%</strong>.<br>
                    • <strong>BR-02:</strong> Trọng số mỗi chỉ tiêu phải nằm trong khoảng từ <strong>5% đến 50%</strong>.<br>
                    • <strong>BR-03:</strong> Một bản KPI cá nhân không nên vượt quá 7 chỉ tiêu để đảm bảo tính tập trung.
                </div>
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-header">
            <h2>2. Cấu hình Chỉ số & Trọng số KPI</h2>
        </div>
        <div class="card-body" style="padding: 0;">
            <div class="table-responsive">
                <table class="table" id="table-kpi-assign">
                    <thead>
                        <tr>
                            <th style="width: 50px;">Bật</th>
                            <th>Mã & Tên Chỉ số KPI</th>
                            <th>Đơn vị tính</th>
                            <th>Mục tiêu giao</th>
                            <th>Trần tối đa (MaxCap)</th>
                            <th>Trọng số (%)</th>
                            <th>Công thức tính</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($masterKpis as $idx => $m): ?>
                            <tr>
                                <td>
                                    <input type="checkbox" name="kpis[<?= $idx ?>][enabled]" value="1" <?= $idx < 4 ? 'checked' : '' ?> onchange="toggleKpiRow(this, <?= $idx ?>)">
                                    <input type="hidden" name="kpis[<?= $idx ?>][kpi_item_id]" value="<?= $m['id'] ?>">
                                    <input type="hidden" name="kpis[<?= $idx ?>][kpi_name]" value="<?= htmlspecialchars($m['name']) ?>">
                                </td>
                                <td>
                                    <strong><?= htmlspecialchars($m['name']) ?></strong>
                                    <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($m['code']) ?> - <?= htmlspecialchars($m['category_name']) ?></div>
                                </td>
                                <td>
                                    <input type="text" name="kpis[<?= $idx ?>][unit]" value="<?= htmlspecialchars($m['unit_symbol'] ?? $m['symbol'] ?? '') ?>" class="form-control" style="width: 90px;" required>
                                </td>
                                <td>
                                    <input type="number" step="0.1" name="kpis[<?= $idx ?>][target_value]" value="<?= $idx == 0 ? 50 : ($idx == 1 ? 95 : ($idx == 2 ? 2 : 100)) ?>" class="form-control" style="width: 110px;" required>
                                </td>
                                <td>
                                    <input type="number" step="1" name="kpis[<?= $idx ?>][max_cap]" value="150" class="form-control" style="width: 90px;" required>
                                </td>
                                <td>
                                    <input type="number" step="1" min="5" max="50" name="kpis[<?= $idx ?>][weight]" value="<?= $idx == 0 ? 40 : ($idx == 1 ? 30 : ($idx == 2 ? 20 : ($idx == 3 ? 10 : 0))) ?>" class="form-control kpi-weight-input" style="width: 100px; font-weight: 700;" required>
                                </td>
                                <td>
                                    <select name="kpis[<?= $idx ?>][scoring_formula]" class="form-control" style="width: 160px;">
                                        <option value="HigherIsBetter" <?= $m['scoring_formula'] === 'HigherIsBetter' ? 'selected' : '' ?>>Càng cao càng tốt</option>
                                        <option value="LowerIsBetter" <?= $m['scoring_formula'] === 'LowerIsBetter' ? 'selected' : '' ?>>Càng thấp càng tốt</option>
                                    </select>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        </div>
        <div class="card-footer" style="padding: 20px 24px; display: flex; justify-content: flex-end; gap: 12px; background: #f8fafc; border-top: 1px solid var(--border);">
            <a href="/kpi/assignments" class="btn btn-secondary">Hủy bỏ</a>
            <button type="submit" id="btn-submit-kpi" class="btn btn-primary">
                Lưu & Khóa Bản Giao KPI (BR-01/02)
            </button>
        </div>
    </div>
</form>

<script>
function toggleKpiRow(checkbox, idx) {
    const row = checkbox.closest('tr');
    const inputs = row.querySelectorAll('input:not([type="checkbox"]), select');
    inputs.forEach(input => {
        input.disabled = !checkbox.checked;
        if (!checkbox.checked && input.classList.contains('kpi-weight-input')) {
            input.dataset.oldVal = input.value;
            input.value = 0;
        } else if (checkbox.checked && input.classList.contains('kpi-weight-input')) {
            if (input.dataset.oldVal) input.value = input.dataset.oldVal;
        }
    });
    // Kích hoạt tính lại tổng trọng số
    const evt = new Event('input', { bubbles: true });
    document.querySelector('.kpi-weight-input').dispatchEvent(evt);
}
</script>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
