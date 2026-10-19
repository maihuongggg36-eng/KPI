<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Phiếu Chi Tiền Mặt - <?= htmlspecialchars($payroll['voucher_code'] ?? 'PC') ?> - <?= htmlspecialchars($payroll['employee_name']) ?></title>
    <style>
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }
        body {
            font-family: 'Times New Roman', Times, serif;
            font-size: 15px;
            color: #000;
            background-color: #f1f5f9;
            padding: 30px;
        }
        .page-container {
            max-width: 800px;
            margin: 0 auto;
            background: #fff;
            padding: 40px 50px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
            border-radius: 4px;
        }
        .header-top {
            display: flex;
            justify-content: space-between;
            margin-bottom: 24px;
        }
        .company-info {
            line-height: 1.4;
            font-weight: bold;
        }
        .company-info span {
            font-weight: normal;
        }
        .form-standard {
            text-align: right;
            font-size: 13px;
            line-height: 1.4;
        }
        .title-section {
            text-align: center;
            margin: 20px 0 24px;
        }
        .voucher-title {
            font-size: 24px;
            font-weight: bold;
            letter-spacing: 1px;
            text-transform: uppercase;
        }
        .voucher-date {
            font-style: italic;
            font-size: 14px;
            margin-top: 5px;
        }
        .voucher-meta {
            font-size: 14px;
            margin-top: 4px;
            font-weight: bold;
        }
        .content-body {
            line-height: 2;
        }
        .field-row {
            display: flex;
            align-items: baseline;
        }
        .field-label {
            min-width: 175px;
        }
        .field-value {
            flex-grow: 1;
            border-bottom: 1px dotted #888;
            padding-left: 6px;
            font-weight: 500;
        }
        .signature-section {
            margin-top: 35px;
            display: grid;
            grid-template-columns: repeat(5, 1fr);
            text-align: center;
            gap: 10px;
        }
        .sig-block {
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            min-height: 130px;
        }
        .sig-title {
            font-weight: bold;
            font-size: 14px;
        }
        .sig-note {
            font-size: 12px;
            font-style: italic;
            color: #555;
        }
        .sig-name {
            font-weight: bold;
            margin-top: auto;
        }
        .cash-badge {
            display: inline-block;
            background: #dcfce7;
            color: #15803d;
            border: 1px solid #86efac;
            padding: 3px 8px;
            border-radius: 4px;
            font-size: 12px;
            font-weight: bold;
            margin-left: 8px;
        }
        .no-print {
            text-align: center;
            margin-bottom: 20px;
        }
        .btn-print {
            background: #2563eb;
            color: #fff;
            padding: 10px 20px;
            border: none;
            border-radius: 6px;
            font-size: 15px;
            cursor: pointer;
            font-weight: bold;
            display: inline-flex;
            align-items: center;
            gap: 8px;
        }
        .btn-print:hover {
            background: #1d4ed8;
        }
        .kpi-box {
            margin: 15px 0;
            padding: 10px 15px;
            background: #f8fafc;
            border: 1px dashed #cbd5e1;
            border-radius: 4px;
            font-size: 13.5px;
            line-height: 1.6;
        }
        @media print {
            body {
                background: #fff;
                padding: 0;
            }
            .page-container {
                box-shadow: none;
                padding: 15px;
                max-width: 100%;
            }
            .no-print {
                display: none;
            }
        }
    </style>
</head>
<body>

<div class="no-print">
    <button class="btn-print" onclick="window.print()">
        🖨️ In Phiếu Chi Tiền Mặt
    </button>
    <a href="/payroll" style="margin-left: 15px; color: #475569; text-decoration: none; font-size: 14px;">
        ← Quay lại Bảng lương
    </a>
</div>

<div class="page-container">
    <div class="header-top">
        <div class="company-info">
            Đơn vị: SỞ NGOẠI VỤ TP HÀ NỘI<br>
            <span>Bộ phận: Kế hoạch - Tài chính</span><br>
            <span>Mã số thuế: 0101234567</span>
        </div>
        <div class="form-standard">
            <strong>Mẫu số 02 - TT</strong><br>
            (Ban hành theo Thông tư số 200/2014/TT-BTC<br>
            ngày 22/12/2014 của Bộ Tài chính)
        </div>
    </div>

    <div class="title-section">
        <div class="voucher-title">PHIẾU CHI TIỀN MẶT</div>
        <div class="voucher-date">
            Ngày <?= date('d') ?> tháng <?= date('m') ?> năm <?= date('Y') ?>
        </div>
        <div class="voucher-meta">
            Số: <?= htmlspecialchars($payroll['voucher_code'] ?? 'PC-001') ?>
            <span class="cash-badge">💵 THANH TOÁN TIỀN MẶT</span>
        </div>
    </div>

    <div class="content-body">
        <div class="field-row">
            <span class="field-label">Họ và tên người nhận tiền:</span>
            <span class="field-value"><strong><?= htmlspecialchars($payroll['employee_name']) ?></strong></span>
        </div>
        <div class="field-row">
            <span class="field-label">Chức vụ / Phòng ban:</span>
            <span class="field-value"><?= htmlspecialchars($payroll['position_name'] ?? 'Cán bộ') ?> — <?= htmlspecialchars($payroll['department_name'] ?? 'Sở Ngoại vụ') ?></span>
        </div>
        <div class="field-row">
            <span class="field-label">Lý do chi:</span>
            <span class="field-value">Thanh toán tiền lương và tiền thưởng hiệu quả theo kết quả KPI - <?= htmlspecialchars($payroll['period_name'] ?? 'Quý 1/2026') ?></span>
        </div>
        <div class="field-row">
            <span class="field-label">Số tiền chi (bằng số):</span>
            <span class="field-value" style="font-size: 16px; color: #059669; font-weight: bold;">
                <?= number_format($payroll['net_salary']) ?> VNĐ
            </span>
        </div>
        <div class="field-row">
            <span class="field-label">Số tiền (viết bằng chữ):</span>
            <span class="field-value" style="font-style: italic; font-weight: 600;"><?= $amountInWords ?></span>
        </div>
        <div class="field-row">
            <span class="field-label">Hình thức thanh toán:</span>
            <span class="field-value"><strong>Tiền mặt tại quỹ</strong> (Người nhận đã kiểm đếm và nhận đủ tiền mặt)</span>
        </div>
        <div class="field-row">
            <span class="field-label">Kèm theo:</span>
            <span class="field-value">Bản đánh giá hiệu suất KPI (Đạt <?= number_format($payroll['kpi_score'], 1) ?> điểm — <?= htmlspecialchars($payroll['final_classification'] ?? 'Đạt chuẩn') ?>)</span>
        </div>
    </div>

    <div class="kpi-box">
        <strong>Bảng kê cơ cấu thu nhập tiền mặt theo KPI:</strong><br>
        • Lương cơ bản / ngạch bậc: <?= number_format($payroll['base_salary']) ?> đ &nbsp;|&nbsp;
        • Thưởng hiệu quả KPI (P3): <strong>+<?= number_format($payroll['kpi_bonus']) ?> đ</strong> (Điểm KPI: <?= number_format($payroll['kpi_score'], 1) ?> đ) &nbsp;|&nbsp;
        • Phụ cấp: +<?= number_format($payroll['allowances']) ?> đ &nbsp;|&nbsp;
        • Giảm trừ: -<?= number_format($payroll['deductions']) ?> đ<br>
        👉 <strong>Thực nhận tiền mặt: <?= number_format($payroll['net_salary']) ?> VNĐ</strong>
    </div>

    <div class="signature-section">
        <div class="sig-block">
            <div class="sig-title">Thủ trưởng đơn vị</div>
            <div class="sig-note">(Ký, họ tên, đóng dấu)</div>
            <div class="sig-name">Trần Lãnh Đạo</div>
        </div>

        <div class="sig-block">
            <div class="sig-title">Kế toán trưởng</div>
            <div class="sig-note">(Ký, họ tên)</div>
            <div class="sig-name">Trần Thị Thu</div>
        </div>

        <div class="sig-block">
            <div class="sig-title">Người lập phiếu</div>
            <div class="sig-note">(Ký, họ tên)</div>
            <div class="sig-name">Hoàng Kế Toán</div>
        </div>

        <div class="sig-block">
            <div class="sig-title">Thủ quỹ</div>
            <div class="sig-note">(Ký, họ tên)</div>
            <div class="sig-name">Lê Thị Quỹ</div>
        </div>

        <div class="sig-block">
            <div class="sig-title">Người nhận tiền mặt</div>
            <div class="sig-note">(Ký, ghi rõ họ tên)</div>
            <div class="sig-name"><?= htmlspecialchars($payroll['employee_name']) ?></div>
        </div>
    </div>

    <div style="margin-top: 30px; font-size: 13px; font-style: italic; border-top: 1px dotted #ccc; padding-top: 10px;">
        Đã nhận đủ số tiền (viết bằng chữ): <?= $amountInWords ?><br>
        Xác nhận tiền mặt đã giao nhận tại quỹ: <?= $payroll['paid_at'] ? date('d/m/Y H:i', strtotime($payroll['paid_at'])) : 'Đang tiến hành chi tiền mặt' ?>
    </div>
</div>

</body>
</html>
