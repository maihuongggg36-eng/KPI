<?php
$pageTitle = 'Bảng Tính Lương Theo KPI & Quản Lý Chi Tiền Mặt | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
$userRole = $_SESSION['role'] ?? 'Employee';
?>

<div class="page-header">
    <div class="page-title">
        <div style="display: flex; align-items: center; gap: 8px;">
            <span class="badge" style="background: #10b981; color: white; font-weight: 700;">💵 THANH TOÁN TIỀN MẶT</span>
            <span style="font-size: 13px; color: var(--text-secondary);">Phân hệ Kế toán & Tiền lương</span>
        </div>
        <h1 style="margin-top: 4px;">Bảng Tính Lương Theo KPI & Quản Lý Chi Trả Tiền Mặt</h1>
        <p>Kỳ lương: <strong><?= htmlspecialchars($period['name']) ?></strong> — Phương thức chi trả: <strong style="color: #059669;">100% Tiền mặt tại quỹ</strong></p>
    </div>
    <div class="page-actions" style="display: flex; gap: 10px; flex-wrap: wrap;">
        <?php if (in_array($userRole, ['Accountant', 'Director', 'Super Admin'])): ?>
            <form method="POST" action="/payroll/calculate" onsubmit="return confirm('Hệ thống sẽ đồng bộ toàn bộ điểm KPI hiện tại của cán bộ nhân viên để tính lương, thưởng hiệu quả KPI và lập phiếu chi tiền mặt. Bạn muốn tiếp tục?');">
                <input type="hidden" name="period_id" value="<?= $periodId ?>">
                <button type="submit" class="btn btn-primary" style="background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"></path></svg>
                    <span>⚡ Tính lương từ KPI</span>
                </button>
            </form>

            <form method="POST" action="/payroll/pay-all-cash" onsubmit="return confirm('Xác nhận đã hoàn tất xuất quỹ chi trả tiền mặt cho toàn bộ nhân viên trong kỳ?');">
                <input type="hidden" name="period_id" value="<?= $periodId ?>">
                <button type="submit" class="btn" style="background: #059669; color: white;">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>
                    <span>💵 Chi tiền mặt toàn bộ</span>
                </button>
            </form>
        <?php endif; ?>

        <a href="/payroll/cash-sheet?period_id=<?= $periodId ?><?= $deptId ? '&department_id=' . $deptId : '' ?>" target="_blank" class="btn btn-secondary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"></polyline><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
            <span>Bảng ký nhận tiền mặt</span>
        </a>
    </div>
</div>

<!-- Bộ thẻ tóm tắt quỹ tiền mặt -->
<div class="stats-grid" style="grid-template-columns: repeat(4, 1fr); margin-bottom: 24px;">
    <div class="stat-card" style="border-left: 4px solid var(--primary);">
        <div class="stat-icon icon-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value"><?= number_format($stats['total_employees']) ?></div>
            <div class="stat-label">Cán bộ / NV được tính lương</div>
        </div>
    </div>

    <div class="stat-card" style="border-left: 4px solid #10b981; background: linear-gradient(135deg, rgba(16, 185, 129, 0.05) 0%, rgba(5, 150, 105, 0.08) 100%);">
        <div class="stat-icon" style="background: rgba(16, 185, 129, 0.15); color: #059669;">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="4" width="20" height="16" rx="2"></rect><line x1="12" y1="11" x2="12" y2="17"></line><line x1="9" y1="14" x2="15" y2="14"></line></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value" style="color: #059669;"><?= number_format($stats['total_net_cash']) ?> đ</div>
            <div class="stat-label">Tổng quỹ tiền mặt cần chi</div>
        </div>
    </div>

    <div class="stat-card" style="border-left: 4px solid #3b82f6;">
        <div class="stat-icon icon-info">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value" style="color: #2563eb;"><?= number_format($stats['total_cash_paid']) ?> đ</div>
            <div class="stat-label">Đã chi tiền mặt (<?= $stats['count_paid'] ?> người)</div>
        </div>
    </div>

    <div class="stat-card" style="border-left: 4px solid #f59e0b;">
        <div class="stat-icon icon-warning">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value" style="color: #d97706;"><?= number_format($stats['total_cash_pending']) ?> đ</div>
            <div class="stat-label">Tồn quỹ chờ chi (<?= $stats['count_pending'] ?> người)</div>
        </div>
    </div>
</div>

<!-- Bộ lọc kỳ và phòng ban -->
<div class="card" style="margin-bottom: 20px;">
    <div class="card-body" style="padding: 16px 20px;">
        <form method="GET" action="/payroll" style="display: flex; gap: 16px; align-items: center; flex-wrap: wrap;">
            <div style="display: flex; align-items: center; gap: 8px;">
                <label for="period_select" style="font-weight: 600; font-size: 13px; color: var(--text-secondary);">Kỳ đánh giá & Lương:</label>
                <select id="period_select" name="period_id" class="form-control" style="width: auto; padding: 6px 12px;" onchange="this.form.submit()">
                    <?php foreach ($periods as $p): ?>
                        <option value="<?= $p['id'] ?>" <?= $p['id'] == $periodId ? 'selected' : '' ?>>
                            <?= htmlspecialchars($p['name']) ?> (<?= $p['year'] ?>)
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <div style="display: flex; align-items: center; gap: 8px;">
                <label for="dept_select" style="font-weight: 600; font-size: 13px; color: var(--text-secondary);">Phòng ban:</label>
                <select id="dept_select" name="department_id" class="form-control" style="width: auto; padding: 6px 12px;" onchange="this.form.submit()">
                    <option value="">-- Tất cả phòng ban --</option>
                    <?php foreach ($departments as $d): ?>
                        <option value="<?= $d['id'] ?>" <?= $deptId == $d['id'] ? 'selected' : '' ?>>
                            <?= htmlspecialchars($d['name']) ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <noscript><button type="submit" class="btn btn-secondary btn-sm">Lọc</button></noscript>
        </form>
    </div>
</div>

<!-- Bảng chi tiết lương -->
<div class="card">
    <div class="card-header" style="display: flex; justify-content: space-between; align-items: center;">
        <h3 class="card-title">Chi tiết Bảng Thanh toán Tiền lương Tiền mặt</h3>
        <span style="font-size: 12px; color: var(--text-muted);">
            Thưởng KPI được tính tự động từ điểm đánh giá hiệu suất của kỳ
        </span>
    </div>
    <div class="card-body" style="padding: 0;">
        <?php if (!empty($payrolls)): ?>
            <div class="table-responsive">
                <table class="table">
                    <thead>
                        <tr>
                            <th style="width: 90px;">Mã PC</th>
                            <th>Cán bộ / Nhân viên</th>
                            <th>Phòng ban</th>
                            <th style="text-align: center;">Điểm KPI & Xếp loại</th>
                            <th style="text-align: right;">Lương cứng / CB</th>
                            <th style="text-align: right;">Thưởng KPI (P3)</th>
                            <th style="text-align: right;">Phụ cấp</th>
                            <th style="text-align: right;">Giảm trừ</th>
                            <th style="text-align: right; background: #f0fdf4;">Thực lĩnh TIỀN MẶT</th>
                            <th style="text-align: center;">Trạng thái chi</th>
                            <th style="text-align: right;">Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($payrolls as $pr): ?>
                            <tr>
                                <td>
                                    <span style="font-family: 'JetBrains Mono', monospace; font-size: 12px; font-weight: 600; color: var(--text-secondary);">
                                        <?= htmlspecialchars($pr['voucher_code'] ?? 'PC-000') ?>
                                    </span>
                                </td>
                                <td>
                                    <strong><?= htmlspecialchars($pr['employee_name']) ?></strong>
                                    <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($pr['position_name'] ?? '') ?></div>
                                </td>
                                <td><?= htmlspecialchars($pr['department_name'] ?? '') ?></td>
                                <td style="text-align: center;">
                                    <?php if ($pr['kpi_score'] > 0): ?>
                                        <div style="font-weight: 700; color: var(--primary);">
                                            <?= number_format($pr['kpi_score'], 1) ?> đ
                                        </div>
                                        <div style="font-size: 11px;">
                                            <?php if ($pr['kpi_score'] >= 90): ?>
                                                <span class="badge badge-success" style="font-size: 10px;">Xuất sắc (x<?= $pr['kpi_multiplier'] ?>)</span>
                                            <?php elseif ($pr['kpi_score'] >= 75): ?>
                                                <span class="badge badge-primary" style="font-size: 10px;">Tốt (x<?= $pr['kpi_multiplier'] ?>)</span>
                                            <?php else: ?>
                                                <span class="badge badge-warning" style="font-size: 10px;">Hoàn thành</span>
                                            <?php endif; ?>
                                        </div>
                                    <?php else: ?>
                                        <span style="color: var(--text-muted); font-size: 11px;">Chưa chốt KPI</span>
                                    <?php endif; ?>
                                </td>
                                <td style="text-align: right;"><?= number_format($pr['base_salary']) ?> đ</td>
                                <td style="text-align: right; color: var(--success); font-weight: 600;">
                                    +<?= number_format($pr['kpi_bonus']) ?> đ
                                </td>
                                <td style="text-align: right; color: var(--text-secondary);">+<?= number_format($pr['allowances']) ?> đ</td>
                                <td style="text-align: right; color: var(--danger);">-<?= number_format($pr['deductions']) ?> đ</td>
                                <td style="text-align: right; background: #f0fdf4;">
                                    <strong style="font-size: 15px; color: #059669;">
                                        <?= number_format($pr['net_salary']) ?> đ
                                    </strong>
                                    <div style="font-size: 10px; color: #059669; font-weight: 600;">💵 Tiền mặt</div>
                                </td>
                                <td style="text-align: center;">
                                    <?php if ($pr['payment_status'] === 'Đã chi tiền mặt'): ?>
                                        <span class="badge badge-success" style="display: inline-flex; align-items: center; gap: 4px;">
                                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="20 6 9 17 4 12"></polyline></svg>
                                            Đã nhận tiền mặt
                                        </span>
                                        <div style="font-size: 10.5px; color: var(--text-muted); margin-top: 2px;">
                                            <?= $pr['paid_at'] ? date('d/m/Y H:i', strtotime($pr['paid_at'])) : '' ?>
                                        </div>
                                    <?php else: ?>
                                        <span class="badge badge-warning">Chờ chi tiền mặt</span>
                                    <?php endif; ?>
                                </td>
                                <td style="text-align: right;">
                                    <div style="display: inline-flex; gap: 6px;">
                                        <a href="/payroll/voucher?id=<?= $pr['id'] ?>" target="_blank" class="btn btn-secondary btn-sm" title="In Phiếu chi tiền mặt cá nhân">
                                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"></polyline><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
                                            Phiếu chi
                                        </a>

                                        <?php if (in_array($userRole, ['Accountant', 'Super Admin'])): ?>
                                            <?php if ($pr['payment_status'] !== 'Đã chi tiền mặt'): ?>
                                                <form method="POST" action="/payroll/pay-cash" style="display: inline;">
                                                    <input type="hidden" name="payroll_id" value="<?= $pr['id'] ?>">
                                                    <button type="submit" class="btn btn-sm" style="background: #10b981; color: white;" title="Xác nhận đã giao tiền mặt tại quỹ">
                                                        💵 Đã chi
                                                    </button>
                                                </form>
                                            <?php endif; ?>

                                            <button type="button" class="btn btn-secondary btn-sm" onclick="openEditModal(<?= htmlspecialchars(json_encode($pr)) ?>)" title="Điều chỉnh các khoản lương">
                                                Sửa
                                            </button>
                                        <?php endif; ?>
                                    </div>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                    <tfoot>
                        <tr style="background: #f8fafc; font-weight: 700;">
                            <td colspan="4" style="text-align: right;">TỔNG CỘNG TOÀN CƠ QUAN:</td>
                            <td style="text-align: right;"><?= number_format($stats['total_base_salary']) ?> đ</td>
                            <td style="text-align: right; color: var(--success);"><?= number_format($stats['total_kpi_bonus']) ?> đ</td>
                            <td style="text-align: right;"><?= number_format($stats['total_allowances']) ?> đ</td>
                            <td style="text-align: right; color: var(--danger);">-<?= number_format($stats['total_deductions']) ?> đ</td>
                            <td style="text-align: right; background: #ecfdf5; color: #059669; font-size: 16px;">
                                <?= number_format($stats['total_net_cash']) ?> đ
                            </td>
                            <td colspan="2" style="text-align: center; color: var(--text-secondary); font-size: 12px;">
                                Chi trả 100% bằng Tiền mặt tại quỹ
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>
        <?php else: ?>
            <div style="text-align: center; padding: 48px 20px;">
                <div style="width: 56px; height: 56px; border-radius: 50%; background: #ecfdf5; color: #10b981; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;">
                    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="4" width="20" height="16" rx="2"></rect><line x1="12" y1="11" x2="12" y2="17"></line><line x1="9" y1="14" x2="15" y2="14"></line></svg>
                </div>
                <h3>Chưa có dữ liệu tính lương cho kỳ này</h3>
                <p style="color: var(--text-muted); margin-bottom: 20px;">Kế toán bấm nút bên dưới để tự động tính lương và thưởng hiệu quả dựa trên kết quả đánh giá KPI của nhân viên.</p>
                <?php if (in_array($userRole, ['Accountant', 'Director', 'Super Admin'])): ?>
                    <form method="POST" action="/payroll/calculate">
                        <input type="hidden" name="period_id" value="<?= $periodId ?>">
                        <button type="submit" class="btn btn-primary">
                            ⚡ Bắt đầu Tính Lương Theo KPI
                        </button>
                    </form>
                <?php endif; ?>
            </div>
        <?php endif; ?>
    </div>
</div>

<!-- Modal Chỉnh sửa phiếu lương -->
<div id="editPayrollModal" style="display: none; position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 1000; align-items: center; justify-content: center; padding: 20px;">
    <div style="background: white; border-radius: var(--radius-lg); width: 100%; max-width: 520px; box-shadow: var(--shadow-xl); overflow: hidden;">
        <div style="padding: 18px 24px; border-bottom: 1px solid var(--border); display: flex; justify-content: space-between; align-items: center;">
            <h3 style="font-size: 17px; font-weight: 700; margin: 0;">Điều chỉnh Phiếu lương Tiền mặt</h3>
            <button type="button" onclick="closeEditModal()" style="border: none; background: none; font-size: 22px; cursor: pointer; color: var(--text-muted);">&times;</button>
        </div>
        <form method="POST" action="/payroll/update" style="padding: 24px;">
            <input type="hidden" id="modal_payroll_id" name="payroll_id">
            
            <div style="margin-bottom: 16px;">
                <label style="font-size: 13px; font-weight: 600; color: var(--text-secondary); display: block; margin-bottom: 6px;">Cán bộ / Nhân viên:</label>
                <div id="modal_employee_name" style="font-weight: 700; font-size: 15px; color: var(--text-primary);"></div>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 14px;">
                <div>
                    <label for="modal_base_salary" style="font-size: 12.5px; font-weight: 600; display: block; margin-bottom: 4px;">Lương cứng / Cơ bản (VNĐ):</label>
                    <input type="number" id="modal_base_salary" name="base_salary" class="form-control" required>
                </div>
                <div>
                    <label for="modal_kpi_bonus" style="font-size: 12.5px; font-weight: 600; display: block; margin-bottom: 4px;">Thưởng hiệu quả KPI (VNĐ):</label>
                    <input type="number" id="modal_kpi_bonus" name="kpi_bonus" class="form-control" required>
                </div>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 14px;">
                <div>
                    <label for="modal_allowances" style="font-size: 12.5px; font-weight: 600; display: block; margin-bottom: 4px;">Phụ cấp (VNĐ):</label>
                    <input type="number" id="modal_allowances" name="allowances" class="form-control" required>
                </div>
                <div>
                    <label for="modal_deductions" style="font-size: 12.5px; font-weight: 600; display: block; margin-bottom: 4px;">Giảm trừ (BHXH/BHYT) (VNĐ):</label>
                    <input type="number" id="modal_deductions" name="deductions" class="form-control" required>
                </div>
            </div>

            <div style="margin-bottom: 20px;">
                <label for="modal_notes" style="font-size: 12.5px; font-weight: 600; display: block; margin-bottom: 4px;">Ghi chú chi tiền mặt:</label>
                <textarea id="modal_notes" name="notes" class="form-control" rows="2" placeholder="Ghi chú chi trả tiền mặt..."></textarea>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 10px;">
                <button type="button" onclick="closeEditModal()" class="btn btn-secondary">Hủy bỏ</button>
                <button type="submit" class="btn btn-primary">Lưu điều chỉnh</button>
            </div>
        </form>
    </div>
</div>

<script>
function openEditModal(pr) {
    document.getElementById('modal_payroll_id').value = pr.id;
    document.getElementById('modal_employee_name').textContent = pr.employee_name + ' (' + (pr.position_name || '') + ')';
    document.getElementById('modal_base_salary').value = pr.base_salary;
    document.getElementById('modal_kpi_bonus').value = pr.kpi_bonus;
    document.getElementById('modal_allowances').value = pr.allowances;
    document.getElementById('modal_deductions').value = pr.deductions;
    document.getElementById('modal_notes').value = pr.notes || '';
    
    const modal = document.getElementById('editPayrollModal');
    modal.style.display = 'flex';
}

function closeEditModal() {
    document.getElementById('editPayrollModal').style.display = 'none';
}
</script>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
