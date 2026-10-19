/**
 * Hệ thống Quản lý KPI & Đánh giá Cán bộ Công chức
 * dashboard.js - Script Quản trị Biểu đồ ApexCharts & Điều khiển Bàn làm việc Đa Vai trò
 */

(function () {
    'use strict';

    // Biến trạng thái toàn cục
    let activeRole = 'Director';
    let autoRefreshEnabled = true;
    let countdownSeconds = 60;
    let countdownTimerInterval = null;
    const charts = {};

    // Helper kiểm tra Theme
    function isDark() {
        return document.documentElement.getAttribute('data-theme') === 'dark';
    }

    function getChartTheme() {
        return {
            mode: isDark() ? 'dark' : 'light',
            palette: 'palette1'
        };
    }

    function getGridBorderColor() {
        return isDark() ? 'rgba(255, 255, 255, 0.08)' : 'rgba(0, 0, 0, 0.06)';
    }

    function getTextColor() {
        return isDark() ? '#94a3b8' : '#64748b';
    }

    // =========================================================================
    // 1. KHỞI TẠO CÁC BIỂU ĐỒ CHO TỪNG PHÂN HỆ VAI TRÒ
    // =========================================================================

    // 1.1 Biểu đồ Giám đốc (Director)
    function initDirectorCharts() {
        // A. Radar Chart: So sánh hiệu suất 5 phòng ban
        const radarEl = document.querySelector("#directorRadarChart");
        if (radarEl && !charts.directorRadar) {
            const options = {
                chart: {
                    type: 'radar',
                    height: 360,
                    toolbar: { show: true, tools: { download: true } },
                    theme: getChartTheme()
                },
                series: [
                    { name: 'Phòng Quản lý Đào tạo', data: [92, 88, 95, 82, 91] },
                    { name: 'Phòng Tổ chức Cán bộ', data: [89, 94, 98, 76, 88] },
                    { name: 'Phòng Kế hoạch - Tài chính', data: [95, 90, 96, 72, 85] },
                    { name: 'Khoa Quản trị và HCC', data: [86, 96, 92, 94, 95] },
                    { name: 'Ban Giám đốc', data: [98, 95, 99, 90, 97] }
                ],
                labels: ['Tiến độ đào tạo', 'Chất lượng chuyên môn', 'Kỷ luật công vụ', 'Đổi mới sáng tạo', 'Hài lòng người học'],
                colors: ['#2563eb', '#6366f1', '#10b981', '#f59e0b', '#ec4899'],
                stroke: { width: 2 },
                fill: { opacity: 0.2 },
                markers: { size: 4 },
                legend: { position: 'bottom', labels: { colors: getTextColor() } },
                yaxis: { max: 100, min: 60, labels: { style: { colors: getTextColor() } } }
            };
            charts.directorRadar = new ApexCharts(radarEl, options);
            charts.directorRadar.render();
        }

        // B. Donut Chart: Phân bổ xếp loại
        const donutEl = document.querySelector("#directorDonutChart");
        if (donutEl && !charts.directorDonut) {
            const options = {
                chart: {
                    type: 'donut',
                    height: 310,
                    toolbar: { show: true },
                    theme: getChartTheme()
                },
                series: [1, 4, 1, 0],
                labels: ['Xuất sắc (≤20%)', 'Hoàn thành tốt', 'Hoàn thành', 'Không hoàn thành'],
                colors: ['#10b981', '#2563eb', '#f59e0b', '#ef4444'],
                legend: { position: 'bottom', labels: { colors: getTextColor() } },
                dataLabels: {
                    enabled: true,
                    formatter: function (val, opts) {
                        return opts.w.config.series[opts.seriesIndex] + ' CB';
                    }
                },
                plotOptions: {
                    pie: {
                        donut: {
                            size: '68%',
                            labels: {
                                show: true,
                                total: {
                                    show: true,
                                    label: 'Tổng Cán bộ',
                                    formatter: () => '6 Cán bộ',
                                    color: getTextColor()
                                }
                            }
                        }
                    }
                }
            };
            charts.directorDonut = new ApexCharts(donutEl, options);
            charts.directorDonut.render();
        }

        // C. Line Chart: Xu hướng điểm 12 tháng
        const lineEl = document.querySelector("#directorLineChart");
        if (lineEl && !charts.directorLine) {
            const options = {
                chart: {
                    type: 'line',
                    height: 320,
                    toolbar: { show: true },
                    animations: { enabled: true, easing: 'easeinout', speed: 800 }
                },
                series: [
                    { name: 'Điểm thực tế đạt được', data: [82.5, 83.8, 85.0, 86.2, 86.0, 88.4, 89.2, 90.0, 89.5, 91.2, 92.5, 94.0] },
                    { name: 'Chỉ tiêu mục tiêu đề ra', data: [80.0, 80.0, 80.0, 85.0, 85.0, 85.0, 85.0, 85.0, 85.0, 90.0, 90.0, 90.0] }
                ],
                xaxis: {
                    categories: ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10', 'T11', 'T12'],
                    labels: { style: { colors: getTextColor() } }
                },
                yaxis: { min: 70, max: 100, labels: { style: { colors: getTextColor() } } },
                colors: ['#2563eb', '#10b981'],
                stroke: { curve: 'smooth', width: [3, 2], dashArray: [0, 5] },
                markers: { size: 4 },
                legend: { position: 'top', labels: { colors: getTextColor() } },
                grid: { borderColor: getGridBorderColor() }
            };
            charts.directorLine = new ApexCharts(lineEl, options);
            charts.directorLine.render();
        }

        // D. Bell Curve (Gauss): Thực tế vs Chuẩn
        const gaussEl = document.querySelector("#directorGaussChart");
        if (gaussEl && !charts.directorGauss) {
            const options = {
                chart: {
                    type: 'bar',
                    height: 320,
                    toolbar: { show: true }
                },
                series: [
                    { name: 'Tỷ lệ Chuẩn Gauss (%)', data: [15.0, 70.0, 10.0, 5.0] },
                    { name: 'Tỷ lệ Thực tế Học viện (%)', data: [16.7, 66.6, 16.7, 0.0] }
                ],
                xaxis: {
                    categories: ['Xuất sắc (≤20%)', 'Tốt', 'Hoàn thành', 'KHTNV'],
                    labels: { style: { colors: getTextColor() } }
                },
                yaxis: { max: 100, labels: { formatter: (val) => val + '%', style: { colors: getTextColor() } } },
                colors: ['#94a3b8', '#6366f1'],
                plotOptions: {
                    bar: { horizontal: false, columnWidth: '55%', borderRadius: 6 }
                },
                dataLabels: { enabled: false },
                legend: { position: 'top', labels: { colors: getTextColor() } },
                grid: { borderColor: getGridBorderColor() }
            };
            charts.directorGauss = new ApexCharts(gaussEl, options);
            charts.directorGauss.render();
        }
    }

    // 1.2 Biểu đồ Trưởng phòng (Manager)
    function initManagerCharts() {
        // A. Bar Chart: So sánh điểm
        const barEl = document.querySelector("#managerBarChart");
        if (barEl && !charts.managerBar) {
            const options = {
                chart: {
                    type: 'bar',
                    height: 330,
                    toolbar: { show: true }
                },
                series: [
                    { name: 'Điểm Tự chấm', data: [93.5, 91.0, 86.0] },
                    { name: 'Điểm Trưởng phòng Thẩm định', data: [94.0, 90.5, 87.0] }
                ],
                xaxis: {
                    categories: ['Lê Trưởng Phòng', 'Nguyễn Chuyên Viên', 'Trần Giảng Viên'],
                    labels: { style: { colors: getTextColor() } }
                },
                yaxis: { min: 60, max: 100, labels: { style: { colors: getTextColor() } } },
                colors: ['#3b82f6', '#10b981'],
                plotOptions: { bar: { columnWidth: '45%', borderRadius: 6 } },
                dataLabels: { enabled: true, formatter: (val) => val.toFixed(1) },
                legend: { position: 'top', labels: { colors: getTextColor() } },
                grid: { borderColor: getGridBorderColor() }
            };
            charts.managerBar = new ApexCharts(barEl, options);
            charts.managerBar.render();
        }

        // B. Calendar Heatmap
        const heatEl = document.querySelector("#managerHeatmapChart");
        if (heatEl && !charts.managerHeatmap) {
            // Dữ liệu mẫu 4 tuần
            const options = {
                chart: {
                    type: 'heatmap',
                    height: 220,
                    toolbar: { show: false }
                },
                series: [
                    { name: 'Thứ 2', data: [{ x: 'Tuần 1', y: 3 }, { x: 'Tuần 2', y: 4 }, { x: 'Tuần 3', y: 2 }, { x: 'Tuần 4', y: 3 }] },
                    { name: 'Thứ 3', data: [{ x: 'Tuần 1', y: 2 }, { x: 'Tuần 2', y: 3 }, { x: 'Tuần 3', y: 3 }, { x: 'Tuần 4', y: 4 }] },
                    { name: 'Thứ 4', data: [{ x: 'Tuần 1', y: 4 }, { x: 'Tuần 2', y: 2 }, { x: 'Tuần 3', y: 4 }, { x: 'Tuần 4', y: 2 }] },
                    { name: 'Thứ 5', data: [{ x: 'Tuần 1', y: 3 }, { x: 'Tuần 2', y: 3 }, { x: 'Tuần 3', y: 2 }, { x: 'Tuần 4', y: 3 }] },
                    { name: 'Thứ 6', data: [{ x: 'Tuần 1', y: 5 }, { x: 'Tuần 2', y: 4 }, { x: 'Tuần 3', y: 3 }, { x: 'Tuần 4', y: 4 }] }
                ],
                colors: ["#2563eb"],
                dataLabels: { enabled: false },
                xaxis: { labels: { style: { colors: getTextColor() } } },
                yaxis: { labels: { style: { colors: getTextColor() } } },
                grid: { borderColor: getGridBorderColor() }
            };
            charts.managerHeatmap = new ApexCharts(heatEl, options);
            charts.managerHeatmap.render();
        }
    }

    // 1.3 Biểu đồ Chuyên viên (Employee)
    function initEmployeeCharts() {
        const radEl = document.querySelector("#employeeRadialChart");
        if (radEl && !charts.employeeRadial) {
            const options = {
                chart: {
                    type: 'radialBar',
                    height: 200,
                    sparkline: { enabled: true }
                },
                series: [92.5],
                colors: ['#2563eb'],
                plotOptions: {
                    radialBar: {
                        startAngle: -135,
                        endAngle: 135,
                        track: {
                            background: isDark() ? 'rgba(255,255,255,0.08)' : '#e2e8f0',
                            strokeWidth: '97%'
                        },
                        dataLabels: {
                            name: { show: false },
                            value: {
                                offsetY: 8,
                                fontSize: '24px',
                                fontWeight: 700,
                                color: getTextColor(),
                                formatter: (val) => val + '%'
                            }
                        }
                    }
                },
                fill: {
                    type: 'gradient',
                    gradient: {
                        shade: 'dark',
                        type: 'horizontal',
                        gradientToColors: ['#6366f1'],
                        stops: [0, 100]
                    }
                },
                stroke: { lineCap: 'round' }
            };
            charts.employeeRadial = new ApexCharts(radEl, options);
            charts.employeeRadial.render();
        }
    }

    // 1.4 Biểu đồ Kế toán (Accountant)
    function initAccountantCharts() {
        const salEl = document.querySelector("#accountantSalaryChart");
        if (salEl && !charts.accountantSalary) {
            const options = {
                chart: {
                    type: 'bar',
                    height: 340,
                    stacked: true,
                    toolbar: { show: true }
                },
                series: [
                    { name: 'P1: Lương ngạch bậc (Cố định)', data: [28, 22, 18, 16, 20] },
                    { name: 'P2: Phụ cấp trách nhiệm công vụ', data: [6, 3.5, 3, 2.8, 3.2] },
                    { name: 'P3: Lương hiệu quả KPI', data: [18.5, 16.2, 12.8, 11.5, 14.5] }
                ],
                xaxis: {
                    categories: ['Ban Giám đốc', 'Phòng Đào tạo', 'Phòng TCCB', 'Phòng KHTC', 'Khoa QHCC'],
                    labels: { style: { colors: getTextColor() } }
                },
                yaxis: {
                    labels: {
                        formatter: (val) => val + ' tr.đ',
                        style: { colors: getTextColor() }
                    }
                },
                colors: ['#2563eb', '#f59e0b', '#10b981'],
                plotOptions: { bar: { columnWidth: '45%', borderRadius: 4 } },
                legend: { position: 'top', labels: { colors: getTextColor() } },
                grid: { borderColor: getGridBorderColor() },
                dataLabels: { enabled: false }
            };
            charts.accountantSalary = new ApexCharts(salEl, options);
            charts.accountantSalary.render();
        }
    }

    // 1.5 Biểu đồ HR Admin
    function initHrCharts() {
        // A. Gauge Meter: Trần 20% NĐ 90
        const gaugeEl = document.querySelector("#hrGaugeChart");
        if (gaugeEl && !charts.hrGauge) {
            const options = {
                chart: {
                    type: 'radialBar',
                    height: 250,
                    offsetY: -10
                },
                series: [16.7],
                plotOptions: {
                    radialBar: {
                        startAngle: -135,
                        endAngle: 135,
                        track: {
                            background: isDark() ? 'rgba(255,255,255,0.08)' : '#e2e8f0',
                            strokeWidth: '95%'
                        },
                        dataLabels: {
                            name: {
                                show: true,
                                label: 'Tỷ lệ Xuất sắc',
                                color: getTextColor(),
                                fontSize: '13px',
                                offsetY: -10
                            },
                            value: {
                                fontSize: '28px',
                                fontWeight: 700,
                                color: '#10b981',
                                offsetY: 8,
                                formatter: (val) => val + '%'
                            }
                        }
                    }
                },
                fill: { colors: ['#10b981'] },
                stroke: { lineCap: 'round' }
            };
            charts.hrGauge = new ApexCharts(gaugeEl, options);
            charts.hrGauge.render();
        }

        // B. Phân bổ theo phòng ban
        const deptEl = document.querySelector("#hrDeptChart");
        if (deptEl && !charts.hrDept) {
            const options = {
                chart: { type: 'donut', height: 280, theme: getChartTheme() },
                series: [2, 2, 1, 1, 1],
                labels: ['Ban Giám đốc', 'Phòng Đào tạo', 'Phòng TCCB', 'Phòng KHTC', 'Khoa QHCC'],
                colors: ['#2563eb', '#6366f1', '#10b981', '#f59e0b', '#06b6d4'],
                legend: { position: 'bottom', labels: { colors: getTextColor() } },
                plotOptions: { pie: { donut: { size: '65%' } } }
            };
            charts.hrDept = new ApexCharts(deptEl, options);
            charts.hrDept.render();
        }

        // C. Phân bổ theo ngạch bậc
        const rankEl = document.querySelector("#hrRankChart");
        if (rankEl && !charts.hrRank) {
            const options = {
                chart: { type: 'donut', height: 280, theme: getChartTheme() },
                series: [1, 2, 2, 1],
                labels: ['Chuyên viên cao cấp (A3)', 'Chuyên viên chính (A2)', 'Chuyên viên (A1)', 'Hợp đồng lao động'],
                colors: ['#8b5cf6', '#3b82f6', '#10b981', '#64748b'],
                legend: { position: 'bottom', labels: { colors: getTextColor() } },
                plotOptions: { pie: { donut: { size: '65%' } } }
            };
            charts.hrRank = new ApexCharts(rankEl, options);
            charts.hrRank.render();
        }
    }

    // =========================================================================
    // 2. ĐIỀU KHIỂN CHUYỂN ĐỔI TAB VAI TRÒ (ROLE SWITCHER)
    // =========================================================================
    window.switchDashboardRole = function (role) {
        activeRole = role;

        // Cập nhật Buttons
        document.querySelectorAll('.role-tab-btn').forEach(btn => {
            if (btn.getAttribute('data-role') === role) {
                btn.classList.remove('btn-light', 'text-secondary');
                btn.classList.add('btn-primary', 'active');
            } else {
                btn.classList.remove('btn-primary', 'active');
                btn.classList.add('btn-light', 'text-secondary');
            }
        });

        // Cập nhật Panels
        document.querySelectorAll('.dashboard-panel').forEach(panel => {
            panel.classList.add('d-none');
        });
        const targetPanel = document.getElementById(`panel-${role}`);
        if (targetPanel) {
            targetPanel.classList.remove('d-none');
        }

        // Cập nhật URL không reload
        window.history.replaceState(null, '', `/Dashboard?role=${role}`);

        // Kích hoạt nạp biểu đồ của Tab tương ứng
        setTimeout(() => {
            if (role === 'Director') initDirectorCharts();
            else if (role === 'Manager') initManagerCharts();
            else if (role === 'Employee') initEmployeeCharts();
            else if (role === 'Accountant') initAccountantCharts();
            else if (role === 'Hr') initHrCharts();

            window.dispatchEvent(new Event('resize'));
        }, 50);
    };

    // =========================================================================
    // 3. AUTO-REFRESH & DATA FETCHING QUA AJAX
    // =========================================================================
    window.toggleAutoRefresh = function (chk) {
        autoRefreshEnabled = chk.checked;
        if (autoRefreshEnabled) {
            countdownSeconds = 60;
            updateCountdownDisplay();
        }
    };

    function updateCountdownDisplay() {
        const timerEl = document.getElementById('countdownTimer');
        if (timerEl) {
            timerEl.textContent = countdownSeconds;
        }
    }

    function startAutoRefreshTimer() {
        if (countdownTimerInterval) clearInterval(countdownTimerInterval);
        countdownTimerInterval = setInterval(() => {
            if (!autoRefreshEnabled) return;
            countdownSeconds--;
            if (countdownSeconds <= 0) {
                countdownSeconds = 60;
                refreshCurrentDashboard();
            }
            updateCountdownDisplay();
        }, 1000);
    }

    window.refreshCurrentDashboard = function () {
        const btn = document.getElementById('manualRefreshBtn');
        if (btn) btn.querySelector('i')?.classList.add('spin-animation');

        const endpoint = `/Dashboard/Get${activeRole}Data`;
        fetch(endpoint)
            .then(res => res.json())
            .then(res => {
                if (res.success && res.data) {
                    // Cập nhật lại biểu đồ nếu có instance
                    updateChartsWithData(activeRole, res.data);
                    if (window.toastManager) {
                        window.toastManager.show({
                            title: 'Cập nhật Dữ liệu',
                            message: `Đã làm mới số liệu Bàn làm việc ${activeRole} lúc ${res.timestamp}!`,
                            variant: 'info'
                        });
                    }
                }
            })
            .catch(err => console.warn('Lỗi auto-refresh:', err))
            .finally(() => {
                if (btn) btn.querySelector('i')?.classList.remove('spin-animation');
            });
    };

    function updateChartsWithData(role, data) {
        if (role === 'Director' && charts.directorRadar && data.departmentRadar) {
            charts.directorRadar.updateSeries(data.departmentRadar.series);
        } else if (role === 'Manager' && charts.managerBar && data.scoreComparisons) {
            charts.managerBar.updateSeries([
                { name: 'Điểm Tự chấm', data: data.scoreComparisons.selfScores },
                { name: 'Điểm Trưởng phòng Thẩm định', data: data.scoreComparisons.managerScores }
            ]);
        } else if (role === 'Employee' && charts.employeeRadial && data.overallCompletionRate) {
            charts.employeeRadial.updateSeries([data.overallCompletionRate]);
        } else if (role === 'Accountant' && charts.accountantSalary && data.departmentSalaries) {
            charts.accountantSalary.updateSeries([
                { name: 'P1: Lương ngạch bậc (Cố định)', data: data.departmentSalaries.p1BaseSalaries.map(v => v / 1000000) },
                { name: 'P2: Phụ cấp trách nhiệm công vụ', data: data.departmentSalaries.p2Allowances.map(v => v / 1000000) },
                { name: 'P3: Lương hiệu quả KPI', data: data.departmentSalaries.p3KpiBonuses.map(v => v / 1000000) }
            ]);
        } else if (role === 'Hr' && charts.hrGauge && data.excellentRate) {
            charts.hrGauge.updateSeries([data.excellentRate]);
        }
    }

    // =========================================================================
    // 4. THAO TÁC NHANH (QUICK ACTIONS)
    // =========================================================================
    let currentModalEvalId = null;

    window.quickApprove = function (evalId, employeeName) {
        currentModalEvalId = evalId;
        document.getElementById('modalEvalId').value = evalId;
        document.getElementById('modalEmployeeName').textContent = employeeName;
        document.getElementById('modalComment').value = 'Đồng ý với kết luận tự chấm và thẩm định của đơn vị.';

        const modalEl = document.getElementById('quickActionModal');
        if (modalEl) {
            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        }
    };

    window.submitQuickApprove = function () {
        const evalId = document.getElementById('modalEvalId').value;
        const comment = document.getElementById('modalComment').value;

        fetch('/Dashboard/QuickApprove', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ evaluationId: parseInt(evalId), comment: comment })
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                // Đóng modal
                const modalEl = document.getElementById('quickActionModal');
                const modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) modal.hide();

                // Cập nhật dòng bảng
                const row = document.getElementById(`approval-row-${evalId}`);
                if (row) {
                    row.classList.add('bg-success-subtle');
                    setTimeout(() => {
                        row.remove();
                    }, 500);
                }

                if (window.toastManager) {
                    window.toastManager.show({
                        title: 'Phê duyệt Thành công',
                        message: res.message,
                        variant: 'success'
                    });
                }
            } else {
                alert(res.message);
            }
        });
    };

    window.quickReview = function (evalId, employeeName) {
        if (window.toastManager) {
            window.toastManager.show({
                title: 'Thẩm định Điểm',
                message: `Đang mở biểu mẫu thẩm định cho đồng chí: ${employeeName}...`,
                variant: 'info'
            });
        }
    };

    window.quickDisburse = function (voucherId, employeeName, amount) {
        if (!confirm(`Xác nhận đã phát tiền mặt số tiền ${amount} tại quỹ cho cán bộ ${employeeName}?`)) {
            return;
        }

        fetch('/Dashboard/QuickDisburse', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ voucherId: voucherId })
        })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                const statusBadge = document.getElementById(`voucher-status-${voucherId}`);
                if (statusBadge) {
                    statusBadge.className = 'badge bg-success-subtle text-success';
                    statusBadge.textContent = 'Đã chi tiền mặt';
                }
                const btn = document.getElementById(`voucher-btn-${voucherId}`);
                if (btn) {
                    btn.disabled = true;
                    btn.className = 'btn btn-sm btn-outline-secondary rounded-pill px-3';
                    btn.innerHTML = '<i class="bi bi-check-all me-1"></i> Đã hoàn tất';
                }

                if (window.toastManager) {
                    window.toastManager.show({
                        title: 'Đã Phát Tiền Mặt',
                        message: res.message,
                        variant: 'success'
                    });
                }
            } else {
                alert(res.message);
            }
        });
    };

    window.exportDirectorReport = function () {
        if (window.toastManager) {
            window.toastManager.show({
                title: 'Xuất Báo Cáo',
                message: 'Đang kết xuất Báo cáo Đánh giá Tổng thể Toàn Học viện định dạng PDF...',
                variant: 'primary'
            });
        }
    };

    window.exportCashReceiptList = function () {
        window.print();
    };

    // =========================================================================
    // 5. THEME CHANGE OBSERVER CHO APEXCHARTS
    // =========================================================================
    const observer = new MutationObserver(() => {
        const theme = getChartTheme();
        const txtColor = getTextColor();
        const gridColor = getGridBorderColor();

        // Cập nhật lại theme cho tất cả charts
        Object.values(charts).forEach(chart => {
            if (chart && typeof chart.updateOptions === 'function') {
                chart.updateOptions({
                    theme: theme,
                    grid: { borderColor: gridColor }
                });
            }
        });
    });
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

    // =========================================================================
    // 6. KHỞI CHẠY KHI DOM SẴN SÀNG
    // =========================================================================
    document.addEventListener('DOMContentLoaded', () => {
        // Lấy role từ URL nếu có
        const urlParams = new URLSearchParams(window.location.search);
        const roleParam = urlParams.get('role');
        if (roleParam) {
            activeRole = roleParam;
        }

        // Khởi tạo các biểu đồ của tab mặc định
        setTimeout(() => {
            if (activeRole === 'Director') initDirectorCharts();
            else if (activeRole === 'Manager') initManagerCharts();
            else if (activeRole === 'Employee') initEmployeeCharts();
            else if (activeRole === 'Accountant') initAccountantCharts();
            else if (activeRole === 'Hr') initHrCharts();
        }, 100);

        // Bắt đầu timer 60s
        startAutoRefreshTimer();
    });

})();
