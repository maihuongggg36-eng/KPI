/**
 * KPI & Civil Servant Management System
 * Interactive Client-Side Business Rules Validator
 */

document.addEventListener('DOMContentLoaded', function() {
    initWeightCalculator();
    initSelfAssessCalculator();
    initManagerReviewCalculator();
    initRoleSwitcher();
});

// 1. Cân bằng trọng số KPI (BR-01 & BR-02)
function initWeightCalculator() {
    const weightInputs = document.querySelectorAll('.kpi-weight-input');
    const totalWeightDisplay = document.getElementById('total-weight-display');
    const weightStatusBar = document.getElementById('weight-status-bar');
    const submitBtn = document.getElementById('btn-submit-kpi');

    if (!weightInputs.length || !totalWeightDisplay) return;

    function recalculateTotalWeight() {
        let total = 0;
        let hasInvalidSingle = false;

        weightInputs.forEach(input => {
            const val = parseFloat(input.value) || 0;
            total += val;

            // Kiểm tra BR-02: 5% <= trọng số <= 50%
            if (val > 0 && (val < 5 || val > 50)) {
                hasInvalidSingle = true;
                input.style.borderColor = '#ef4444';
            } else {
                input.style.borderColor = '#e2e8f0';
            }
        });

        total = Math.round(total * 100) / 100;
        totalWeightDisplay.textContent = total + '%';

        if (Math.abs(total - 100) < 0.01) {
            totalWeightDisplay.style.color = '#10b981';
            if (weightStatusBar) {
                weightStatusBar.textContent = '✓ Tổng trọng số đạt chuẩn 100.0%';
                weightStatusBar.className = 'badge badge-success';
            }
            if (submitBtn) submitBtn.disabled = hasInvalidSingle;
        } else {
            totalWeightDisplay.style.color = '#ef4444';
            if (weightStatusBar) {
                weightStatusBar.textContent = `⚠ Tổng hiện tại là ${total}% (cần đúng 100%)`;
                weightStatusBar.className = 'badge badge-danger';
            }
            if (submitBtn) submitBtn.disabled = true;
        }
    }

    weightInputs.forEach(input => {
        input.addEventListener('input', recalculateTotalWeight);
    });

    recalculateTotalWeight();
}

// 2. Tính điểm tự đánh giá theo thời gian thực (BR-09, BR-10, BR-20)
function initSelfAssessCalculator() {
    const rows = document.querySelectorAll('.self-assess-row');
    if (!rows.length) return;

    rows.forEach(row => {
        const actualInput = row.querySelector('.input-actual');
        const targetVal = parseFloat(row.getAttribute('data-target')) || 1;
        const weightVal = parseFloat(row.getAttribute('data-weight')) || 0;
        const maxCap = parseFloat(row.getAttribute('data-maxcap')) || 150;
        const formula = row.getAttribute('data-formula') || 'HigherIsBetter';
        const rateDisplay = row.querySelector('.display-rate');
        const scoreDisplay = row.querySelector('.display-score');
        const evidenceAlert = row.querySelector('.evidence-alert');
        const fileInput = row.querySelector('.input-evidence');

        if (!actualInput) return;

        function updateRowCalculation() {
            const actual = parseFloat(actualInput.value) || 0;
            let rate = 0;

            if (formula === 'LowerIsBetter') {
                rate = actual > 0 ? (targetVal / actual) * 100 : 0;
            } else {
                rate = targetVal > 0 ? (actual / targetVal) * 100 : 0;
            }

            // Giới hạn trần maxCap (BR-10)
            if (rate > maxCap) rate = maxCap;
            rate = Math.round(rate * 10) / 10;

            // Tính điểm = Trọng số * Tỷ lệ / 100
            let score = (rate * weightVal) / 100;
            score = Math.round(score * 100) / 100;

            if (rateDisplay) rateDisplay.textContent = rate + '%';
            if (scoreDisplay) scoreDisplay.textContent = score;

            // Kiểm tra BR-09: Tỷ lệ >= 120% bắt buộc minh chứng
            if (rate >= 120) {
                if (evidenceAlert) evidenceAlert.style.display = 'block';
                if (fileInput) fileInput.setAttribute('required', 'required');
            } else {
                if (evidenceAlert) evidenceAlert.style.display = 'none';
                if (fileInput) fileInput.removeAttribute('required');
            }

            updateGrandTotalSelfScore();
        }

        actualInput.addEventListener('input', updateRowCalculation);
    });

    function updateGrandTotalSelfScore() {
        let grandTotal = 0;
        document.querySelectorAll('.display-score').forEach(el => {
            grandTotal += parseFloat(el.textContent) || 0;
        });
        const grandDisplay = document.getElementById('grand-total-self-score');
        if (grandDisplay) grandDisplay.textContent = Math.round(grandTotal * 100) / 100;
    }
}

// 3. Tính điểm quản lý thẩm định & kiểm tra lệch điểm (BR-05)
function initManagerReviewCalculator() {
    const rows = document.querySelectorAll('.manager-review-row');
    if (!rows.length) return;

    rows.forEach(row => {
        const mgrScoreInput = row.querySelector('.input-mgr-score');
        const selfScore = parseFloat(row.getAttribute('data-self-score')) || 0;
        const weightVal = parseFloat(row.getAttribute('data-weight')) || 100;
        const gapDisplay = row.querySelector('.display-gap');
        const reasonContainer = row.querySelector('.reason-container');
        const reasonInput = row.querySelector('.input-gap-reason');

        if (!mgrScoreInput) return;

        function checkGap() {
            const mgrScore = parseFloat(mgrScoreInput.value) || 0;
            const diff = Math.abs(selfScore - mgrScore);
            const gapPercent = weightVal > 0 ? (diff / weightVal) * 100 : 0;

            if (gapDisplay) {
                gapDisplay.textContent = (mgrScore - selfScore > 0 ? '+' : '') + Math.round((mgrScore - selfScore) * 100) / 100;
            }

            // BR-05: Lệch điểm > 10%
            if (gapPercent > 10) {
                if (reasonContainer) {
                    reasonContainer.style.display = 'block';
                    reasonInput.setAttribute('required', 'required');
                }
            } else {
                if (reasonContainer) {
                    reasonContainer.style.display = 'none';
                    reasonInput.removeAttribute('required');
                }
            }

            updateGrandTotalMgrScore();
        }

        mgrScoreInput.addEventListener('input', checkGap);
    });

    function updateGrandTotalMgrScore() {
        let total = 0;
        document.querySelectorAll('.input-mgr-score').forEach(input => {
            total += parseFloat(input.value) || 0;
        });
        const grandDisplay = document.getElementById('grand-total-mgr-score');
        if (grandDisplay) grandDisplay.textContent = Math.round(total * 100) / 100;
    }
}

// 4. Role Quick Switcher Helper
function initRoleSwitcher() {
    const select = document.getElementById('role-quick-switcher');
    if (!select) return;

    select.addEventListener('change', function() {
        const targetUsername = this.value;
        if (targetUsername) {
            window.location.href = '/login?quick_switch=' + encodeURIComponent(targetUsername);
        }
    });
}
