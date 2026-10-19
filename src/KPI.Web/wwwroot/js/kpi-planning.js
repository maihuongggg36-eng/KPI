/**
 * KPI Planning & Assignment Module — Client-side Business Rules & Wizard Engine
 * Antigravity Enterprise Core (Session 6/12)
 * Enforces BR-01 (Sum = 100%), BR-02 (5% <= Weight <= 50%), BR-03 (Lock on Approve)
 */

// Global State
window.currentStep = 1;
window.assignedKpis = [];
window.weightChartInstance = null;
window.previewChartInstance = null;
window.targetAssignmentId = null;

// ==========================================
// 1. WIZARD STEP NAVIGATION & ANIMATION
// ==========================================
function goToStep(step) {
    if (step === 2 && !validateStep1(false)) return;
    if (step === 3 && (!validateStep1(false) || !validateStep2(false))) return;

    window.currentStep = step;

    // Update Step Indicators
    for (let i = 1; i <= 3; i++) {
        const node = document.getElementById(`step-node-${i}`);
        const content = document.getElementById(`step-content-${i}`);
        if (!node || !content) continue;

        if (i < step) {
            node.className = "wizard-step completed";
        } else if (i === step) {
            node.className = "wizard-step active";
            content.classList.remove("d-none");
        } else {
            node.className = "wizard-step";
            content.classList.add("d-none");
        }
    }

    // Update connector lines
    const line12 = document.getElementById("line-1-2");
    const line23 = document.getElementById("line-2-3");
    if (line12) line12.className = step >= 2 ? "wizard-line active" : "wizard-line";
    if (line23) line23.className = step >= 3 ? "wizard-line active" : "wizard-line";

    if (step === 3) {
        populateReviewStep();
    }
}

function validateStep1AndNext() {
    if (validateStep1(true)) {
        goToStep(2);
    }
}

function validateStep1(showAlert) {
    const empSelect = document.getElementById("select-employee");
    const periodSelect = document.getElementById("select-period");

    if (!empSelect || !empSelect.value) {
        if (showAlert) showToast("Vui lòng chọn nhân viên được giao chỉ tiêu!", "danger");
        return false;
    }
    if (!periodSelect || !periodSelect.value) {
        if (showAlert) showToast("Vui lòng chọn kỳ đánh giá!", "danger");
        return false;
    }
    return true;
}

function validateStep2AndNext() {
    if (validateStep2(true)) {
        goToStep(3);
    }
}

function validateStep2(showAlert) {
    if (window.assignedKpis.length === 0) {
        if (showAlert) showToast("Bản giao phải có ít nhất 01 chỉ tiêu KPI!", "danger");
        return false;
    }

    const total = calculateTotalWeight();

    // BR-01: Tổng trọng số bắt buộc đúng 100.00%
    if (Math.abs(total - 100.00) > 0.01) {
        if (showAlert) showToast(`[BR-01] Tổng trọng số phải bằng chính xác 100.00%. Hiện tại: ${total.toFixed(2)}%!`, "danger");
        return false;
    }

    // BR-02: Mỗi chỉ tiêu 5% -> 50%
    for (let item of window.assignedKpis) {
        if (item.weight < 5 || item.weight > 50) {
            if (showAlert) showToast(`[BR-02] Chỉ tiêu '${item.kpiName}' có trọng số ${item.weight}% không hợp lệ (Phải từ 5% đến 50%)!`, "danger");
            return false;
        }
        if (!item.targetValue || item.targetValue <= 0) {
            if (showAlert) showToast(`Mục tiêu (Target) của chỉ tiêu '${item.kpiName}' phải lớn hơn 0!`, "danger");
            return false;
        }
    }

    return true;
}

// ==========================================
// 2. EMPLOYEE SELECTION & TEMPLATE SUGGEST
// ==========================================
function onEmployeeChanged(empId) {
    const card = document.getElementById("emp-profile-card");
    if (!empId) {
        if (card) card.classList.add("d-none");
        return;
    }

    const select = document.getElementById("select-employee");
    const opt = select.options[select.selectedIndex];

    document.getElementById("emp-name-display").textContent = opt.textContent.split("—")[0].trim();
    document.getElementById("emp-code-display").textContent = opt.getAttribute("data-code") || "CB";
    document.getElementById("emp-pos-display").textContent = opt.getAttribute("data-pos") || "";
    document.getElementById("emp-dept-display").textContent = opt.getAttribute("data-dept") || "";
    document.getElementById("emp-avatar").textContent = opt.textContent.trim().substring(0, 2).toUpperCase();

    if (card) card.classList.remove("d-none");

    // Tự động kiểm tra xem có template mẫu không
    fetch(`/KpiPlanning/GetTemplateSuggestions?employeeId=${empId}`)
        .then(res => res.json())
        .then(res => {
            const btn = document.getElementById("btn-load-template");
            if (res.success && res.data && res.data.items && res.data.items.length > 0) {
                window.suggestedTemplateData = res.data;
                if (btn) {
                    btn.classList.remove("d-none");
                    btn.textContent = `Nạp bộ mẫu gợi ý (${res.data.items.length} chỉ tiêu)`;
                }
            } else {
                window.suggestedTemplateData = null;
                if (btn) btn.classList.add("d-none");
            }
        });
}

function loadSuggestedTemplate() {
    if (!window.suggestedTemplateData || !window.suggestedTemplateData.items) {
        showToast("Không tìm thấy bộ KPI mẫu cho chức danh này.", "warning");
        return;
    }

    window.assignedKpis = window.suggestedTemplateData.items.map(i => ({
        kpiItemId: i.kpiItemId,
        kpiCode: i.kpiCode,
        kpiName: i.kpiName,
        perspective: i.perspective,
        unit: i.unit,
        targetValue: i.targetValue,
        minThreshold: i.minThreshold || 0,
        maxCap: 150,
        weight: i.weight,
        scoringFormula: i.scoringFormula || "HigherIsBetter"
    }));

    renderAssignedKpisList();
    updateWeightMonitoring();
    showToast(`Đã nạp thành công ${window.assignedKpis.length} chỉ tiêu từ bộ mẫu chức danh!`, "success");
    goToStep(2);
}

// ==========================================
// 3. DRAG & DROP ENGINE & KPI LIST MANAGEMENT
// ==========================================
function onDragStart(event) {
    const card = event.currentTarget;
    const data = {
        kpiItemId: parseInt(card.getAttribute("data-id")),
        kpiCode: card.getAttribute("data-code"),
        kpiName: card.getAttribute("data-name"),
        unit: card.getAttribute("data-unit"),
        scoringFormula: card.getAttribute("data-formula") || "HigherIsBetter",
        perspective: card.getAttribute("data-perspective") || "Quy trình nội bộ"
    };
    event.dataTransfer.setData("application/json", JSON.stringify(data));
    card.classList.add("dragging");
}

function onDragOver(event) {
    event.preventDefault();
    const zone = document.getElementById("kpi-drop-zone");
    if (zone) zone.classList.add("drop-active");
}

function onDragLeave(event) {
    const zone = document.getElementById("kpi-drop-zone");
    if (zone) zone.classList.remove("drop-active");
}

function onDrop(event) {
    event.preventDefault();
    const zone = document.getElementById("kpi-drop-zone");
    if (zone) zone.classList.remove("drop-active");

    const raw = event.dataTransfer.getData("application/json");
    if (!raw) return;

    try {
        const item = JSON.parse(raw);
        addKpiItem(item);
    } catch (e) {
        console.error("Drop JSON parse error", e);
    }
}

function addKpiDirectly(kpiId) {
    const card = document.querySelector(`.kpi-draggable-card[data-id="${kpiId}"]`);
    if (!card) return;

    const item = {
        kpiItemId: parseInt(card.getAttribute("data-id")),
        kpiCode: card.getAttribute("data-code"),
        kpiName: card.getAttribute("data-name"),
        unit: card.getAttribute("data-unit"),
        scoringFormula: card.getAttribute("data-formula") || "HigherIsBetter",
        perspective: card.getAttribute("data-perspective") || "Quy trình nội bộ"
    };
    addKpiItem(item);
}

function addKpiItem(item) {
    // Kiểm tra trùng
    if (window.assignedKpis.some(x => x.kpiItemId === item.kpiItemId)) {
        showToast(`Chỉ tiêu '${item.kpiCode}' đã có trong bản giao!`, "warning");
        return;
    }

    // Đề xuất trọng số mặc định (ví dụ 10%)
    const defaultWeight = 10;
    const defaultTarget = 100;

    window.assignedKpis.push({
        kpiItemId: item.kpiItemId,
        kpiCode: item.kpiCode,
        kpiName: item.kpiName,
        perspective: item.perspective,
        unit: item.unit,
        targetValue: defaultTarget,
        minThreshold: 0,
        maxCap: 150,
        weight: defaultWeight,
        scoringFormula: item.scoringFormula
    });

    renderAssignedKpisList();
    updateWeightMonitoring();
    showToast(`Đã thêm chỉ tiêu '${item.kpiCode}' vào bản giao!`, "success");
}

function removeKpiItem(index) {
    window.assignedKpis.splice(index, 1);
    renderAssignedKpisList();
    updateWeightMonitoring();
}

function renderAssignedKpisList() {
    const container = document.getElementById("assigned-items-list");
    const emptyBox = document.getElementById("empty-kpi-placeholder");
    const countBadge = document.getElementById("item-count-badge");

    if (!container) return;

    if (window.assignedKpis.length === 0) {
        container.innerHTML = "";
        if (emptyBox) emptyBox.classList.remove("d-none");
        if (countBadge) countBadge.textContent = "0 chỉ tiêu";
        return;
    }

    if (emptyBox) emptyBox.classList.add("d-none");
    if (countBadge) countBadge.textContent = `${window.assignedKpis.length} chỉ tiêu`;

    let html = "";
    window.assignedKpis.forEach((item, index) => {
        const isWeightInvalid = item.weight < 5 || item.weight > 50;

        html += `
        <div class="card-enterprise p-3 border shadow-xs item-card-row" id="kpi-row-${index}">
            <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2 pb-2 border-bottom">
                <div class="d-flex align-items-center gap-2">
                    <span class="badge bg-dark font-mono">#${index + 1} — ${item.kpiCode}</span>
                    <span class="fw-bold text-dark">${item.kpiName}</span>
                </div>
                <div class="d-flex align-items-center gap-2">
                    <span class="badge bg-secondary-subtle text-secondary small">${item.perspective}</span>
                    <button type="button" class="btn btn-sm btn-link text-danger p-0 ms-2" onclick="removeKpiItem(${index})" title="Xóa chỉ tiêu">
                        <i class="bi bi-trash-fill fs-6"></i>
                    </button>
                </div>
            </div>

            <div class="row g-3 align-items-center">
                <div class="col-md-3">
                    <label class="form-label small text-muted mb-1">Mục tiêu (Target):</label>
                    <div class="input-group input-group-sm">
                        <input type="number" step="any" class="form-control font-mono fw-bold" 
                               value="${item.targetValue}" 
                               onchange="onTargetChanged(${index}, this.value)" />
                        <span class="input-group-text">${item.unit}</span>
                    </div>
                </div>

                <div class="col-md-3">
                    <label class="form-label small text-muted mb-1">
                        Trọng số (%): <span class="badge ${isWeightInvalid ? 'bg-danger' : 'bg-primary-subtle text-primary'} font-mono ms-1" id="badge-weight-${index}">${item.weight}%</span>
                    </label>
                    <div class="input-group input-group-sm">
                        <input type="number" min="5" max="50" step="1" 
                               class="form-control font-mono fw-bold ${isWeightInvalid ? 'border-danger is-invalid' : ''}" 
                               id="input-weight-${index}"
                               value="${item.weight}" 
                               oninput="onWeightInputChanged(${index}, this.value)" />
                        <span class="input-group-text">%</span>
                    </div>
                    <div class="invalid-feedback small ${isWeightInvalid ? 'd-block' : 'd-none'}" id="feedback-weight-${index}">
                        BR-02: Cho phép 5% -> 50%
                    </div>
                </div>

                <div class="col-md-3">
                    <label class="form-label small text-muted mb-1">Công thức tính điểm:</label>
                    <select class="form-select form-select-sm" onchange="onFormulaChanged(${index}, this.value)">
                        <option value="HigherIsBetter" ${item.scoringFormula === 'HigherIsBetter' ? 'selected' : ''}>Càng cao càng tốt</option>
                        <option value="LowerIsBetter" ${item.scoringFormula === 'LowerIsBetter' ? 'selected' : ''}>Càng thấp càng tốt</option>
                        <option value="TargetRange" ${item.scoringFormula === 'TargetRange' ? 'selected' : ''}>Trong khoảng mục tiêu</option>
                    </select>
                </div>

                <div class="col-md-3">
                    <label class="form-label small text-muted mb-1">Trần tối đa (MaxCap):</label>
                    <div class="input-group input-group-sm">
                        <input type="number" step="1" class="form-control font-mono" value="${item.maxCap || 150}" onchange="onMaxCapChanged(${index}, this.value)" />
                        <span class="input-group-text">%</span>
                    </div>
                </div>
            </div>
        </div>
        `;
    });

    container.innerHTML = html;
}

// ==========================================
// 4. REALTIME VALIDATION (BR-01 & BR-02)
// ==========================================
function onWeightInputChanged(index, val) {
    const num = parseFloat(val) || 0;
    window.assignedKpis[index].weight = num;

    const input = document.getElementById(`input-weight-${index}`);
    const badge = document.getElementById(`badge-weight-${index}`);
    const feedback = document.getElementById(`feedback-weight-${index}`);

    // BR-02 Validation
    const isInvalid = num < 5 || num > 50;
    if (input) {
        if (isInvalid) {
            input.classList.add("border-danger", "is-invalid");
        } else {
            input.classList.remove("border-danger", "is-invalid");
        }
    }
    if (badge) {
        badge.textContent = `${num}%`;
        badge.className = `badge ${isInvalid ? 'bg-danger' : 'bg-primary-subtle text-primary'} font-mono ms-1`;
    }
    if (feedback) {
        feedback.className = `invalid-feedback small ${isInvalid ? 'd-block' : 'd-none'}`;
    }

    updateWeightMonitoring();
}

function onTargetChanged(index, val) {
    window.assignedKpis[index].targetValue = parseFloat(val) || 0;
}

function onFormulaChanged(index, val) {
    window.assignedKpis[index].scoringFormula = val;
}

function onMaxCapChanged(index, val) {
    window.assignedKpis[index].maxCap = parseFloat(val) || 150;
}

function calculateTotalWeight() {
    return window.assignedKpis.reduce((acc, cur) => acc + (parseFloat(cur.weight) || 0), 0);
}

function updateWeightMonitoring() {
    const total = calculateTotalWeight();
    const display = document.getElementById("total-weight-display");
    const bar = document.getElementById("weight-progress-bar");
    const alert = document.getElementById("br-01-alert");

    if (display) display.textContent = `${total.toFixed(2)}%`;

    if (bar) {
        const pct = Math.min(100, Math.max(0, total));
        bar.style.width = `${pct}%`;

        bar.classList.remove("bg-danger", "bg-warning", "bg-success");
        if (Math.abs(total - 100.00) < 0.01) {
            bar.classList.add("bg-success");
        } else if (total >= 85 && total < 100) {
            bar.classList.add("bg-warning");
        } else {
            bar.classList.add("bg-danger");
        }
    }

    if (alert) {
        if (Math.abs(total - 100.00) < 0.01) {
            alert.className = "text-success fw-bold";
            alert.innerHTML = '<i class="bi bi-check-circle-fill me-1"></i>Hoàn hảo! Đạt chuẩn 100.00% theo quy tắc BR-01.';
        } else if (total < 100) {
            alert.className = "text-danger fw-semibold";
            alert.innerHTML = `<i class="bi bi-exclamation-triangle-fill me-1"></i>Còn thiếu ${(100 - total).toFixed(2)}% để đạt 100.00%.`;
        } else {
            alert.className = "text-danger fw-semibold";
            alert.innerHTML = `<i class="bi bi-exclamation-octagon-fill me-1"></i>Vượt quá 100.00% (Dư ${(total - 100).toFixed(2)}%). Vui lòng giảm bớt.`;
        }
    }
}

// ==========================================
// 5. REVIEW STEP & APEXCHARTS RENDER
// ==========================================
function populateReviewStep() {
    const empSelect = document.getElementById("select-employee");
    const periodSelect = document.getElementById("select-period");
    const titleInput = document.getElementById("input-assignment-title");

    const empName = empSelect ? empSelect.options[empSelect.selectedIndex].text : "--";
    const periodName = periodSelect ? periodSelect.options[periodSelect.selectedIndex].text : "--";

    document.getElementById("review-emp-name").textContent = empName.split("—")[0].trim();
    document.getElementById("review-emp-pos").textContent = empName.split("—")[1] || "";
    document.getElementById("review-period-name").textContent = periodName;
    document.getElementById("review-assignment-title").textContent = titleInput ? titleInput.value : "";
    document.getElementById("review-total-weight").textContent = `${calculateTotalWeight().toFixed(2)}%`;

    // Render Review Table
    const tbody = document.getElementById("review-table-body");
    if (tbody) {
        let html = "";
        window.assignedKpis.forEach((item, index) => {
            html += `
            <tr>
                <td>${index + 1}</td>
                <td>
                    <div class="fw-bold">${item.kpiName}</div>
                    <span class="badge bg-dark font-mono small">${item.kpiCode}</span>
                </td>
                <td class="font-mono fw-bold">${item.targetValue} ${item.unit}</td>
                <td><span class="badge bg-primary font-mono">${item.weight}%</span></td>
                <td class="small text-muted">${item.scoringFormula}</td>
            </tr>
            `;
        });
        tbody.innerHTML = html;
    }

    // Render Chart
    renderReviewChart();
}

function renderReviewChart() {
    const chartEl = document.getElementById("chart-weight-distribution");
    if (!chartEl || typeof ApexCharts === "undefined") return;

    if (window.weightChartInstance) {
        window.weightChartInstance.destroy();
    }

    const labels = window.assignedKpis.map(x => x.kpiName);
    const series = window.assignedKpis.map(x => x.weight);

    const options = {
        chart: {
            type: "donut",
            height: 280,
            fontFamily: "Inter, sans-serif"
        },
        series: series,
        labels: labels,
        colors: ["#2563eb", "#6366f1", "#10b981", "#f59e0b", "#ec4899", "#8b5cf6", "#06b6d4"],
        legend: {
            position: "bottom",
            fontSize: "12px"
        },
        dataLabels: {
            enabled: true,
            formatter: function (val) {
                return val.toFixed(1) + "%";
            }
        },
        plotOptions: {
            pie: {
                donut: {
                    size: "60%",
                    labels: {
                        show: true,
                        total: {
                            show: true,
                            label: "Tổng Trọng Số",
                            formatter: () => "100%"
                        }
                    }
                }
            }
        }
    };

    window.weightChartInstance = new ApexCharts(chartEl, options);
    window.weightChartInstance.render();
}

// ==========================================
// 6. SUBMISSION & DRAFT ACTIONS
// ==========================================
function buildPayload(isDraft) {
    const empSelect = document.getElementById("select-employee");
    const periodSelect = document.getElementById("select-period");
    const titleInput = document.getElementById("input-assignment-title");

    return {
        id: window.existingAssignmentData ? window.existingAssignmentData.Id : null,
        employeeId: parseInt(empSelect.value),
        periodId: parseInt(periodSelect.value),
        title: titleInput ? titleInput.value.trim() : "",
        isDraft: isDraft,
        items: window.assignedKpis.map(i => ({
            kpiItemId: i.kpiItemId,
            kpiName: i.kpiName,
            unit: i.unit,
            targetValue: i.targetValue,
            minThreshold: i.minThreshold || 0,
            maxCap: i.maxCap || 150,
            weight: i.weight,
            scoringFormula: i.scoringFormula || "HigherIsBetter"
        }))
    };
}

function saveAsDraft() {
    if (!validateStep1(true)) return;
    if (window.assignedKpis.length === 0) {
        showToast("Vui lòng thêm ít nhất 01 chỉ tiêu trước khi lưu nháp!", "warning");
        return;
    }

    const payload = buildPayload(true);

    fetch("/KpiPlanning/SaveDraft", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast(data.message, "success");
            setTimeout(() => { window.location.href = "/KpiPlanning/Approvals"; }, 1000);
        } else {
            showToast(data.message || "Lỗi khi lưu bản nháp", "danger");
        }
    })
    .catch(err => {
        showToast("Lỗi kết nối máy chủ", "danger");
    });
}

function submitKpiAssignment() {
    if (!validateStep1(true) || !validateStep2(true)) return;

    const payload = buildPayload(false);

    fetch("/KpiPlanning/Submit", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast(data.message, "success");
            setTimeout(() => { window.location.href = "/KpiPlanning/Approvals"; }, 1200);
        } else {
            showToast(data.message || "Lỗi gửi bản giao", "danger");
        }
    })
    .catch(err => {
        showToast("Lỗi kết nối máy chủ", "danger");
    });
}

// ==========================================
// 7. ELECTRONIC COMMITMENT (EMPLOYEE)
// ==========================================
function confirmCommitment(assignmentId) {
    if (!confirm("Bạn có chắc chắn muốn ký cam kết điện tử cho bản giao KPI này không?")) return;

    fetch("/KpiPlanning/StaffConfirm", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            assignmentId: assignmentId,
            agree: true,
            electronicSignatureNote: "Cán bộ đã xác thực cam kết mục tiêu công tác trực tuyến"
        })
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast(data.message, "success");
            setTimeout(() => { window.location.reload(); }, 1200);
        } else {
            showToast(data.message || "Lỗi khi ký cam kết", "danger");
        }
    });
}

function openAdjustmentModal() {
    new bootstrap.Modal(document.getElementById("modalAdjustmentRequest")).show();
}

function submitAdjustmentRequest(assignmentId) {
    const reasonInput = document.getElementById("adjustment-reason");
    if (!reasonInput || !reasonInput.value.trim()) {
        showToast("Vui lòng nhập lý do điều chỉnh!", "warning");
        return;
    }

    fetch("/KpiPlanning/StaffRequestAdjustment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            assignmentId: assignmentId,
            agree: false,
            adjustmentReason: reasonInput.value.trim()
        })
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast(data.message, "success");
            setTimeout(() => { window.location.href = "/KpiPlanning/MyKpi"; }, 1200);
        } else {
            showToast(data.message || "Lỗi gửi yêu cầu", "danger");
        }
    });
}

// ==========================================
// 8. DIRECTOR APPROVAL (BR-03 LOCK)
// ==========================================
function quickPreviewAssignment(id) {
    fetch(`/KpiPlanning/GetAssignmentDetails?id=${id}`)
        .then(res => res.json())
        .then(data => {
            document.getElementById("previewEmpName").textContent = data.EmployeeName || "--";
            document.getElementById("previewEmpInfo").textContent = `${data.StaffCode} • ${data.PositionName} [${data.DepartmentName}]`;

            // Table
            const tbody = document.getElementById("previewKpiTableBody");
            let html = "";
            (data.Details || []).forEach(d => {
                html += `
                <tr>
                    <td><strong>${d.KpiName}</strong> <span class="badge bg-dark font-mono small">${d.KpiCode}</span></td>
                    <td class="font-mono">${d.TargetValue} ${d.Unit}</td>
                    <td><span class="badge bg-primary font-mono">${d.Weight}%</span></td>
                </tr>
                `;
            });
            tbody.innerHTML = html;

            // Chart
            const chartEl = document.getElementById("previewWeightChart");
            if (chartEl && typeof ApexCharts !== "undefined") {
                if (window.previewChartInstance) window.previewChartInstance.destroy();
                const options = {
                    chart: { type: "pie", height: 250 },
                    series: (data.Details || []).map(x => x.Weight),
                    labels: (data.Details || []).map(x => x.KpiName),
                    legend: { position: "bottom", fontSize: "11px" }
                };
                window.previewChartInstance = new ApexCharts(chartEl, options);
                window.previewChartInstance.render();
            }

            // Buttons
            const btnArea = document.getElementById("previewActionButtons");
            if (btnArea) {
                if (data.Status !== 4) { // Not Approved_Locked
                    btnArea.innerHTML = `
                        <button type="button" class="btn btn-danger rounded-pill px-3" onclick="openRejectModal(${data.Id}, '${data.EmployeeName}')">Trả lại</button>
                        <button type="button" class="btn btn-success rounded-pill px-4 fw-bold" onclick="openApproveModal(${data.Id}, '${data.EmployeeName}')">Phê duyệt & Khóa (BR-03)</button>
                    `;
                } else {
                    btnArea.innerHTML = `<span class="badge bg-success p-2 fs-6"><i class="bi bi-lock-fill me-1"></i>Đã phê duyệt & Khóa</span>`;
                }
            }

            new bootstrap.Modal(document.getElementById("modalQuickPreview")).show();
        });
}

function openApproveModal(id, name) {
    window.targetAssignmentId = id;
    document.getElementById("approveEmpName").textContent = name;
    new bootstrap.Modal(document.getElementById("modalApproveConfirm")).show();
}

function executeApprove() {
    if (!window.targetAssignmentId) return;
    const comments = document.getElementById("approve-comments")?.value || "";

    fetch("/KpiPlanning/Approve", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            assignmentId: window.targetAssignmentId,
            comments: comments
        })
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast(data.message, "success");
            setTimeout(() => { window.location.reload(); }, 1200);
        } else {
            showToast(data.message || "Lỗi phê duyệt", "danger");
        }
    });
}

function openRejectModal(id, name) {
    window.targetAssignmentId = id;
    document.getElementById("rejectEmpName").textContent = name;
    new bootstrap.Modal(document.getElementById("modalRejectConfirm")).show();
}

function executeReject() {
    if (!window.targetAssignmentId) return;
    const reason = document.getElementById("reject-reason")?.value || "";

    if (!reason.trim()) {
        showToast("Vui lòng nhập lý do trả lại!", "warning");
        return;
    }

    fetch("/KpiPlanning/Reject", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            assignmentId: window.targetAssignmentId,
            reason: reason
        })
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast(data.message, "success");
            setTimeout(() => { window.location.reload(); }, 1200);
        } else {
            showToast(data.message || "Lỗi trả lại", "danger");
        }
    });
}

// ==========================================
// 9. REVISION & EXISTING LOADER
// ==========================================
function loadExistingAssignment(data) {
    if (!data) return;

    const empSelect = document.getElementById("select-employee");
    const periodSelect = document.getElementById("select-period");
    const titleInput = document.getElementById("input-assignment-title");

    if (empSelect) empSelect.value = data.EmployeeId;
    if (periodSelect) periodSelect.value = data.PeriodId;
    if (titleInput) titleInput.value = data.Title || "";

    onEmployeeChanged(data.EmployeeId);

    window.assignedKpis = (data.Details || []).map(d => ({
        kpiItemId: d.KpiItemId,
        kpiCode: d.KpiCode,
        kpiName: d.KpiName,
        perspective: d.Perspective,
        unit: d.Unit,
        targetValue: d.TargetValue,
        minThreshold: d.MinThreshold || 0,
        maxCap: d.MaxCap || 150,
        weight: d.Weight,
        scoringFormula: d.ScoringFormula
    }));

    renderAssignedKpisList();
    updateWeightMonitoring();
}

function openKpiBankDrawer() {
    new bootstrap.Modal(document.getElementById("modalKpiBankDrawer")).show();
}

function filterKpiBankList(query) {
    const q = (query || "").toLowerCase();
    document.querySelectorAll(".kpi-draggable-card").forEach(el => {
        const text = el.textContent.toLowerCase();
        el.style.display = text.includes(q) ? "" : "none";
    });
}

function filterModalKpiList(query) {
    const q = (query || "").toLowerCase();
    document.querySelectorAll(".modal-kpi-row").forEach(el => {
        const text = el.textContent.toLowerCase();
        el.style.display = text.includes(q) ? "" : "none";
    });
}

function showToast(message, type = "info") {
    const toastContainer = document.querySelector(".toast-container") || createToastContainer();
    const id = "toast-" + Date.now();
    const bgClass = type === "success" ? "bg-success" : type === "danger" ? "bg-danger" : type === "warning" ? "bg-warning text-dark" : "bg-primary";
    
    const toastHtml = `
    <div id="${id}" class="toast align-items-center ${bgClass} text-white border-0 shadow-lg" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex">
            <div class="toast-body small fw-semibold">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    </div>`;
    
    toastContainer.insertAdjacentHTML("beforeend", toastHtml);
    const toastEl = document.getElementById(id);
    const bsToast = new bootstrap.Toast(toastEl, { delay: 4000 });
    bsToast.show();
    toastEl.addEventListener("hidden.bs.toast", () => toastEl.remove());
}

function createToastContainer() {
    const div = document.createElement("div");
    div.className = "toast-container position-fixed bottom-0 end-0 p-3";
    div.style.zIndex = "9999";
    document.body.appendChild(div);
    return div;
}
