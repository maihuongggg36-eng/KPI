/**
 * Module Lương & Chi Tiền Mặt (3P Performance Payroll)
 * Quản lý tính toán P3, chi tiền mặt Mẫu 02-TT & Mẫu 05a
 */

document.addEventListener('DOMContentLoaded', function () {
    const periodId = window.payrollConfig ? window.payrollConfig.periodId : 1;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // 1. TÍNH LƯƠNG TOÀN BỘ (BATCH CALCULATE)
    const btnCalculateAll = document.getElementById('btnCalculateAll');
    if (btnCalculateAll) {
        btnCalculateAll.addEventListener('click', async function () {
            if (!confirm('Bạn có chắc chắn muốn TÍNH TOÁN LẠI TOÀN BỘ BẢNG LƯƠNG theo kết quả đánh giá KPI cho kỳ này?')) {
                return;
            }

            const originalHtml = btnCalculateAll.innerHTML;
            btnCalculateAll.disabled = true;
            btnCalculateAll.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang tính toán...';

            try {
                const response = await fetch('/Payroll/Calculate', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'X-CSRF-TOKEN': token
                    },
                    body: JSON.stringify({ periodId: periodId })
                });

                const res = await response.json();
                if (res.success) {
                    showToast('Thành công', res.message || 'Đã tính toán bảng lương thành công!', 'success');
                    setTimeout(() => location.reload(), 1000);
                } else {
                    showToast('Lỗi', res.message || 'Không thể tính lương', 'danger');
                    btnCalculateAll.disabled = false;
                    btnCalculateAll.innerHTML = originalHtml;
                }
            } catch (err) {
                console.error(err);
                showToast('Lỗi kết nối', 'Không thể gửi yêu cầu tính lương: ' + err.message, 'danger');
                btnCalculateAll.disabled = false;
                btnCalculateAll.innerHTML = originalHtml;
            }
        });
    }

    // 2. CHI TIỀN MẶT HÀNG LOẠT (BATCH DISBURSE)
    const btnPayAllCash = document.getElementById('btnPayAllCash');
    if (btnPayAllCash) {
        btnPayAllCash.addEventListener('click', async function () {
            if (!confirm('Xác nhận: Tất cả cán bộ chưa nhận tiền sẽ được chuyển sang trạng thái "ĐÃ THANH TOÁN TIỀN MẶT"?')) {
                return;
            }

            const originalHtml = btnPayAllCash.innerHTML;
            btnPayAllCash.disabled = true;
            btnPayAllCash.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang giải ngân...';

            try {
                const response = await fetch('/Payroll/PayAllCash', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'X-CSRF-TOKEN': token
                    },
                    body: JSON.stringify({ periodId: periodId })
                });

                const res = await response.json();
                if (res.success) {
                    showToast('Thành công', res.message || 'Đã giải ngân tiền mặt thành công!', 'success');
                    setTimeout(() => location.reload(), 1000);
                } else {
                    showToast('Lỗi', res.message || 'Không thể giải ngân tiền mặt', 'danger');
                    btnPayAllCash.disabled = false;
                    btnPayAllCash.innerHTML = originalHtml;
                }
            } catch (err) {
                console.error(err);
                showToast('Lỗi kết nối', 'Không thể thực hiện chi tiền mặt: ' + err.message, 'danger');
                btnPayAllCash.disabled = false;
                btnPayAllCash.innerHTML = originalHtml;
            }
        });
    }

    // 3. CHI TIỀN MẶT ĐƠN LẺ QUA MODAL
    let modalPayCashEl = document.getElementById('modalPayCash');
    let payModal = modalPayCashEl && typeof bootstrap !== 'undefined' ? new bootstrap.Modal(modalPayCashEl) : null;

    document.querySelectorAll('.btn-pay-single').forEach(btn => {
        btn.addEventListener('click', function () {
            const payrollId = this.getAttribute('data-id');
            const name = this.getAttribute('data-name');
            const amount = this.getAttribute('data-amount');

            document.getElementById('payModalPayrollId').value = payrollId;
            document.getElementById('payModalEmployeeName').textContent = name;
            document.getElementById('payModalAmount').textContent = amount + ' đ';
            document.getElementById('payModalSignature').value = 'Đã nhận đủ tiền mặt tại quỹ bởi ' + name;

            if (payModal) {
                payModal.show();
            }
        });
    });

    const btnConfirmPayCash = document.getElementById('btnConfirmPayCash');
    if (btnConfirmPayCash) {
        btnConfirmPayCash.addEventListener('click', async function () {
            const payrollId = parseInt(document.getElementById('payModalPayrollId').value);
            const signature = document.getElementById('payModalSignature').value;

            btnConfirmPayCash.disabled = true;
            btnConfirmPayCash.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang xử lý...';

            try {
                const response = await fetch('/Payroll/PayCash', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'X-CSRF-TOKEN': token
                    },
                    body: JSON.stringify({
                        payrollId: payrollId,
                        signature: signature
                    })
                });

                const res = await response.json();
                if (res.success) {
                    if (payModal) payModal.hide();
                    showToast('Thành công', 'Đã xác nhận chi tiền mặt cho cán bộ!', 'success');
                    setTimeout(() => location.reload(), 800);
                } else {
                    showToast('Lỗi', res.message || 'Không thể chi tiền mặt', 'danger');
                    btnConfirmPayCash.disabled = false;
                    btnConfirmPayCash.innerHTML = '<i class="bi bi-check2 me-1"></i>Xác Nhận Đã Chi Tiền';
                }
            } catch (err) {
                console.error(err);
                showToast('Lỗi kết nối', err.message, 'danger');
                btnConfirmPayCash.disabled = false;
                btnConfirmPayCash.innerHTML = '<i class="bi bi-check2 me-1"></i>Xác Nhận Đã Chi Tiền';
            }
        });
    }

    // 4. TÌM KIẾM NHANH TRÊN GRID TABLE
    const searchInput = document.getElementById('txtQuickSearchPayroll');
    if (searchInput) {
        searchInput.addEventListener('keyup', function () {
            const term = this.value.toLowerCase().trim();
            const rows = document.querySelectorAll('#tblPayroll tbody tr');

            rows.forEach(row => {
                const text = row.textContent.toLowerCase();
                if (text.includes(term)) {
                    row.style.display = '';
                } else {
                    row.style.display = 'none';
                }
            });
        });
    }

    // Helper Toast
    function showToast(title, message, type) {
        if (window.ToastManager) {
            window.ToastManager.show(title, message, type);
        } else {
            alert(`${title}: ${message}`);
        }
    }
});
