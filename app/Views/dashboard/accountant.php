<?php
$pageTitle = 'Bàn làm việc Kế toán | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <div style="display: flex; align-items: center; gap: 8px;">
            <span class="badge" style="background: #10b981; color: white;">VAI TRÒ: KẾ TOÁN VIÊN</span>
            <span style="font-size: 13px; color: var(--text-secondary);">Phòng Kế hoạch - Tài chính</span>
        </div>
        <h1 style="margin-top: 4px;">Tổng quan KPI & Quỹ Lương Tiền Mặt</h1>
        <p>Kỳ làm việc: <strong><?= htmlspecialchars($period['name']) ?></strong> — Thủ tục thanh toán: <strong>100% Tiền mặt tại quỹ</strong></p>
    </div>
    <div class="page-actions" style="display: flex; gap: 8px;">
        <form method="POST" action="/payroll/calculate" onsubmit="return confirm('Đồng bộ điểm KPI để tự động tính toán lương và lập phiếu chi tiền mặt cho toàn bộ cán bộ?');">
            <input type="hidden" name="period_id" value="<?= $period['id'] ?>">
            <button type="submit" class="btn btn-primary" style="background: linear-gradient(135deg, #059669 0%, #10b981 100%);">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"></path></svg>
                <span>⚡ Tính lương từ KPI</span>
            </button>
        </form>
        <a href="/payroll" class="btn btn-secondary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="4" width="20" height="16" rx="2"></rect><line x1="12" y1="11" x2="12" y2="17"></line><line x1="9" y1="14" x2="15" y2="14"></line></svg>
            <span>Bảng lương tiền mặt</span>
        </a>
    </div>
</div>

<!-- Bộ thẻ thống kê tiền mặt & KPI -->
<div class="stats-grid" style="grid-template-columns: repeat(4, 1fr); margin-bottom: 24px;">
    <div class="stat-card" style="border-left: 4px solid #10b981;">
        <div class="stat-icon" style="background: rgba(16, 185, 129, 0.15); color: #059669;">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="4" width="20" height="16" rx="2"></rect><line x1="12" y1="11" x2="12" y2="17"></line><line x1="9" y1="14" x2="15" y2="14"></line></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value" style="color: #059669;"><?= number_format($payrollStats['total_net_cash'] ?? 0) ?> đ</div>
            <div class="stat-label">Tổng quỹ TIỀN MẶT tại quỹ</div>
        </div>
    </div>

    <div class="stat-card" style="border-left: 4px solid #2563eb;">
        <div class="stat-icon icon-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value" style="color: #2563eb;"><?= number_format($payrollStats['total_cash_paid'] ?? 0) ?> đ</div>
            <div class="stat-label">Đã chi tiền mặt (<?= $payrollStats['count_paid'] ?? 0 ?> cán bộ)</div>
        </div>
    </div>

    <div class="stat-card" style="border-left: 4px solid #f59e0b;">
        <div class="stat-icon icon-warning">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value" style="color: #d97706;"><?= number_format($payrollStats['total_cash_pending'] ?? 0) ?> đ</div>
            <div class="stat-label">Tồn quỹ chờ phát (<?= $payrollStats['count_pending'] ?? 0 ?> cán bộ)</div>
        </div>
    </div>

    <div class="stat-card" style="border-left: 4px solid #8b5cf6;">
        <div class="stat-icon" style="background: rgba(139, 92, 246, 0.15); color: #7c3aed;">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
        </div>
        <div class="stat-info">
            <div class="stat-value" style="color: #7c3aed;"><?= number_format($stats['avgScore'] ?? 0, 1) ?> đ</div>
            <div class="stat-label">Điểm KPI trung bình cơ quan</div>
        </div>
    </div>
</div>

<div class="grid-2-1" style="display: grid; grid-template-columns: 2fr 1fr; gap: 24px; margin-bottom: 24px;">
    <!-- Bảng lương tiền mặt gần nhất -->
    <div class="card">
        <div class="card-header" style="display: flex; justify-content: space-between; align-items: center;">
            <h3 class="card-title">Bảng Lương & Chi Tiền Mặt Gắn Với KPI</h3>
            <a href="/payroll" style="font-size: 13px; color: var(--primary); text-decoration: none; font-weight: 600;">Xem tất cả →</a>
        </div>
        <div class="card-body" style="padding: 0;">
            <div class="table-responsive">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Mã PC</th>
                            <th>Cán bộ / Nhân viên</th>
                            <th style="text-align: center;">Điểm KPI</th>
                            <th style="text-align: right;">Thưởng KPI</th>
                            <th style="text-align: right;">Thực lĩnh TIỀN MẶT</th>
                            <th style="text-align: center;">Trạng thái</th>
                            <th style="text-align: right;">Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php if (!empty($recentPayrolls)): ?>
                            <?php foreach (array_slice($recentPayrolls, 0, 5) as $pr): ?>
                                <tr>
                                    <td style="font-family: monospace; font-size: 11.5px;"><?= htmlspecialchars($pr['voucher_code'] ?? '') ?></td>
                                    <td>
                                        <strong><?= htmlspecialchars($pr['employee_name']) ?></strong>
                                        <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($pr['position_name'] ?? '') ?></div>
                                    </td>
                                    <td style="text-align: center; font-weight: bold; color: var(--primary);">
                                        <?= number_format($pr['kpi_score'], 1) ?>
                                    </td>
                                    <td style="text-align: right; color: var(--success); font-weight: 600;">
                                        +<?= number_format($pr['kpi_bonus']) ?> đ
                                    </td>
                                    <td style="text-align: right; font-weight: bold; color: #059669;">
                                        <?= number_format($pr['net_salary']) ?> đ
                                    </td>
                                    <td style="text-align: center;">
                                        <?php if ($pr['payment_status'] === 'Đã chi tiền mặt'): ?>
                                            <span class="badge badge-success">Đã nhận TM</span>
                                        <?php else: ?>
                                            <span class="badge badge-warning">Chờ chi TM</span>
                                        <?php endif; ?>
                                    </td>
                                    <td style="text-align: right;">
                                        <div style="display: inline-flex; gap: 4px;">
                                            <a href="/payroll/voucher?id=<?= $pr['id'] ?>" target="_blank" class="btn btn-secondary btn-sm" title="In phiếu chi tiền mặt">
                                                Phiếu chi
                                            </a>
                                            <?php if ($pr['payment_status'] !== 'Đã chi tiền mặt'): ?>
                                                <form method="POST" action="/payroll/pay-cash" style="display: inline;">
                                                    <input type="hidden" name="payroll_id" value="<?= $pr['id'] ?>">
                                                    <button type="submit" class="btn btn-sm" style="background: #10b981; color: white;" title="Xác nhận chi tiền mặt">
                                                        💵 Đã chi
                                                    </button>
                                                </form>
                                            <?php endif; ?>
                                        </div>
                                    </td>
                                </tr>
                            <?php endforeach; ?>
                        <?php else: ?>
                            <tr>
                                <td colspan="7" style="text-align: center; padding: 24px; color: var(--text-muted);">
                                    Chưa có dữ liệu bảng lương. Bấm nút "⚡ Tính lương từ KPI" để khởi tạo.
                                </td>
                            </tr>
                        <?php endif; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>

    <!-- Hướng dẫn & Quy định chi lương tiền mặt -->
    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Quy Định Chi Trả Tiền Mặt</h3>
        </div>
        <div class="card-body" style="padding: 20px; font-size: 13.5px; line-height: 1.6;">
            <div style="display: flex; gap: 10px; margin-bottom: 14px;">
                <div style="font-size: 20px;">💵</div>
                <div>
                    <strong>Phương thức trả lương:</strong><br>
                    Toàn bộ tiền lương, phụ cấp và thưởng hiệu quả KPI được chi trả <strong>bằng tiền mặt tại quỹ</strong> cơ quan.
                </div>
            </div>

            <div style="display: flex; gap: 10px; margin-bottom: 14px;">
                <div style="font-size: 20px;">🎯</div>
                <div>
                    <strong>Công thức Thưởng KPI:</strong><br>
                    • Điểm KPI ≥ 90: Thưởng 125% lương hiệu quả<br>
                    • Điểm KPI 75-89: Thưởng 100% lương hiệu quả<br>
                    • Điểm KPI 50-74: Thưởng 75% lương hiệu quả<br>
                    • Điểm KPI &lt; 50: Không có thưởng KPI
                </div>
            </div>

            <div style="display: flex; gap: 10px; margin-bottom: 14px;">
                <div style="font-size: 20px;">📝</div>
                <div>
                    <strong>Chứng từ kế toán bắt buộc:</strong><br>
                    • Phiếu chi tiền mặt (Mẫu 02-TT)<br>
                    • Bảng ký nhận tiền mặt (Mẫu 05a-LĐTL)<br>
                    • Đầy đủ chữ ký người nhận và thủ quỹ
                </div>
            </div>

            <div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid var(--border);">
                <a href="/payroll/cash-sheet" target="_blank" class="btn btn-secondary" style="width: 100%; justify-content: center;">
                    🖨️ In Bảng Ký Nhận Tiền Mặt
                </a>
            </div>
        </div>
    </div>
</div>

<!-- Danh sách bản giao KPI để Kế toán theo dõi -->
<div class="card">
    <div class="card-header" style="display: flex; justify-content: space-between; align-items: center;">
        <h3 class="card-title">Theo Dõi Tiến Độ Đánh Giá KPI Toàn Cơ Quan</h3>
        <a href="/kpi/assignments" style="font-size: 13px; color: var(--primary); text-decoration: none; font-weight: 600;">Xem toàn bộ danh sách KPI →</a>
    </div>
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
                    <?php if (!empty($recentAssignments)): ?>
                        <?php foreach (array_slice($recentAssignments, 0, 5) as $a): ?>
                            <tr>
                                <td><strong><?= htmlspecialchars($a['employee_name'] ?? '') ?></strong></td>
                                <td><?= htmlspecialchars($a['department_name'] ?? '') ?></td>
                                <td><?= htmlspecialchars($a['position_name'] ?? '') ?></td>
                                <td><?= htmlspecialchars($a['title'] ?? '') ?></td>
                                <td><span class="badge badge-success"><?= $a['total_weight'] ?? 100 ?>%</span></td>
                                <td>
                                    <?php if (!empty($a['final_score'])): ?>
                                        <strong style="color: var(--primary);"><?= number_format($a['final_score'], 1) ?> đ</strong>
                                        <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($a['final_classification'] ?? '') ?></div>
                                    <?php else: ?>
                                        <span style="color: var(--text-muted); font-size: 12px;">Đang đánh giá</span>
                                    <?php endif; ?>
                                </td>
                                <td><span class="badge badge-primary"><?= htmlspecialchars($a['status']) ?></span></td>
                                <td style="text-align: right;">
                                    <div style="display: inline-flex; gap: 6px;">
                                        <a href="/kpi/view?assignment_id=<?= $a['id'] ?>" class="btn btn-secondary btn-sm">Xem KPI</a>
                                        <a href="/payroll?period_id=<?= $a['period_id'] ?>" class="btn btn-sm" style="background: #10b981; color: white;">💵 Tính lương</a>
                                    </div>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
