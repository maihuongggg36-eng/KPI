/**
 * manager-review.js
 * Quản lý tính toán điểm thẩm định của Quản lý, đối chiếu khoảng cách điểm,
 * kiểm soát quy tắc BR-05 (lệch > 10% bắt buộc giải trình >= 50 ký tự) và trả hồ sơ.
 */

document.addEventListener('DOMContentLoaded', function () {
    initManagerReviewEvents();
    recalculateManagerTotals();
});

function initManagerReviewEvents() {
    const table = document.getElementById('managerReviewTable');
    if (!table) return;

    // Lắng nghe thay đổi điểm thẩm định
    table.querySelectorAll('.manager-score-input').forEach(input => {
        input.addEventListener('input', function () {
            const row = this.closest('.mgr-eval-row');
            handleScoreChange(row);
            recalculateManagerTotals();
        });
    });

    // Lắng nghe đếm ký tự giải trình BR-05
    table.querySelectorAll('.gap-reason-textarea').forEach(textarea => {
        textarea.addEventListener('input', function () {
            const row = this.closest('.mgr-eval-row');
            handleReasonInput(row);
        });
    });
}

/**
 * Xử lý thay đổi điểm số và kiểm tra BR-05 gap
 */
function handleScoreChange(row) {
    const selfScore = parseFloat(row.dataset.selfScore) || 0;
    const mgrScoreInput = row.querySelector('.manager-score-input');
    const mgrScore = parseFloat(mgrScoreInput.value) || 0;

    const gap = Math.abs(selfScore - mgrScore);
    const gapBadge = row.querySelector('.gap-badge');
    const gapVal = row.querySelector('.gap-val');
    const gapReasonWrapper = row.querySelector('.gap-reason-wrapper');

    if (gapVal) gapVal.textContent = gap.toFixed(1);

    if (gap > 10.0) {
        mgrScoreInput.classList.add('border-danger', 'bg-danger-subtle');
        if (gapBadge) {
            gapBadge.className = 'badge bg-danger font-mono gap-badge';
        }
        if (gapReasonWrapper) {
            gapReasonWrapper.classList.remove('d-none');
        }
        handleReasonInput(row);
    } else {
        mgrScoreInput.classList.remove('border-danger', 'bg-danger-subtle');
        if (gapBadge) {
            gapBadge.className = 'badge bg-light text-muted font-mono gap-badge';
        }
        if (gapReasonWrapper) {
            gapReasonWrapper.classList.add('d-none');
        }
    }
}

/**
 * Kiểm soát bộ đếm ký tự giải trình BR-05 (>= 50 ký tự)
 */
function handleReasonInput(row) {
    const textarea = row.querySelector('.gap-reason-textarea');
    if (!textarea) return;

    const len = textarea.value.trim().length;
    const charCountEl = row.querySelector('.char-count');
    const charCounterEl = row.querySelector('.char-counter');

    if (charCountEl) charCountEl.textContent = len;

    if (len >= 50) {
        if (charCounterEl) {
            charCounterEl.classList.remove('text-danger');
            charCounterEl.classList.add('text-success');
        }
        textarea.classList.remove('border-danger');
    } else {
        if (charCounterEl) {
            charCounterEl.classList.remove('text-success');
            charCounterEl.classList.add('text-danger');
        }
        textarea.classList.add('border-danger');
    }
}

/**
 * Tính tổng điểm Quản lý thẩm định và số lượng chỉ tiêu vi phạm
 */
function recalculateManagerTotals() {
    let grandMgrTotal = 0;
    let br05ViolationCount = 0;

    const rows = document.querySelectorAll('.mgr-eval-row');
    rows.forEach(row => {
        const weight = parseFloat(row.dataset.weight) || 0;
        const mgrScore = parseFloat(row.querySelector('.manager-score-input')?.value) || 0;
        const selfScore = parseFloat(row.dataset.selfScore) || 0;
        const gap = Math.abs(selfScore - mgrScore);

        grandMgrTotal += (mgrScore * weight) / 100.0;

        if (gap > 10.0) {
            const reason = row.querySelector('.gap-reason-textarea')?.value?.trim() || '';
            if (reason.length < 50) {
                br05ViolationCount++;
            }
        }
    });

    const display = document.getElementById('grandManagerScoreDisplay');
    const counterBadge = document.getElementById('br05GapWarningCount');

    if (display) display.textContent = grandMgrTotal.toFixed(1);
    if (counterBadge) {
        counterBadge.textContent = br05ViolationCount;
        counterBadge.className = br05ViolationCount > 0 ? 'badge bg-danger rounded-pill px-2' : 'badge bg-success rounded-pill px-2';
    }
}

/**
 * Gửi thẩm định Quản lý (Lưu nháp / Hoàn tất)
 */
async function submitManagerReview(isDraft) {
    const items = [];
    let br05Error = null;

    const rows = document.querySelectorAll('.mgr-eval-row');
    rows.forEach(row => {
        const detailId = parseInt(row.dataset.detailId);
        const mgrScore = parseFloat(row.querySelector('.manager-score-input').value) || 0;
        const remarks = row.querySelector('.manager-remark-input')?.value || '';
        const gapReason = row.querySelector('.gap-reason-textarea')?.value || '';
        const selfScore = parseFloat(row.dataset.selfScore) || 0;
        const gap = Math.abs(selfScore - mgrScore);

        if (!isDraft && gap > 10.0 && gapReason.trim().length < 50) {
            if (!br05Error) {
                const kpiName = row.querySelector('.fw-semibold')?.textContent || 'Chỉ tiêu';
                br05Error = `Chỉ tiêu '${kpiName}' có độ lệch điểm > 10 điểm (Lệch: ${gap.toFixed(1)}). Bắt buộc phải có lý do giải trình tối thiểu 50 ký tự theo quy định BR-05! (Hiện tại: ${gapReason.trim().length}/50 ký tự)`;
                row.querySelector('.gap-reason-textarea')?.focus();
            }
        }

        items.push({
            detailId: detailId,
            managerScore: mgrScore,
            managerRemarks: remarks,
            scoreGapReason: gapReason
        });
    });

    if (br05Error) {
        showEnterpriseToast(br05Error, 'danger');
        return;
    }

    const payload = {
        evaluationId: window.evaluationData.evaluationId,
        overallRemarks: document.getElementById('managerOverallRemarks')?.value || '',
        isDraft: isDraft,
        items: items
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const btn = isDraft ? document.getElementById('btnSaveManagerDraft') : document.getElementById('btnSubmitManagerFinal');
    if (btn) btn.disabled = true;

    try {
        const response = await fetch('/Evaluation/SubmitManagerReview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });

        const res = await response.json();
        if (res.success) {
            showEnterpriseToast(res.message, 'success');
            setTimeout(() => {
                window.location.href = '/Evaluation/ManagerList';
            }, 1200);
        } else {
            showEnterpriseToast(res.message || 'Lỗi khi lưu kết quả thẩm định', 'danger');
            if (btn) btn.disabled = false;
        }
    } catch (err) {
        showEnterpriseToast('Lỗi hệ thống: ' + err.message, 'danger');
        if (btn) btn.disabled = false;
    }
}

function openReturnModal() {
    const modal = new bootstrap.Modal(document.getElementById('returnToStaffModal'));
    modal.show();
}

/**
 * Trả hồ sơ lại cho nhân viên
 */
async function performReturnToStaff() {
    const reason = document.getElementById('returnReasonTextarea')?.value?.trim();
    if (!reason) {
        showEnterpriseToast('Vui lòng nhập lý do trả lại hồ sơ.', 'warning');
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    try {
        const response = await fetch('/Evaluation/ReturnToStaff', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                evaluationId: window.evaluationData.evaluationId,
                reason: reason
            })
        });

        const res = await response.json();
        if (res.success) {
            showEnterpriseToast(res.message, 'success');
            setTimeout(() => {
                window.location.href = '/Evaluation/ManagerList';
            }, 1200);
        } else {
            showEnterpriseToast(res.message || 'Lỗi khi trả lại hồ sơ', 'danger');
        }
    } catch (err) {
        showEnterpriseToast('Lỗi kết nối: ' + err.message, 'danger');
    }
}

function showEnterpriseToast(message, type = 'info') {
    const toastDiv = document.createElement('div');
    toastDiv.className = `alert alert-${type} position-fixed top-0 end-0 m-3 shadow-lg z-3`;
    toastDiv.style.minWidth = '320px';
    toastDiv.innerHTML = `
        <div class="d-flex align-items-center justify-content-between">
            <span>${message}</span>
            <button type="button" class="btn-close ms-2" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
    `;
    document.body.appendChild(toastDiv);
    setTimeout(() => toastDiv.remove(), 4000);
}
