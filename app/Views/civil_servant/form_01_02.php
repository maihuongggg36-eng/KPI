<?php
$pageTitle = 'Phiếu đánh giá ' . htmlspecialchars($formType) . ' | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header no-print">
    <div class="page-title">
        <h1>Biểu mẫu Đánh giá Cán bộ Công chức Chuẩn Quốc gia</h1>
        <p>Áp dụng theo Nghị định số 90/2020/NĐ-CP và Nghị định số 48/2023/NĐ-CP của Chính phủ</p>
    </div>
    <div class="page-actions">
        <a href="/civil-servant" class="btn btn-secondary">Quay lại danh sách</a>
        <button onclick="window.print()" class="btn btn-primary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"></polyline><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
            <span>In Phiếu Đánh Giá (A4)</span>
        </button>
    </div>
</div>

<div class="card" style="max-width: 900px; margin: 0 auto 40px; padding: 48px 56px; font-family: 'Times New Roman', Times, serif; color: #000; line-height: 1.6; background: #fff;">
    <!-- Quốc hiệu Tiêu ngữ chuẩn thể thức văn bản hành chính Việt Nam -->
    <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px;">
        <div style="text-align: center; width: 45%;">
            <div style="font-size: 13pt; font-weight: bold; text-transform: uppercase;"><?= htmlspecialchars($data['org_name']) ?></div>
            <div style="font-size: 13pt; font-weight: bold; text-transform: uppercase;"><?= htmlspecialchars($data['department_name']) ?></div>
            <div style="width: 120px; height: 1px; background: #000; margin: 4px auto 0;"></div>
        </div>
        <div style="text-align: center; width: 55%;">
            <div style="font-size: 12.5pt; font-weight: bold; text-transform: uppercase;">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</div>
            <div style="font-size: 13pt; font-weight: bold;">Độc lập - Tự do - Hạnh phúc</div>
            <div style="width: 160px; height: 1px; background: #000; margin: 4px auto 0;"></div>
            <div style="font-size: 12pt; font-style: italic; margin-top: 8px;">Hà Nội, ngày <?= date('d') ?> tháng <?= date('m') ?> năm <?= date('Y') ?></div>
        </div>
    </div>

    <!-- Tên biểu mẫu -->
    <div style="text-align: center; margin: 32px 0 28px;">
        <div style="font-size: 12pt; font-style: italic; font-weight: bold; color: #444;"><?= htmlspecialchars($formType) ?></div>
        <h2 style="font-size: 16pt; font-weight: bold; text-transform: uppercase; margin: 6px 0;">
            PHIẾU ĐÁNH GIÁ, XẾP LOẠI CHẤT LƯỢNG CÔNG CHỨC
        </h2>
        <div style="font-size: 14pt; font-weight: bold;">Năm đánh giá: <?= $data['year'] ?? 2026 ?></div>
    </div>

    <!-- I. Thông tin công chức -->
    <div style="margin-bottom: 20px;">
        <div style="font-size: 13pt; font-weight: bold; text-transform: uppercase;">I. THÔNG TIN CHUNG CỦA CÔNG CHỨC</div>
        <table style="width: 100%; font-size: 13pt; margin-top: 8px; border-collapse: collapse;">
            <tr>
                <td style="padding: 4px 0; width: 35%;">1. Họ và tên:</td>
                <td style="padding: 4px 0; font-weight: bold; text-transform: uppercase;"><?= htmlspecialchars($data['full_name']) ?></td>
            </tr>
            <tr>
                <td style="padding: 4px 0;">2. Chức vụ, chức danh:</td>
                <td style="padding: 4px 0;"><?= htmlspecialchars($data['position_name']) ?></td>
            </tr>
            <tr>
                <td style="padding: 4px 0;">3. Cơ quan, đơn vị công tác:</td>
                <td style="padding: 4px 0;"><?= htmlspecialchars($data['department_name']) ?> - <?= htmlspecialchars($data['org_name']) ?></td>
            </tr>
            <tr>
                <td style="padding: 4px 0;">4. Ngạch công chức:</td>
                <td style="padding: 4px 0;"><?= $isLeadership ? 'Chuyên viên chính / Lãnh đạo' : 'Chuyên viên nghiệp vụ' ?></td>
            </tr>
        </table>
    </div>

    <!-- II. Tự đánh giá -->
    <div style="margin-bottom: 20px;">
        <div style="font-size: 13pt; font-weight: bold; text-transform: uppercase;">II. TỰ ĐÁNH GIÁ KẾT QUẢ RÈN LUYỆN, CÔNG TÁC</div>
        
        <div style="font-size: 13pt; margin-top: 8px;">
            <p><strong>1. Về chính trị tư tưởng:</strong> Chấp hành nghiêm túc chủ trương, đường lối của Đảng, chính sách pháp luật của Nhà nước; có lập trường tư tưởng vững vàng, kiên định mục tiêu độc lập dân tộc và chủ nghĩa xã hội.</p>
            <p style="margin-top: 6px;"><strong>2. Về đạo đức, lối sống:</strong> Giữ gìn phẩm chất đạo đức trong sáng, lối sống lành mạnh, cần kiệm liêm chính; không vụ lợi, gương mẫu trong sinh hoạt và quan hệ quần chúng.</p>
            <p style="margin-top: 6px;"><strong>3. Về tác phong, lề lối làm việc:</strong> Có tinh thần trách nhiệm cao, năng động, sáng tạo; thực hiện tốt văn hóa công sở, kỷ cương hành chính và lề lối làm việc chuẩn mực.</p>
            <p style="margin-top: 6px;"><strong>4. Về ý thức tổ chức kỷ luật:</strong> Chấp hành nghiêm chỉnh sự phân công của tổ chức; thực hiện nghiêm các quy chế, nội quy của cơ quan; kê khai tài sản, thu nhập đúng quy định.</p>
            <p style="margin-top: 6px;">
                <strong>5. Về kết quả thực hiện chức trách, nhiệm vụ được giao (Kết quả KPI):</strong><br>
                - Điểm tự chấm KPI: <strong><?= $data['self_total_score'] ?? '---' ?> / 100 điểm</strong>.<br>
                - Điểm Quản lý thẩm định: <strong><?= $data['manager_total_score'] ?? '---' ?> / 100 điểm</strong>.<br>
                - Hoàn thành đầy đủ các nhiệm vụ trọng tâm theo kế hoạch công tác được giao đúng tiến độ, chất lượng.
            </p>
            <?php if (!empty($data['has_approved_initiative'])): ?>
                <p style="margin-top: 6px;"><strong>6. Sáng kiến cải tiến (BR-08):</strong> Có đề tài, sáng kiến kinh nghiệm được cấp có thẩm quyền công nhận và áp dụng hiệu quả vào thực tiễn quản lý, chuyên môn.</p>
            <?php endif; ?>
            <p style="margin-top: 6px;">
                <strong>7. Tự nhận mức xếp loại chất lượng:</strong> 
                <span style="font-weight: bold;"><?= htmlspecialchars($data['proposed_rank'] ?? 'Hoàn thành tốt nhiệm vụ') ?></span>
            </p>
        </div>
    </div>

    <!-- III. Ý kiến nhận xét của chi bộ & đoàn thể -->
    <div style="margin-bottom: 20px;">
        <div style="font-size: 13pt; font-weight: bold; text-transform: uppercase;">III. Ý KIẾN NHẬN XÉT CỦA CẤP ỦY NƠI CÔNG TÁC</div>
        <p style="font-size: 13pt; margin-top: 8px;">
            <?= !empty($data['party_opinion']) ? htmlspecialchars($data['party_opinion']) : 'Nhất trí với bản tự kiểm điểm của đồng chí. Trong năm công tác, đồng chí luôn giữ vững lập trường chính trị, hoàn thành tốt nhiệm vụ chuyên môn được giao.' ?>
        </p>
    </div>

    <!-- IV. Kết quả đánh giá của cấp có thẩm quyền -->
    <div style="margin-bottom: 32px;">
        <div style="font-size: 13pt; font-weight: bold; text-transform: uppercase;">IV. KẾT QUẢ ĐÁNH GIÁ, XẾP LOẠI CHẤT LƯỢNG CỦA CẤP CÓ THẨM QUYỀN</div>
        <div style="font-size: 13pt; margin-top: 8px;">
            <p>1. Điểm đánh giá chính thức: <strong><?= $data['final_score'] ?? $data['manager_total_score'] ?? '91.0' ?> / 100 điểm</strong></p>
            <p>2. Quyết định xếp loại chất lượng: <strong style="text-transform: uppercase; font-size: 13.5pt;"><?= htmlspecialchars($data['finalized_rank'] ?? 'HOÀN THÀNH TỐT NHIỆM VỤ') ?></strong></p>
            <p style="font-size: 11.5pt; font-style: italic; color: #555;">(Căn cứ quy định tại Nghị định số 90/2020/NĐ-CP và Nghị định số 48/2023/NĐ-CP của Chính phủ)</p>
        </div>
    </div>

    <!-- Chữ ký -->
    <div style="display: flex; justify-content: space-between; margin-top: 40px; page-break-inside: avoid;">
        <div style="text-align: center; width: 45%;">
            <div style="font-size: 13pt; font-weight: bold;">CÔNG CHỨC TỰ ĐÁNH GIÁ</div>
            <div style="font-size: 11pt; font-style: italic;">(Ký, ghi rõ họ tên)</div>
            <div style="height: 80px;"></div>
            <div style="font-size: 13pt; font-weight: bold;"><?= htmlspecialchars($data['full_name']) ?></div>
        </div>
        <div style="text-align: center; width: 45%;">
            <div style="font-size: 13pt; font-weight: bold;">THỦ TRƯỞNG CƠ QUAN, ĐƠN VỊ</div>
            <div style="font-size: 11pt; font-style: italic;">(Ký, ghi rõ họ tên và đóng dấu)</div>
            <div style="height: 80px;"></div>
            <div style="font-size: 13pt; font-weight: bold;">Trần Lãnh Đạo</div>
        </div>
    </div>
</div>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
