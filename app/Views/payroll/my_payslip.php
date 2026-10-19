<?php
$pageTitle = 'Phiếu Lương Tiền Mặt Cá Nhân | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <div style="display: flex; align-items: center; gap: 8px;">
            <span class="badge" style="background: #10b981; color: white;">💵 HÌNH THỨC: TIỀN MẶT</span>
        </div>
        <h1 style="margin-top: 4px;">Phiếu Lương Tiền Mặt Cá Nhân</h1>
        <p>Kỳ lương: <strong><?= htmlspecialchars($period['name']) ?></strong> — Cán bộ: <strong><?= htmlspecialchars($_SESSION['full_name']) ?></strong></p>
    </div>
    <div class="page-actions">
        <?php if ($payroll): ?>
            <a href="/payroll/voucher?id=<?= $payroll['id'] ?>" target="_blank" class="btn btn-primary" style="background: linear-gradient(135deg, #059669 0%, #10b981 100%);">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"></polyline><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
                <span>In Phiếu Chi Tiền Mặt Mẫu 02-TT</span>
            </a>
        <?php endif; ?>
    </div>
</div>

<?php if ($payroll): ?>
    <div class="stats-grid" style="grid-template-columns: repeat(3, 1fr); margin-bottom: 24px;">
        <div class="stat-card">
            <div class="stat-icon icon-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
            </div>
            <div class="stat-info">
                <div class="stat-value"><?= number_format($payroll['kpi_score'], 1) ?> đ</div>
                <div class="stat-label">Điểm KPI (<?= htmlspecialchars($payroll['final_classification'] ?? 'Đang đánh giá') ?>)</div>
            </div>
        </div>

        <div class="stat-card" style="background: linear-gradient(135deg, rgba(16, 185, 129, 0.08) 0%, rgba(5, 150, 105, 0.12) 100%); border-color: rgba(16, 185, 129, 0.3);">
            <div class="stat-icon" style="background: rgba(16, 185, 129, 0.2); color: #059669;">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="4" width="20" height="16" rx="2"></rect><line x1="12" y1="11" x2="12" y2="17"></line><line x1="9" y1="14" x2="15" y2="14"></line></svg>
            </div>
            <div class="stat-info">
                <div class="stat-value" style="color: #059669; font-size: 26px;"><?= number_format($payroll['net_salary']) ?> đ</div>
                <div class="stat-label">Thực lĩnh TIỀN MẶT tại quỹ</div>
            </div>
        </div>

        <div class="stat-card">
            <div class="stat-icon icon-info">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
            </div>
            <div class="stat-info">
                <div class="stat-value" style="font-size: 18px;">
                    <?php if ($payroll['payment_status'] === 'Đã chi tiền mặt'): ?>
                        <span style="color: #059669;">Đã nhận tiền mặt</span>
                    <?php else: ?>
                        <span style="color: #d97706;">Chờ nhận tiền mặt</span>
                    <?php endif; ?>
                </div>
                <div class="stat-label">Mã phiếu chi: <?= htmlspecialchars($payroll['voucher_code'] ?? 'PC') ?></div>
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Chi tiết Cơ cấu Thu nhập & Thưởng Hiệu suất KPI</h3>
        </div>
        <div class="card-body" style="padding: 24px;">
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px;">
                <div>
                    <h4 style="font-size: 15px; margin-bottom: 14px; color: var(--text-primary); border-bottom: 2px solid var(--border); padding-bottom: 8px;">
                        1. Các khoản Thu nhập (Thu nhập chịu thuế)
                    </h4>
                    <table style="width: 100%; border-collapse: collapse;">
                        <tr style="border-bottom: 1px solid var(--border); height: 38px;">
                            <td style="color: var(--text-secondary);">Lương cơ bản / ngạch bậc:</td>
                            <td style="text-align: right; font-weight: 600;"><?= number_format($payroll['base_salary']) ?> đ</td>
                        </tr>
                        <tr style="border-bottom: 1px solid var(--border); height: 38px;">
                            <td style="color: var(--text-secondary);">
                                Thưởng hiệu suất KPI (P3) 
                                <span class="badge badge-success" style="font-size: 10px;">x<?= $payroll['kpi_multiplier'] ?></span>:
                            </td>
                            <td style="text-align: right; font-weight: 700; color: #059669;">+<?= number_format($payroll['kpi_bonus']) ?> đ</td>
                        </tr>
                        <tr style="border-bottom: 1px solid var(--border); height: 38px;">
                            <td style="color: var(--text-secondary);">Phụ cấp trách nhiệm / công vụ:</td>
                            <td style="text-align: right; font-weight: 600;">+<?= number_format($payroll['allowances']) ?> đ</td>
                        </tr>
                        <tr style="height: 44px; font-weight: bold; background: #f8fafc;">
                            <td>TỔNG THU NHẬP:</td>
                            <td style="text-align: right; color: var(--primary);">
                                <?= number_format($payroll['base_salary'] + $payroll['kpi_bonus'] + $payroll['allowances']) ?> đ
                            </td>
                        </tr>
                    </table>
                </div>

                <div>
                    <h4 style="font-size: 15px; margin-bottom: 14px; color: var(--text-primary); border-bottom: 2px solid var(--border); padding-bottom: 8px;">
                        2. Giảm trừ & Thực lĩnh Tiền mặt
                    </h4>
                    <table style="width: 100%; border-collapse: collapse;">
                        <tr style="border-bottom: 1px solid var(--border); height: 38px;">
                            <td style="color: var(--text-secondary);">Trừ BHXH, BHYT, BHTN (10.5%):</td>
                            <td style="text-align: right; font-weight: 600; color: var(--danger);">-<?= number_format($payroll['deductions']) ?> đ</td>
                        </tr>
                        <tr style="border-bottom: 1px solid var(--border); height: 38px;">
                            <td style="color: var(--text-secondary);">Phương thức chi trả:</td>
                            <td style="text-align: right; font-weight: 700; color: #059669;">💵 100% TIỀN MẶT TẠI QUỸ</td>
                        </tr>
                        <tr style="border-bottom: 1px solid var(--border); height: 38px;">
                            <td style="color: var(--text-secondary);">Trạng thái chi trả:</td>
                            <td style="text-align: right;">
                                <?php if ($payroll['payment_status'] === 'Đã chi tiền mặt'): ?>
                                    <span class="badge badge-success">✓ Đã nhận tiền mặt</span>
                                <?php else: ?>
                                    <span class="badge badge-warning">⏳ Chờ nhận tiền mặt tại quỹ</span>
                                <?php endif; ?>
                            </td>
                        </tr>
                        <tr style="height: 44px; font-weight: bold; background: #ecfdf5;">
                            <td style="color: #059669; font-size: 15px;">THỰC LĨNH TIỀN MẶT:</td>
                            <td style="text-align: right; color: #059669; font-size: 18px;">
                                <?= number_format($payroll['net_salary']) ?> đ
                            </td>
                        </tr>
                    </table>
                </div>
            </div>

            <div style="margin-top: 24px; padding: 16px; background: #f8fafc; border-radius: var(--radius-md); border-left: 4px solid #10b981;">
                <div style="font-weight: 600; color: var(--text-primary); margin-bottom: 4px;">
                    Số tiền viết bằng chữ: <span style="font-style: italic; color: #059669;"><?= $amountInWords ?></span>
                </div>
                <div style="font-size: 13px; color: var(--text-secondary);">
                    Địa điểm nhận tiền mặt: Phòng Kế hoạch - Tài chính (Gặp Kế toán viên: <strong>Hoàng Kế Toán</strong> hoặc Thủ quỹ để ký nhận tiền mặt).
                </div>
            </div>
        </div>
    </div>
<?php else: ?>
    <div class="card">
        <div class="card-body" style="text-align: center; padding: 50px 20px;">
            <div style="width: 60px; height: 60px; border-radius: 50%; background: #eff6ff; color: var(--primary); display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;">
                <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
            </div>
            <h3>Phiếu lương kỳ này đang được Kế toán tính toán</h3>
            <p style="color: var(--text-muted); max-width: 500px; margin: 8px auto 20px;">
                Sau khi kết quả đánh giá KPI của kỳ được chốt, Kế toán sẽ thực hiện tính toán lương và lập phiếu chi tiền mặt tại quỹ cho bạn.
            </p>
            <a href="/kpi/my" class="btn btn-secondary">Xem Bản chỉ tiêu KPI của tôi</a>
        </div>
    </div>
<?php endif; ?>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
