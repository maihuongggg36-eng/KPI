<?php
$pageTitle = 'Báo cáo Tổng hợp & Phân bổ Gauss | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header no-print">
    <div class="page-title">
        <h1>Báo cáo Tổng hợp Đánh giá & Phân bổ Xếp loại</h1>
        <p>Phân tích hiệu suất theo hình chuông Gauss chuẩn (BR-14) và tổng hợp theo từng đơn vị, phòng ban</p>
    </div>
    <div class="page-actions">
        <a href="/reports/export-csv" class="btn btn-secondary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
            <span>Xuất CSV (Excel)</span>
        </a>
        <button onclick="window.print()" class="btn btn-primary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"></polyline><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path><rect x="6" y="14" width="12" height="8"></rect></svg>
            <span>In Báo Cáo</span>
        </button>
    </div>
</div>

<!-- Phân tích Chuông Gauss (BR-14) -->
<div class="card">
    <div class="card-header">
        <h2>Phân bổ Kết quả Xếp loại Thực tế so với Chuẩn Phân phối Gauss (BR-14)</h2>
        <span class="badge badge-primary">Mô hình phân phối chuẩn</span>
    </div>
    <div class="card-body">
        <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; text-align: center;">
            <div style="background: #ecfdf5; padding: 20px; border-radius: var(--radius-md); border: 1px solid #a7f3d0;">
                <div style="font-size: 13px; font-weight: 700; color: #065f46; text-transform: uppercase;">1. Hoàn thành Xuất sắc</div>
                <div style="font-size: 32px; font-weight: 800; color: #047857; margin: 8px 0;"><?= $stats['excellent_rate'] ?? 0 ?>%</div>
                <div style="font-size: 12px; color: #065f46;">Chuẩn mục tiêu: <strong>10% - 15%</strong></div>
            </div>

            <div style="background: #eff6ff; padding: 20px; border-radius: var(--radius-md); border: 1px solid #bfdbfe;">
                <div style="font-size: 13px; font-weight: 700; color: #1e40af; text-transform: uppercase;">2. Hoàn thành Tốt</div>
                <div style="font-size: 32px; font-weight: 800; color: #1d4ed8; margin: 8px 0;"><?= $stats['good_rate'] ?? 0 ?>%</div>
                <div style="font-size: 12px; color: #1e40af;">Chuẩn mục tiêu: <strong>65% - 70%</strong></div>
            </div>

            <div style="background: #fffbeb; padding: 20px; border-radius: var(--radius-md); border: 1px solid #fde68a;">
                <div style="font-size: 13px; font-weight: 700; color: #92400e; text-transform: uppercase;">3. Hoàn thành Nhiệm vụ</div>
                <div style="font-size: 32px; font-weight: 800; color: #d97706; margin: 8px 0;"><?= $stats['satisfactory_rate'] ?? 0 ?>%</div>
                <div style="font-size: 12px; color: #92400e;">Chuẩn mục tiêu: <strong>10% - 15%</strong></div>
            </div>

            <div style="background: #fef2f2; padding: 20px; border-radius: var(--radius-md); border: 1px solid #fecaca;">
                <div style="font-size: 13px; font-weight: 700; color: #991b1b; text-transform: uppercase;">4. Không hoàn thành</div>
                <div style="font-size: 32px; font-weight: 800; color: #dc2626; margin: 8px 0;"><?= $stats['unsatisfactory_rate'] ?? 0 ?>%</div>
                <div style="font-size: 12px; color: #991b1b;">Chuẩn mục tiêu: <strong>≤ 5%</strong></div>
            </div>
        </div>
    </div>
</div>

<!-- Bảng tổng hợp theo phòng ban -->
<div class="card">
    <div class="card-header">
        <h2>Tổng hợp Theo Đơn vị & Phòng ban</h2>
    </div>
    <div class="card-body" style="padding: 0;">
        <table class="table">
            <thead>
                <tr>
                    <th>Tên Phòng ban</th>
                    <th>Tổng nhân sự</th>
                    <th>Đã đánh giá</th>
                    <th>Điểm TB</th>
                    <th>Xuất sắc</th>
                    <th>Hoàn thành tốt</th>
                    <th>Hoàn thành</th>
                    <th>Không đạt</th>
                </tr>
            </thead>
            <tbody>
                <?php foreach ($deptStats as $deptName => $d): 
                    $avg = $d['evaluated'] > 0 ? round($d['total_score'] / $d['evaluated'], 1) : 0;
                ?>
                    <tr>
                        <td><strong><?= htmlspecialchars($deptName) ?></strong></td>
                        <td><?= $d['total'] ?></td>
                        <td><span class="badge badge-primary"><?= $d['evaluated'] ?></span></td>
                        <td><strong style="color: var(--primary); font-size: 15px;"><?= $avg ?></strong></td>
                        <td><span class="badge badge-success"><?= $d['excellent'] ?></span></td>
                        <td><span class="badge badge-primary"><?= $d['good'] ?></span></td>
                        <td><span class="badge badge-warning"><?= $d['satisfactory'] ?></span></td>
                        <td><span class="badge badge-danger"><?= $d['unsatisfactory'] ?></span></td>
                    </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Danh sách chi tiết cá nhân -->
<div class="card">
    <div class="card-header">
        <h2>Bảng Xếp Hạng Kết Quả Đánh Giá Cá Nhân</h2>
    </div>
    <div class="card-body" style="padding: 0;">
        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th style="width: 50px;">Hạng</th>
                        <th>Họ và tên</th>
                        <th>Phòng ban</th>
                        <th>Chức danh</th>
                        <th>Điểm Tự chấm</th>
                        <th>Điểm QL chấm</th>
                        <th>Điểm Phê duyệt</th>
                        <th>Mức xếp loại</th>
                        <th>Trạng thái</th>
                    </tr>
                </thead>
                <tbody>
                    <?php $rank = 1; foreach ($rows as $r): ?>
                        <tr>
                            <td><strong>#<?= $rank++ ?></strong></td>
                            <td>
                                <strong><?= htmlspecialchars($r['full_name']) ?></strong>
                                <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($r['username']) ?></div>
                            </td>
                            <td><?= htmlspecialchars($r['department_name']) ?></td>
                            <td><?= htmlspecialchars($r['position_name']) ?></td>
                            <td><?= $r['self_total_score'] ?? '---' ?></td>
                            <td><?= $r['manager_total_score'] ?? '---' ?></td>
                            <td>
                                <strong style="font-size: 15px; color: var(--primary);"><?= $r['final_score'] ?? '---' ?></strong>
                            </td>
                            <td>
                                <span class="badge badge-success"><?= htmlspecialchars($r['final_classification'] ?? 'Chưa xếp loại') ?></span>
                            </td>
                            <td>
                                <span class="badge badge-secondary"><?= htmlspecialchars($r['eval_status'] ?? 'Chưa đánh giá') ?></span>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
