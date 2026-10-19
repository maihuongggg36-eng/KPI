<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Bảng Thanh Toán Tiền Lương Bằng Tiền Mặt - Kỳ <?= htmlspecialchars($period['name']) ?></title>
    <style>
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }
        body {
            font-family: 'Times New Roman', Times, serif;
            font-size: 14px;
            color: #000;
            background-color: #f1f5f9;
            padding: 25px;
        }
        .page-container {
            max-width: 1100px;
            margin: 0 auto;
            background: #fff;
            padding: 35px 40px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
            border-radius: 4px;
        }
        .header-top {
            display: flex;
            justify-content: space-between;
            margin-bottom: 20px;
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
            font-size: 12.5px;
            line-height: 1.4;
        }
        .title-section {
            text-align: center;
            margin: 15px 0 25px;
        }
        .sheet-title {
            font-size: 21px;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        .sheet-subtitle {
            font-size: 14px;
            font-style: italic;
            margin-top: 5px;
        }
        .cash-badge {
            display: inline-block;
            background: #dcfce7;
            color: #15803d;
            border: 1px solid #86efac;
            padding: 2px 10px;
            border-radius: 4px;
            font-size: 12px;
            font-weight: bold;
            margin-top: 6px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
            font-size: 13.5px;
        }
        th, td {
            border: 1px solid #333;
            padding: 7px 8px;
            vertical-align: middle;
        }
        th {
            background-color: #f1f5f9;
            text-align: center;
            font-weight: bold;
        }
        .text-center { text-align: center; }
        .text-right { text-align: right; }
        .font-bold { font-weight: bold; }
        .signature-section {
            margin-top: 35px;
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            text-align: center;
            gap: 15px;
        }
        .sig-block {
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            min-height: 120px;
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
        @media print {
            body {
                background: #fff;
                padding: 0;
            }
            .page-container {
                box-shadow: none;
                padding: 10px;
                max-width: 100%;
            }
            .no-print {
                display: none;
            }
            th {
                background-color: #eee !important;
                -webkit-print-color-adjust: exact;
            }
        }
    </style>
</head>
<body>

<div class="no-print">
    <button class="btn-print" onclick="window.print()">
        🖨️ In Bảng Ký Nhận Lương Tiền Mặt
    </button>
    <a href="/payroll" style="margin-left: 15px; color: #475569; text-decoration: none; font-size: 14px;">
        ← Quay lại Bảng lương
    </a>
</div>

<div class="page-container">
    <div class="header-top">
        <div class="company-info">
            Đơn vị: SỞ NGOẠI VỤ TP HÀ NỘI<br>
            <span>Phòng Kế hoạch - Tài chính</span><br>
            <span>Mã số thuế: 0101234567</span>
        </div>
        <div class="form-standard">
            <strong>Mẫu số 05a - LĐTL</strong><br>
            (Ban hành theo Thông tư số 200/2014/TT-BTC<br>
            ngày 22/12/2014 của Bộ Tài chính)
        </div>
    </div>

    <div class="title-section">
        <div class="sheet-title">BẢNG THANH TOÁN TIỀN LƯƠNG VÀ CÁC KHOẢN PHỤ CẤP BẰNG TIỀN MẶT</div>
        <div class="sheet-subtitle">
            Kỳ thanh toán: <?= htmlspecialchars($period['name']) ?> (Năm <?= $period['year'] ?>)
        </div>
        <div>
            <span class="cash-badge">💵 HÌNH THỨC CHI TRẢ: 100% TIỀN MẶT TẠI QUỸ</span>
        </div>
    </div>

    <table>
        <thead>
            <tr>
                <th style="width: 35px;">STT</th>
                <th style="width: 75px;">Mã PC</th>
                <th>Họ và tên cán bộ, công chức</th>
                <th>Chức danh</th>
                <th>Phòng ban</th>
                <th style="width: 60px;">Điểm KPI</th>
                <th style="width: 95px;">Lương cứng / CB</th>
                <th style="width: 95px;">Thưởng KPI (P3)</th>
                <th style="width: 85px;">Phụ cấp</th>
                <th style="width: 80px;">Giảm trừ</th>
                <th style="width: 105px;">Thực lĩnh TIỀN MẶT</th>
                <th style="width: 130px;">Ký nhận tiền mặt</th>
            </tr>
        </thead>
        <tbody>
            <?php 
            $stt = 1;
            if (!empty($payrolls)):
                foreach ($payrolls as $pr): 
            ?>
                <tr>
                    <td class="text-center"><?= $stt++ ?></td>
                    <td class="text-center font-bold" style="font-family: monospace; font-size: 11.5px;">
                        <?= htmlspecialchars($pr['voucher_code'] ?? '') ?>
                    </td>
                    <td>
                        <strong><?= htmlspecialchars($pr['employee_name']) ?></strong>
                    </td>
                    <td><?= htmlspecialchars($pr['position_name'] ?? '') ?></td>
                    <td><?= htmlspecialchars($pr['department_name'] ?? '') ?></td>
                    <td class="text-center font-bold" style="color: #1d4ed8;">
                        <?= number_format($pr['kpi_score'], 1) ?>
                    </td>
                    <td class="text-right"><?= number_format($pr['base_salary']) ?></td>
                    <td class="text-right font-bold" style="color: #059669;">
                        <?= number_format($pr['kpi_bonus']) ?>
                    </td>
                    <td class="text-right"><?= number_format($pr['allowances']) ?></td>
                    <td class="text-right" style="color: #dc2626;"><?= number_format($pr['deductions']) ?></td>
                    <td class="text-right font-bold" style="color: #059669; font-size: 14px;">
                        <?= number_format($pr['net_salary']) ?>
                    </td>
                    <td class="text-center" style="font-size: 12px; height: 42px;">
                        <?php if ($pr['payment_status'] === 'Đã chi tiền mặt'): ?>
                            <span style="font-style: italic; color: #059669; font-weight: bold;">
                                <?= htmlspecialchars($pr['recipient_signature'] ?: 'Đã nhận đủ tiền mặt') ?>
                            </span>
                        <?php else: ?>
                            <span style="color: #94a3b8;">.........................</span>
                        <?php endif; ?>
                    </td>
                </tr>
            <?php 
                endforeach;
            else: 
            ?>
                <tr><td colspan="12" class="text-center" style="padding: 20px;">Chưa có dữ liệu bảng lương trong kỳ.</td></tr>
            <?php endif; ?>
        </tbody>
        <tfoot>
            <tr style="font-weight: bold; background-color: #f8fafc;">
                <td colspan="6" class="text-center">TỔNG CỘNG TOÀN CƠ QUAN:</td>
                <td class="text-right"><?= number_format($stats['total_base_salary']) ?></td>
                <td class="text-right" style="color: #059669;"><?= number_format($stats['total_kpi_bonus']) ?></td>
                <td class="text-right"><?= number_format($stats['total_allowances']) ?></td>
                <td class="text-right" style="color: #dc2626;"><?= number_format($stats['total_deductions']) ?></td>
                <td class="text-right" style="color: #059669; font-size: 14.5px;">
                    <?= number_format($stats['total_net_cash']) ?> đ
                </td>
                <td class="text-center" style="font-size: 11px;">100% Tiền mặt</td>
            </tr>
        </tfoot>
    </table>

    <div style="margin-top: 15px; font-style: italic; font-size: 13.5px;">
        • Tổng số tiền mặt thanh toán (viết bằng chữ): <strong><?= $totalInWords ?></strong><br>
        • Tất cả các khoản lương, thưởng hiệu quả theo KPI và phụ cấp đều được chi trả bằng tiền mặt tại quỹ tài chính của cơ quan.
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
            <div class="sig-title">Người lập biểu</div>
            <div class="sig-note">(Ký, họ tên)</div>
            <div class="sig-name">Hoàng Kế Toán</div>
        </div>

        <div class="sig-block">
            <div class="sig-title">Thủ quỹ chi tiền mặt</div>
            <div class="sig-note">(Ký, họ tên)</div>
            <div class="sig-name">Lê Thị Quỹ</div>
        </div>
    </div>
</div>

</body>
</html>
