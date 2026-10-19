<?php
$pageTitle = 'Ngân hàng Chỉ số & Danh mục Quản trị | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Ngân hàng Chỉ số KPI & Danh mục Hệ thống</h1>
        <p>Kho từ điển KPI chuẩn hóa theo 4 viễn cảnh BSC và nhóm chỉ số Đạo đức công vụ (Session 07, 08)</p>
    </div>
    <div class="page-actions">
        <button class="btn btn-primary" onclick="openNewKpiModal()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
            <span>Thêm Chỉ số KPI Mới</span>
        </button>
    </div>
</div>

<div class="card">
    <div class="card-header">
        <h2>1. Danh mục Ngân hàng Chỉ số KPI Chuẩn</h2>
        <span class="badge badge-primary"><?= count($masterItems) ?> chỉ số</span>
    </div>
    <div class="card-body" style="padding: 0;">
        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th>Mã KPI</th>
                        <th>Tên chỉ tiêu</th>
                        <th>Nhóm / Viễn cảnh BSC</th>
                        <th>Đơn vị tính</th>
                        <th>Công thức</th>
                        <th>Đối tượng</th>
                        <th>Trạng thái</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($masterItems as $k): ?>
                        <tr>
                            <td><code><?= htmlspecialchars($k['code']) ?></code></td>
                            <td>
                                <strong><?= htmlspecialchars($k['name']) ?></strong>
                                <div style="font-size: 11.5px; color: var(--text-muted);"><?= htmlspecialchars($k['description'] ?? '') ?></div>
                            </td>
                            <td><?= htmlspecialchars($k['category_name']) ?></td>
                            <td><span class="badge badge-secondary"><?= htmlspecialchars($k['unit_name']) ?> (<?= htmlspecialchars($k['unit_symbol'] ?? $k['symbol'] ?? '') ?>)</span></td>
                            <td>
                                <span class="badge <?= $k['scoring_formula'] === 'HigherIsBetter' ? 'badge-success' : 'badge-primary' ?>">
                                    <?= $k['scoring_formula'] === 'HigherIsBetter' ? 'Càng cao càng tốt' : 'Càng thấp càng tốt' ?>
                                </span>
                            </td>
                            <td>
                                <?= !empty($k['is_civil_servant_only']) ? '<span class="badge badge-warning">Công chức</span>' : '<span class="badge badge-secondary">Tất cả</span>' ?>
                            </td>
                            <td>
                                <span class="badge badge-success">Hoạt động</span>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px;">
    <!-- Nhóm danh mục -->
    <div class="card">
        <div class="card-header">
            <h3>2. Nhóm Chỉ số (Viễn cảnh BSC & Đạo đức)</h3>
        </div>
        <div class="card-body" style="padding: 0;">
            <table class="table">
                <thead>
                    <tr>
                        <th>Mã</th>
                        <th>Tên nhóm</th>
                        <th>Viễn cảnh</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($categories as $cat): ?>
                        <tr>
                            <td><code><?= htmlspecialchars($cat['code']) ?></code></td>
                            <td><strong><?= htmlspecialchars($cat['name']) ?></strong></td>
                            <td><?= htmlspecialchars($cat['perspective']) ?></td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    </div>

    <!-- Đơn vị tính -->
    <div class="card">
        <div class="card-header">
            <h3>3. Đơn vị Đo lường Chuẩn</h3>
        </div>
        <div class="card-body" style="padding: 0;">
            <table class="table">
                <thead>
                    <tr>
                        <th>Mã</th>
                        <th>Tên đơn vị</th>
                        <th>Ký hiệu</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($units as $u): ?>
                        <tr>
                            <td><code><?= htmlspecialchars($u['code']) ?></code></td>
                            <td><strong><?= htmlspecialchars($u['name']) ?></strong></td>
                            <td><span class="badge badge-primary"><?= htmlspecialchars($u['symbol']) ?></span></td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- Modal Thêm KPI Mới -->
<div id="newKpiModal" style="display: none; position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 1000; align-items: center; justify-content: center;">
    <div style="background: white; border-radius: var(--radius-lg); padding: 32px; width: 100%; max-width: 520px; box-shadow: var(--shadow-xl);">
        <h3 style="font-size: 18px; font-weight: 700; margin-bottom: 16px;">Bổ sung Chỉ tiêu vào Ngân hàng KPI</h3>

        <form method="POST" action="/catalogs/kpi/store">
            <div class="form-group">
                <label>Mã chỉ tiêu (Code) *</label>
                <input type="text" name="code" class="form-control" placeholder="VD: KPI_SNGV_05" required>
            </div>

            <div class="form-group">
                <label>Tên chỉ tiêu *</label>
                <input type="text" name="name" class="form-control" placeholder="VD: Tỷ lệ dịch thuật văn bản đối ngoại chính xác..." required>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                <div class="form-group">
                    <label>Nhóm chỉ tiêu</label>
                    <select name="category_id" class="form-control">
                        <?php foreach ($categories as $c): ?>
                            <option value="<?= $c['id'] ?>"><?= htmlspecialchars($c['name']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="form-group">
                    <label>Đơn vị tính</label>
                    <select name="unit_id" class="form-control">
                        <?php foreach ($units as $u): ?>
                            <option value="<?= $u['id'] ?>"><?= htmlspecialchars($u['name']) ?> (<?= htmlspecialchars($u['symbol']) ?>)</option>
                        <?php endforeach; ?>
                    </select>
                </div>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                <div class="form-group">
                    <label>Công thức đo lường</label>
                    <select name="scoring_formula" class="form-control">
                        <option value="HigherIsBetter">Càng cao càng tốt</option>
                        <option value="LowerIsBetter">Càng thấp càng tốt</option>
                    </select>
                </div>

                <div class="form-group">
                    <label>Phân loại đối tượng</label>
                    <select name="is_civil_servant_only" class="form-control">
                        <option value="1">Dành cho Công chức</option>
                        <option value="0">Dành cho Doanh nghiệp / Chung</option>
                    </select>
                </div>
            </div>

            <div class="form-group">
                <label>Mô tả chi tiết / Phương pháp đo</label>
                <textarea name="description" rows="2" class="form-control" placeholder="Mô tả cách thức thu thập số liệu..."></textarea>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 12px; margin-top: 24px;">
                <button type="button" class="btn btn-secondary" onclick="closeNewKpiModal()">Hủy</button>
                <button type="submit" class="btn btn-primary">Lưu Chỉ Số</button>
            </div>
        </form>
    </div>
</div>

<script>
function openNewKpiModal() {
    document.getElementById('newKpiModal').style.display = 'flex';
}
function closeNewKpiModal() {
    document.getElementById('newKpiModal').style.display = 'none';
}
</script>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
