/**
 * self-assessment.js
 * Quản lý tính toán điểm tự đánh giá thời gian thực (BR-10, BR-09),
 * upload minh chứng số hóa kéo thả và nộp phiếu tự chấm.
 */

document.addEventListener('DOMContentLoaded', function () {
    initRealtimeCalculation();
    initDropZone();
    recalculateAll();
});

let currentActiveDetailId = null;
let selectedEvidenceFile = null;

/**
 * Khởi tạo lắng nghe sự kiện thay đổi kết quả thực tế
 */
function initRealtimeCalculation() {
    const table = document.getElementById('selfAssessmentTable');
    if (!table) return;

    table.querySelectorAll('.actual-value-input').forEach(input => {
        input.addEventListener('input', function () {
            const row = this.closest('.kpi-eval-row');
            calculateRow(row);
            recalculateGrandTotal();
        });
    });
}

/**
 * Tính toán cho từng hàng chỉ tiêu (BR-10, BR-09)
 */
function calculateRow(row) {
    const target = parseFloat(row.dataset.target) || 0;
    const weight = parseFloat(row.dataset.weight) || 0;
    const maxCap = parseFloat(row.dataset.maxcap) || 150.0;
    const formula = row.dataset.formula || 'HigherIsBetter';

    const actualInput = row.querySelector('.actual-value-input');
    const actual = parseFloat(actualInput.value) || 0;

    let completionRate = 0;
    if (target > 0) {
        if (formula.toLowerCase() === 'lowerisbetter') {
            completionRate = (target / Math.max(actual, 0.001)) * 100.0;
        } else {
            completionRate = (actual / target) * 100.0;
        }
    }

    // Chặn trần MaxCap theo BR-10
    if (completionRate > maxCap) {
        completionRate = maxCap;
    }
    if (completionRate < 0) completionRate = 0;

    const rateFormatted = completionRate.toFixed(1);
    const selfScore = completionRate; // Thang 100/150 của chỉ tiêu
    const weightedScore = (selfScore * weight) / 100.0;

    // Cập nhật DOM
    const rateBadge = row.querySelector('.completion-rate-badge');
    const rateVal = row.querySelector('.rate-val');
    const selfScoreVal = row.querySelector('.self-score-val');
    const weightedScoreVal = row.querySelector('.weighted-score-val');

    if (rateVal) rateVal.textContent = rateFormatted;
    if (selfScoreVal) selfScoreVal.textContent = selfScore.toFixed(1);
    if (weightedScoreVal) weightedScoreVal.textContent = weightedScore.toFixed(1);

    // Xử lý cảnh báo BR-09 (>= 120%)
    let br09Alert = row.querySelector('.br09-alert-badge');
    if (completionRate >= 120.0) {
        if (rateBadge) {
            rateBadge.className = 'font-mono fw-bold completion-rate-badge badge bg-warning text-dark border border-warning px-2 py-1';
        }
        if (!br09Alert) {
            br09Alert = document.createElement('div');
            br09Alert.className = 'br09-alert-badge mt-1 text-danger small fw-bold';
            br09Alert.style.fontSize = '11px';
            br09Alert.innerHTML = '<i class="bi bi-exclamation-triangle-fill text-warning me-1"></i>Vượt trần!';
            row.querySelector('td:nth-child(6)').appendChild(br09Alert);
        }
    } else {
        if (rateBadge) {
            if (completionRate >= 100.0) {
                rateBadge.className = 'font-mono fw-bold completion-rate-badge badge bg-success-subtle text-success px-2 py-1';
            } else {
                rateBadge.className = 'font-mono fw-bold completion-rate-badge badge bg-light text-secondary px-2 py-1';
            }
        }
        if (br09Alert) br09Alert.remove();
    }
}

/**
 * Tính lại toàn bộ bảng và cập nhật tổng điểm
 */
function recalculateAll() {
    const rows = document.querySelectorAll('.kpi-eval-row');
    rows.forEach(row => calculateRow(row));
    recalculateGrandTotal();
}

/**
 * Tính tổng điểm Grand Total Score
 */
function recalculateGrandTotal() {
    let grandTotal = 0;
    let missingEvidenceCount = 0;

    const rows = document.querySelectorAll('.kpi-eval-row');
    rows.forEach(row => {
        const weightedScoreVal = row.querySelector('.weighted-score-val');
        if (weightedScoreVal) {
            grandTotal += parseFloat(weightedScoreVal.textContent) || 0;
        }

        const rateVal = parseFloat(row.querySelector('.rate-val')?.textContent) || 0;
        const evidenceContainer = row.querySelector('.evidence-container');
        const hasEvidence = evidenceContainer && evidenceContainer.querySelectorAll('.evidence-file-tag').length > 0;

        if (rateVal >= 120.0 && !hasEvidence) {
            missingEvidenceCount++;
        }
    });

    const grandScoreDisplay = document.getElementById('grandSelfScoreDisplay');
    const grandProgressBar = document.getElementById('grandSelfProgressBar');
    const warningCount = document.getElementById('evidenceWarningCount');

    if (grandScoreDisplay) {
        grandScoreDisplay.textContent = grandTotal.toFixed(1);
    }
    if (grandProgressBar) {
        const pct = Math.min(100, Math.max(0, grandTotal));
        grandProgressBar.style.width = pct + '%';
    }
    if (warningCount) {
        warningCount.textContent = missingEvidenceCount;
    }
}

/**
 * Khởi tạo Drag & Drop Zone cho upload minh chứng
 */
function initDropZone() {
    const dropZone = document.getElementById('evidenceDropZone');
    const fileInput = document.getElementById('evidenceFileInput');
    if (!dropZone || !fileInput) return;

    dropZone.addEventListener('click', () => fileInput.click());

    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.classList.add('border-primary', 'bg-primary-subtle');
    });

    dropZone.addEventListener('dragleave', () => {
        dropZone.classList.remove('border-primary', 'bg-primary-subtle');
    });

    dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.classList.remove('border-primary', 'bg-primary-subtle');
        if (e.dataTransfer.files.length > 0) {
            handleFileSelection(e.dataTransfer.files[0]);
        }
    });

    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) {
            handleFileSelection(e.target.files[0]);
        }
    });
}

function openUploadModal(detailId, kpiName) {
    currentActiveDetailId = detailId;
    clearSelectedFile();
    document.getElementById('modalKpiNameDisplay').textContent = kpiName;
    const modal = new bootstrap.Modal(document.getElementById('uploadEvidenceModal'));
    modal.show();
}

function handleFileSelection(file) {
    if (file.size > 25 * 1024 * 1024) {
        showToast('Dung lượng tệp vượt quá giới hạn 25MB.', 'danger');
        return;
    }

    selectedEvidenceFile = file;
    document.getElementById('fileSelectedInfo').classList.remove('d-none');
    document.getElementById('selectedFileNameDisplay').textContent = file.name;
    document.getElementById('selectedFileSizeDisplay').textContent = `(${(file.size / 1024).toFixed(1)} KB)`;
}

function clearSelectedFile() {
    selectedEvidenceFile = null;
    const fileInput = document.getElementById('evidenceFileInput');
    if (fileInput) fileInput.value = '';
    const info = document.getElementById('fileSelectedInfo');
    if (info) info.classList.add('d-none');
    const desc = document.getElementById('evidenceDescriptionInput');
    if (desc) desc.value = '';
}

/**
 * Thực thi Upload Evidence qua AJAX
 */
async function performUploadEvidence() {
    if (!selectedEvidenceFile || !currentActiveDetailId) {
        showToast('Vui lòng chọn một tệp tài liệu minh chứng.', 'warning');
        return;
    }

    const btn = document.getElementById('btnUploadEvidenceSubmit');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang tải lên...';

    const formData = new FormData();
    formData.append('assignmentDetailId', currentActiveDetailId);
    formData.append('description', document.getElementById('evidenceDescriptionInput').value);
    formData.append('file', selectedEvidenceFile);

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    try {
        const response = await fetch('/Evaluation/UploadEvidence', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });

        const res = await response.json();
        if (res.success) {
            showToast(res.message, 'success');
            appendEvidenceToDOM(currentActiveDetailId, res.evidence);
            bootstrap.Modal.getInstance(document.getElementById('uploadEvidenceModal')).hide();
            clearSelectedFile();
            recalculateGrandTotal();
        } else {
            showToast(res.message || 'Lỗi khi tải tệp lên', 'danger');
        }
    } catch (err) {
        showToast('Lỗi kết nối khi tải tệp: ' + err.message, 'danger');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-upload me-1"></i>Tải lên';
    }
}

function appendEvidenceToDOM(detailId, ev) {
    const container = document.getElementById('evidenceList_' + detailId);
    if (!container) return;

    let list = container.querySelector('.d-flex.flex-column');
    if (!list) {
        list = document.createElement('div');
        list.className = 'd-flex flex-column gap-1 mb-2';
        container.appendChild(list);
    }

    const item = document.createElement('div');
    item.className = 'evidence-file-tag d-flex align-items-center justify-content-between p-1 px-2 rounded border bg-light-subtle small';
    item.id = 'evidenceItem_' + ev.id;
    item.innerHTML = `
        <a href="${ev.filePath}" target="_blank" class="text-truncate text-primary text-decoration-none" style="max-width: 140px;" title="${ev.fileName}">
            <i class="bi bi-file-earmark-check-fill me-1 text-success"></i>${ev.fileName}
        </a>
        <button type="button" class="btn btn-link btn-sm text-danger p-0 ms-2" onclick="deleteEvidenceFile(${ev.id}, ${detailId})">
            <i class="bi bi-x-circle"></i>
        </button>
    `;
    list.appendChild(item);
}

/**
 * Xóa tài liệu minh chứng
 */
async function deleteEvidenceFile(evidenceId, detailId) {
    if (!confirm('Bạn có chắc chắn muốn xóa tệp minh chứng này?')) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    try {
        const response = await fetch('/Evaluation/DeleteEvidence', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ evidenceId: evidenceId })
        });

        const res = await response.json();
        if (res.success) {
            document.getElementById('evidenceItem_' + evidenceId)?.remove();
            showToast('Đã xóa tệp minh chứng.', 'info');
            recalculateGrandTotal();
        } else {
            showToast(res.message || 'Lỗi khi xóa tệp', 'danger');
        }
    } catch (err) {
        showToast('Lỗi kết nối: ' + err.message, 'danger');
    }
}

function confirmSubmitFinal() {
    // Kiểm tra BR-09 trước khi mở modal
    let hasBr09Violation = false;
    let violatingKpiName = '';

    const rows = document.querySelectorAll('.kpi-eval-row');
    rows.forEach(row => {
        const rate = parseFloat(row.querySelector('.rate-val')?.textContent) || 0;
        const evContainer = row.querySelector('.evidence-container');
        const count = evContainer ? evContainer.querySelectorAll('.evidence-file-tag').length : 0;

        if (rate >= 120.0 && count === 0) {
            hasBr09Violation = true;
            violatingKpiName = row.querySelector('.fw-semibold')?.textContent || 'Chỉ tiêu';
            row.classList.add('table-danger');
        } else {
            row.classList.remove('table-danger');
        }
    });

    if (hasBr09Violation) {
        showToast(`Vi phạm BR-09: Chỉ tiêu '${violatingKpiName}' đạt >= 120% nhưng chưa có tệp minh chứng. Bắt buộc tải lên minh chứng trước khi nộp chính thức!`, 'warning');
        return;
    }

    const modal = new bootstrap.Modal(document.getElementById('confirmSubmitModal'));
    modal.show();
}

/**
 * Gửi phiếu tự đánh giá (Lưu nháp / Nộp chính thức)
 */
async function submitAssessment(isDraft) {
    const items = [];
    const rows = document.querySelectorAll('.kpi-eval-row');

    rows.forEach(row => {
        const detailId = parseInt(row.dataset.detailId);
        const actual = parseFloat(row.querySelector('.actual-value-input').value) || 0;
        const remarks = row.querySelector('.self-remark-input')?.value || '';

        items.push({
            assignmentDetailId: detailId,
            actualValue: actual,
            selfRemarks: remarks
        });
    });

    const payload = {
        evaluationId: window.evaluationData.evaluationId,
        assignmentId: window.evaluationData.assignmentId,
        overallRemarks: document.getElementById('overallRemarksTextarea')?.value || '',
        isDraft: isDraft,
        items: items
    };

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const btn = isDraft ? document.getElementById('btnSaveDraft') : document.getElementById('btnSubmitFinal');
    if (btn) btn.disabled = true;

    try {
        const response = await fetch('/Evaluation/SubmitSelfAssess', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });

        const res = await response.json();
        if (res.success) {
            showToast(res.message, 'success');
            setTimeout(() => {
                window.location.reload();
            }, 1200);
        } else {
            showToast(res.message || 'Lỗi khi gửi đánh giá', 'danger');
            if (btn) btn.disabled = false;
        }
    } catch (err) {
        showToast('Lỗi hệ thống: ' + err.message, 'danger');
        if (btn) btn.disabled = false;
    }
}

function showToast(message, type = 'info') {
    if (window.showEnterpriseToast) {
        window.showEnterpriseToast(message, type);
        return;
    }

    const toastDiv = document.createElement('div');
    toastDiv.className = `alert alert-${type} position-fixed top-0 end-0 m-3 shadow-lg z-3`;
    toastDiv.style.minWidth = '300px';
    toastDiv.innerHTML = `
        <div class="d-flex align-items-center justify-content-between">
            <span>${message}</span>
            <button type="button" class="btn-close ms-2" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
    `;
    document.body.appendChild(toastDiv);
    setTimeout(() => toastDiv.remove(), 4000);
}
