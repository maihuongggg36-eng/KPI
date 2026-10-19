/**
 * Civil Servant Form 01/02 - Decree 90/2020 & 48/2023
 * Dynamic scoring, Business Rules Engine (BR-06, BR-07, BR-08) & AJAX submit
 */

document.addEventListener('DOMContentLoaded', function () {
    initFormCalculations();
    initSaveAction();
});

function initFormCalculations() {
    const isLeadership = window.formConfig?.isLeadership || false;
    const scoreInputs = document.querySelectorAll('.score-input');
    const chkDisciplinary = document.querySelector('#chkDisciplinaryAction');
    const chkInitiative = document.querySelector('#chkApprovedInitiative');

    // Lắng nghe thay đổi điểm số
    scoreInputs.forEach(input => {
        input.addEventListener('input', calculateTotalAndSuggestRank);
    });

    // Lắng nghe thay đổi điều kiện pháp lý (BR-07, BR-08)
    if (chkDisciplinary) {
        chkDisciplinary.addEventListener('change', function () {
            const br07Alert = document.querySelector('#br07Alert');
            if (this.checked) {
                br07Alert?.classList.remove('d-none');
            } else {
                br07Alert?.classList.add('d-none');
            }
            calculateTotalAndSuggestRank();
        });
    }

    if (chkInitiative) {
        chkInitiative.addEventListener('change', function () {
            const br08Alert = document.querySelector('#br08Alert');
            if (!this.checked) {
                br08Alert?.classList.remove('d-none');
            } else {
                br08Alert?.classList.add('d-none');
            }
            calculateTotalAndSuggestRank();
        });
    }

    // Tính toán ban đầu
    calculateTotalAndSuggestRank();
}

function calculateTotalAndSuggestRank() {
    const isLeadership = window.formConfig?.isLeadership || false;

    const pol = parseFloat(document.querySelector('#inputPoliticalEthics')?.value) || 0;
    const disc = parseFloat(document.querySelector('#inputWorkDiscipline')?.value) || 0;
    const duty = parseFloat(document.querySelector('#inputDutyExecution')?.value) || 0;
    const innov = parseFloat(document.querySelector('#inputInnovation')?.value) || 0;
    const lead = isLeadership ? (parseFloat(document.querySelector('#inputLeadershipCapacity')?.value) || 0) : 0;

    let total = 0;
    if (isLeadership) {
        total = (pol * 0.15) + (disc * 0.15) + (duty * 0.40) + (lead * 0.15) + (innov * 0.15);
    } else {
        total = (pol * 0.15) + (disc * 0.15) + (duty * 0.55) + (innov * 0.15);
    }

    total = Math.round(total * 10) / 10;

    const displayTotal = document.querySelector('#displayTotalScore');
    if (displayTotal) {
        displayTotal.innerText = total.toFixed(1);
    }

    // Thẩm định xếp loại theo Business Rules Engine
    const hasDisciplinary = document.querySelector('#chkDisciplinaryAction')?.checked || false;
    const hasInitiative = document.querySelector('#chkApprovedInitiative')?.checked || false;

    const selFinalized = document.querySelector('#selFinalizedRank');
    const txtProposed = document.querySelector('#txtProposedRank');

    let suggestedRank = "Hoàn thành tốt nhiệm vụ";

    if (hasDisciplinary) {
        // BR-07: Bị kỷ luật -> Ép xếp loại Không hoàn thành nhiệm vụ
        suggestedRank = "Không hoàn thành nhiệm vụ";
        if (selFinalized) {
            selFinalized.value = "Không hoàn thành nhiệm vụ";
        }
    } else if (total < 50) {
        suggestedRank = "Không hoàn thành nhiệm vụ";
    } else if (total < 70) {
        suggestedRank = "Hoàn thành nhiệm vụ";
    } else if (total < 90) {
        suggestedRank = "Hoàn thành tốt nhiệm vụ";
    } else {
        // Điểm >= 90
        if (!hasInitiative) {
            // BR-08: Thiếu sáng kiến -> Hạ về Hoàn thành tốt nhiệm vụ
            suggestedRank = "Hoàn thành tốt nhiệm vụ";
        } else {
            suggestedRank = "Hoàn thành xuất sắc nhiệm vụ";
        }
    }

    if (txtProposed) {
        txtProposed.value = suggestedRank;
    }
}

function initSaveAction() {
    const btnSave = document.querySelector('#btnSaveClassification');
    if (!btnSave) return;

    btnSave.addEventListener('click', async function () {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const config = window.formConfig;

        if (!config || (!config.evaluationId && !config.employeeId)) {
            alert('Lỗi: Không tìm thấy mã hồ sơ đánh giá hợp lệ.');
            return;
        }

        const payload = {
            employeeId: config.employeeId,
            evaluationId: config.evaluationId,
            politicalEthicsScore: parseInt(document.querySelector('#inputPoliticalEthics')?.value) || 0,
            workDisciplineScore: parseInt(document.querySelector('#inputWorkDiscipline')?.value) || 0,
            dutyExecutionScore: parseInt(document.querySelector('#inputDutyExecution')?.value) || 0,
            leadershipCapacityScore: parseInt(document.querySelector('#inputLeadershipCapacity')?.value) || 0,
            innovationScore: parseInt(document.querySelector('#inputInnovation')?.value) || 0,
            hasDisciplinaryAction: document.querySelector('#chkDisciplinaryAction')?.checked || false,
            hasApprovedInitiative: document.querySelector('#chkApprovedInitiative')?.checked || false,
            collectiveComments: document.querySelector('#txtCollectiveComments')?.value || '',
            partyOpinion: document.querySelector('#txtPartyOpinion')?.value || '',
            proposedRank: document.querySelector('#txtProposedRank')?.value || '',
            finalizedRank: document.querySelector('#selFinalizedRank')?.value || ''
        };

        btnSave.disabled = true;
        btnSave.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang lưu...';

        try {
            const response = await fetch('/CivilServant/SaveForm01_02', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token,
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            });

            const data = await response.json();

            if (data.success) {
                alert(data.message || 'Lưu biểu mẫu thành công!');
                window.location.href = `/CivilServant/Index?periodId=${config.periodId}`;
            } else {
                let alertMsg = data.message || 'Đã xảy ra lỗi khi lưu kết quả.';
                if (data.code) {
                    alertMsg = `[CẢNH BÁO ${data.code}]:\n${alertMsg}`;
                }
                alert(alertMsg);
            }
        } catch (err) {
            console.error('Save Form 01/02 error:', err);
            alert('Lỗi kết nối máy chủ: ' + err.message);
        } finally {
            btnSave.disabled = false;
            btnSave.innerHTML = '<i class="bi bi-check2-circle me-1"></i>Lưu Đánh Giá & Phê Duyệt';
        }
    });
}
