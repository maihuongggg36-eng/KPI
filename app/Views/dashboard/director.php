<?php
$pageTitle = 'Tổng quan Lãnh đạo | KPI Master';
require BASE_PATH . '/app/Views/layouts/header.php';
?>

<div class="page-header">
    <div class="page-title">
        <h1>Bàn làm việc Lãnh đạo & Giám đốc</h1>
        <p>Tổng hợp tiến độ đánh giá, kiểm soát tỷ lệ Nghị định 90 và phê chuẩn kết quả xếp loại</p>
    </div>
    <div class="page-actions">
        <a href="/civil-servant" class="btn btn-primary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line></svg>
            <span>Duyệt Nghị định 90/2020</span>
        </a>
    </div>
</div>

<!-- Thống kê tổng quan -->
<div class="stats-grid">
    <div class="stat-card">
        <div class="stat-info">
            <p>Tổng số bản KPI</p>
            <h3><?= $stats['total'] ?? 0 ?></h3>
            <span>Đã giao trong kỳ</span>
        </div>
        <div class="stat-icon primary">📋</div>
    </div>

    <div class="stat-card">
        <div class="stat-info">
            <p>Chờ Lãnh đạo duyệt</p>
            <h3><?= $stats['pending_approval'] ?? 0 ?></h3>
            <span style="color: var(--warning); font-weight: 600;">Cần phê chuẩn kết luận</span>
        </div>
        <div class="stat-icon warning">⚖</div>
    </div>

    <div class="stat-card">
        <div class="stat-info">
            <p>Đã phê duyệt hoàn tất</p>
            <h3><?= $stats['completed'] ?? 0 ?></h3>
            <span style="color: var(--success); font-weight: 600;">Đã đóng băng dữ liệu</span>
        </div>
        <div class="stat-icon success">✓</div>
    </div>

    <div class="stat-card">
        <div class="stat-info">
            <p>Điểm TB Toàn cơ quan</p>
            <h3><?= $stats['avg_score'] ?? '0.0' ?></h3>
            <span>Thang điểm chuẩn 100</span>
        </div>
        <div class="stat-icon primary">★</div>
    </div>
</div>

<!-- Danh sách hồ sơ cần phê duyệt -->
<div class="card">
    <div class="card-header">
        <h2>Danh sách Bản đánh giá cần Phê duyệt & Đóng băng kết quả</h2>
        <a href="/reports" class="btn btn-secondary btn-sm">Xem Báo cáo Tổng thể</a>
    </div>
    <div class="card-body" style="padding: 0;">
        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th>Họ tên Cán bộ / Nhân viên</th>
                        <th>Phòng ban</th>
                        <th>Chức danh</th>
                        <th>Điểm Tự chấm</th>
                        <th>Điểm QL Thẩm định</th>
                        <th>Trạng thái</th>
                        <th>Hành động</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (!empty($recentAssignments)): ?>
                        <?php foreach ($recentAssignments as $row): ?>
                            <tr>
                                <td>
                                    <strong><?= htmlspecialchars($row['employee_name'] ?? '') ?></strong>
                                    <div style="font-size: 11px; color: var(--text-muted);"><?= htmlspecialchars($row['employee_code'] ?? '') ?></div>
                                </td>
                                <td><?= htmlspecialchars($row['department_name'] ?? '') ?></td>
                                <td><?= htmlspecialchars($row['position_name'] ?? '') ?></td>
                                <td>
                                    <span class="badge badge-secondary"><?= $row['self_total_score'] ?? 'Chưa chấm' ?></span>
                                </td>
                                <td>
                                    <span class="badge badge-primary"><?= $row['manager_total_score'] ?? 'Chưa chấm' ?></span>
                                </td>
                                <td>
                                    <?php 
                                    $eStatus = $row['eval_status'] ?? 'Chưa nộp';
                                    if ($eStatus === 'Completed'): ?>
                                        <span class="badge badge-success">Đã phê chuẩn</span>
                                    <?php elseif ($eStatus === 'Manager_Reviewed'): ?>
                                        <span class="badge badge-warning">Chờ Lãnh đạo duyệt</span>
                                    <?php else: ?>
                                        <span class="badge badge-secondary"><?= htmlspecialchars($eStatus) ?></span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <?php if (($row['eval_status'] ?? '') === 'Manager_Reviewed'): ?>
                                        <button class="btn btn-success btn-sm" onclick="openApprovalModal(<?= $row['eval_id'] ?? 0 ?>, '<?= htmlspecialchars($row['employee_name'] ?? '') ?>', <?= $row['manager_total_score'] ?? 0 ?>)">
                                            Phê chuẩn
                                        </button>
                                    <?php else: ?>
                                        <a href="/civil-servant/form?user_id=<?= $row['employee_id'] ?>" class="btn btn-secondary btn-sm">
                                            Xem Phiếu
                                        </a>
                                    <?php endif; ?>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    <?php else: ?>
                        <tr><td colspan="7" style="text-align: center; color: var(--text-muted); padding: 32px;">Không có hồ sơ nào trong kỳ này.</td></tr>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- Modal Phê chuẩn kết quả -->
<div id="approvalModal" style="display: none; position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 1000; align-items: center; justify-content: center;">
    <div style="background: white; border-radius: var(--radius-lg); padding: 32px; width: 100%; max-width: 480px; box-shadow: var(--shadow-xl);">
        <h3 style="font-size: 18px; font-weight: 700; margin-bottom: 8px;">Phê chuẩn & Đóng băng kết quả</h3>
        <p style="font-size: 13px; color: var(--text-secondary); margin-bottom: 20px;">
            Cán bộ: <strong id="modal-emp-name"></strong>
        </p>

        <form method="POST" action="/evaluation/approve">
            <input type="hidden" name="evaluation_id" id="modal-eval-id">
            
            <div class="form-group">
                <label>Điểm thẩm định của Quản lý</label>
                <input type="number" step="0.1" name="final_score" id="modal-final-score" class="form-control" required>
            </div>

            <div class="form-group">
                <label>Mức xếp loại phê chuẩn cuối cùng (BR-15)</label>
                <select name="final_classification" class="form-control" required>
                    <option value="Hoàn thành xuất sắc nhiệm vụ">Hoàn thành xuất sắc nhiệm vụ</option>
                    <option value="Hoàn thành tốt nhiệm vụ" selected>Hoàn thành tốt nhiệm vụ</option>
                    <option value="Hoàn thành nhiệm vụ">Hoàn thành nhiệm vụ</option>
                    <option value="Không hoàn thành nhiệm vụ">Không hoàn thành nhiệm vụ</option>
                </select>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 12px; margin-top: 24px;">
                <button type="button" class="btn btn-secondary" onclick="closeApprovalModal()">Hủy</button>
                <button type="submit" class="btn btn-primary">Xác nhận Phê chuẩn</button>
            </div>
        </form>
    </div>
</div>

<script>
function openApprovalModal(evalId, empName, score) {
    document.getElementById('modal-eval-id').value = evalId;
    document.getElementById('modal-emp-name').textContent = empName;
    document.getElementById('modal-final-score').value = score;
    const modal = document.getElementById('approvalModal');
    modal.style.display = 'flex';
}
function closeApprovalModal() {
    document.getElementById('approvalModal').style.display = 'none';
}
</script>

<?php require BASE_PATH . '/app/Views/layouts/footer.php'; ?>
