/**
 * Phân Hệ Báo Cáo & Phân Tích Dữ Liệu KPI (FR-REP-01 đến FR-REP-05)
 * Hỗ trợ ApexCharts tương tác, Smart Presets, Drill-down, và Scheduled Reports
 */

document.addEventListener('DOMContentLoaded', function () {
    const data = window.reportData || {};
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // ==========================================================
    // 1. TAB & FILTER CONTROLS
    // ==========================================================
    window.setTab = function (tabName) {
        const input = document.getElementById('activeTabInput');
        if (input) input.value = tabName;

        // Cập nhật URL không reload
        const url = new URL(window.location);
        url.searchParams.set('tab', tabName);
        window.history.replaceState({}, '', url);

        // Kích hoạt tab bootstrap
        const triggerEl = document.querySelector(`#reportTabs button[data-bs-target="#tab${capitalize(tabName)}"]`);
        if (triggerEl && typeof bootstrap !== 'undefined') {
            const tab = new bootstrap.Tab(triggerEl);
            tab.show();
        }

        // Trigger resize cho ApexCharts
        setTimeout(() => window.dispatchEvent(new Event('resize')), 150);
    };

    window.applyPreset = function (presetName) {
        const presetInput = document.getElementById('activePresetInput');
        if (presetInput) presetInput.value = presetName;
        document.getElementById('frmReportFilter')?.submit();
    };

    window.changeSort = function (sortBy) {
        const form = document.getElementById('frmReportFilter');
        let sortInput = form.querySelector('input[name="SortBy"]');
        if (!sortInput) {
            sortInput = document.createElement('input');
            sortInput.type = 'hidden';
            sortInput.name = 'SortBy';
            form.appendChild(sortInput);
        }
        sortInput.value = sortBy;
        form.submit();
    };

    // ==========================================================
    // 2. SHUFFLE ANIMATION CHO LEADERBOARD
    // ==========================================================
    const btnShuffle = document.getElementById('btnShuffleAnimation');
    if (btnShuffle) {
        btnShuffle.addEventListener('click', function () {
            const rows = document.querySelectorAll('.leaderboard-row');
            rows.forEach((row, index) => {
                row.style.transition = 'all 0.5s ease-in-out';
                row.style.transform = `translateY(${index % 2 === 0 ? '-10px' : '10px'}) scale(0.98)`;
                row.style.opacity = '0.7';
            });

            setTimeout(() => {
                rows.forEach(row => {
                    row.style.transform = 'translateY(0) scale(1)';
                    row.style.opacity = '1';
                });
            }, 500);
        });
    }

    // ==========================================================
    // 3. DRILL-DOWN MODAL THEO BĂNG ĐIỂM
    // ==========================================================
    window.openDrillDown = async function (bandCode, bandName) {
        const modalEl = document.getElementById('modalDrillDown');
        const modal = modalEl && typeof bootstrap !== 'undefined' ? new bootstrap.Modal(modalEl) : null;
        const titleEl = document.getElementById('drillDownTitle');
        const bodyEl = document.getElementById('drillDownBody');

        if (titleEl) titleEl.innerHTML = `<i class="bi bi-people-fill me-2"></i>Danh Sách Cán Bộ: ${bandName}`;
        if (bodyEl) bodyEl.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted"><span class="spinner-border spinner-border-sm me-2"></span>Đang tải dữ liệu...</td></tr>';

        if (modal) modal.show();

        try {
            const res = await fetch(`/Report/DrillDownBand?periodId=${data.periodId}&bandCode=${bandCode}`);
            const result = await res.json();

            if (result.success && result.data && result.data.length > 0) {
                let html = '';
                result.data.forEach((item, idx) => {
                    html += `
                        <tr>
                            <td class="font-mono text-muted text-center">${idx + 1}</td>
                            <td class="font-mono fw-semibold">${item.staffCode}</td>
                            <td class="fw-bold text-dark">${item.fullName}</td>
                            <td>${item.departmentName}</td>
                            <td class="text-end font-mono fw-bold text-primary">${item.finalScore.toFixed(1)}</td>
                            <td><span class="badge bg-light text-dark border">${item.classification}</span></td>
                        </tr>
                    `;
                });
                bodyEl.innerHTML = html;
            } else {
                bodyEl.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">Không có cán bộ nào trong băng điểm này.</td></tr>';
            }
        } catch (err) {
            bodyEl.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-danger">Lỗi tải dữ liệu: ${err.message}</td></tr>`;
        }
    };

    // ==========================================================
    // 4. LƯU BỘ LỌC YÊU THÍCH & ĐẶT LỊCH GỬI EMAIL
    // ==========================================================
    const btnSaveFilter = document.getElementById('btnSaveCurrentFilter');
    if (btnSaveFilter) {
        btnSaveFilter.addEventListener('click', async function () {
            const name = prompt('Nhập tên gợi nhớ cho bộ lọc này (Ví dụ: "Ưu tiên khối Đào tạo"):');
            if (!name) return;

            const filterObj = {
                id: 'filter_' + Date.now(),
                name: name,
                filterJson: JSON.stringify({ periodId: data.periodId })
            };

            try {
                const res = await fetch('/Report/SaveFilter', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'X-CSRF-TOKEN': token
                    },
                    body: JSON.stringify(filterObj)
                });
                const resData = await res.json();
                if (resData.success) {
                    alert('Đã lưu bộ lọc thành công!');
                }
            } catch (err) {
                alert('Không thể lưu bộ lọc: ' + err.message);
            }
        });
    }

    const btnConfirmSchedule = document.getElementById('btnConfirmSchedule');
    if (btnConfirmSchedule) {
        btnConfirmSchedule.addEventListener('click', async function () {
            const freq = document.getElementById('txtScheduleFreq').value;
            const format = document.getElementById('txtScheduleFormat').value;
            const emails = document.getElementById('txtScheduleEmails').value;

            btnConfirmSchedule.disabled = true;
            btnConfirmSchedule.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...';

            try {
                const res = await fetch('/Report/ScheduleReport', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'X-CSRF-TOKEN': token
                    },
                    body: JSON.stringify({
                        periodId: data.periodId,
                        frequency: freq,
                        format: format,
                        recipientEmails: emails
                    })
                });

                const resData = await res.json();
                if (resData.success) {
                    alert(resData.message || 'Đã đặt lịch gửi email báo cáo thành công!');
                    const modalEl = document.getElementById('modalScheduleReport');
                    if (modalEl && typeof bootstrap !== 'undefined') {
                        bootstrap.Modal.getInstance(modalEl)?.hide();
                    }
                } else {
                    alert('Lỗi: ' + resData.message);
                }
            } catch (err) {
                alert('Lỗi kết nối: ' + err.message);
            } finally {
                btnConfirmSchedule.disabled = false;
                btnConfirmSchedule.innerHTML = '<i class="bi bi-send-check me-1"></i>Lưu Lịch Gửi';
            }
        });
    }

    // ==========================================================
    // 5. APEXCHARTS INITIALIZATIONS
    // ==========================================================
    if (typeof ApexCharts === 'undefined') return;

    // --- CHART 1: MINI GAUSS OVERLAY (OVERVIEW TAB) ---
    const miniGaussEl = document.querySelector("#chartMiniGauss");
    if (miniGaussEl && data.gauss && data.gauss.bands) {
        const bands = data.gauss.bands;
        const options = {
            series: [
                { name: 'Thực tế (%)', type: 'column', data: bands.map(b => b.actualPercentage) },
                { name: 'Chuẩn Gauss (%)', type: 'line', data: bands.map(b => b.idealPercentage) }
            ],
            chart: {
                height: 280,
                type: 'line',
                toolbar: { show: false },
                fontFamily: 'Inter, sans-serif'
            },
            stroke: { width: [0, 3], curve: 'smooth' },
            colors: ['#2563eb', '#f59e0b'],
            plotOptions: { bar: { columnWidth: '38%', borderRadius: 6 } },
            xaxis: { categories: bands.map(b => b.bandName) },
            yaxis: { labels: { formatter: v => v.toFixed(0) + '%' } },
            tooltip: { y: { formatter: v => v.toFixed(1) + '%' } }
        };
        new ApexCharts(miniGaussEl, options).render();
    }

    // --- CHART 2: OVERVIEW TREND LINE CHART ---
    const overviewTrendEl = document.querySelector("#chartOverviewTrend");
    if (overviewTrendEl && data.trends && data.trends.overallTrends) {
        const tr = data.trends.overallTrends;
        const options = {
            series: [{
                name: 'Điểm KPI Trung Bình',
                data: tr.map(t => t.averageScore)
            }],
            chart: {
                height: 260,
                type: 'area',
                toolbar: { show: false },
                fontFamily: 'Inter, sans-serif'
            },
            colors: ['#10b981'],
            fill: {
                type: 'gradient',
                gradient: { shadeIntensity: 1, opacityFrom: 0.45, opacityTo: 0.05, stops: [0, 90, 100] }
            },
            stroke: { curve: 'smooth', width: 3 },
            xaxis: { categories: tr.map(t => t.periodName) },
            yaxis: { min: 50, max: 100, labels: { formatter: v => v.toFixed(0) + ' đ' } },
            tooltip: { y: { formatter: v => v.toFixed(1) + ' điểm' } }
        };
        new ApexCharts(overviewTrendEl, options).render();
    }

    // --- CHART 3: MAIN GAUSS WITH CURVE POINTS (GAUSS TAB) ---
    const mainGaussEl = document.querySelector("#chartMainGauss");
    if (mainGaussEl && data.gauss && data.gauss.bands) {
        const bands = data.gauss.bands;
        const options = {
            series: [
                { name: 'Phân bổ thực tế (%)', type: 'column', data: bands.map(b => b.actualPercentage) },
                { name: 'Tỷ lệ chuẩn (%)', type: 'line', data: bands.map(b => b.idealPercentage) }
            ],
            chart: {
                height: 380,
                type: 'line',
                toolbar: { show: true },
                fontFamily: 'Inter, sans-serif',
                events: {
                    dataPointSelection: function (event, chartContext, config) {
                        const idx = config.dataPointIndex;
                        if (bands[idx]) {
                            openDrillDown(bands[idx].bandCode, bands[idx].bandName);
                        }
                    }
                }
            },
            stroke: { width: [0, 4], curve: 'smooth' },
            colors: ['#3b82f6', '#f59e0b'],
            plotOptions: { bar: { columnWidth: '45%', borderRadius: 8 } },
            xaxis: { categories: bands.map(b => b.bandName) },
            yaxis: {
                title: { text: 'Tỷ lệ nhân sự (%)' },
                labels: { formatter: v => v.toFixed(1) + '%' }
            },
            tooltip: { y: { formatter: v => v.toFixed(1) + '%' } },
            legend: { position: 'top', horizontalAlign: 'right' }
        };
        new ApexCharts(mainGaussEl, options).render();
    }

    // --- CHART 4: TREND PERIODS (TRENDS TAB) ---
    const trendPeriodsEl = document.querySelector("#chartTrendPeriods");
    if (trendPeriodsEl && data.trends && data.trends.overallTrends) {
        const tr = data.trends.overallTrends;
        const options = {
            series: [{
                name: 'Điểm trung bình toàn viện',
                data: tr.map(t => t.averageScore)
            }],
            chart: { height: 340, type: 'line', toolbar: { show: false }, fontFamily: 'Inter, sans-serif' },
            stroke: { curve: 'straight', width: 3 },
            colors: ['#059669'],
            markers: { size: 6, strokeColors: '#fff', strokeWidth: 2 },
            xaxis: { categories: tr.map(t => t.periodName) },
            yaxis: { min: 60, max: 100, labels: { formatter: v => v.toFixed(0) + ' đ' } }
        };
        new ApexCharts(trendPeriodsEl, options).render();
    }

    // --- CHART 5: DEPARTMENT COMPARISON (TRENDS TAB) ---
    const deptCompEl = document.querySelector("#chartDeptComparison");
    if (deptCompEl && data.trends && data.trends.departmentComparisons) {
        const comps = data.trends.departmentComparisons;
        const options = {
            series: [
                { name: 'Kỳ này', data: comps.map(c => c.currentScore) },
                { name: 'Kỳ trước', data: comps.map(c => c.previousScore) }
            ],
            chart: { height: 340, type: 'bar', toolbar: { show: false }, fontFamily: 'Inter, sans-serif' },
            plotOptions: { bar: { horizontal: true, barHeight: '60%', borderRadius: 4 } },
            colors: ['#2563eb', '#94a3b8'],
            xaxis: { categories: comps.map(c => c.departmentName), min: 50, max: 100 },
            legend: { position: 'top' },
            tooltip: { y: { formatter: v => v.toFixed(1) + ' điểm' } }
        };
        new ApexCharts(deptCompEl, options).render();
    }

    // --- CHART 6: HEATMAP MATRIX (TRENDS TAB) ---
    const heatmapEl = document.querySelector("#chartHeatmapMatrix");
    if (heatmapEl && data.trends && data.trends.heatmapMatrix) {
        const matrix = data.trends.heatmapMatrix;
        // Gom nhóm theo phòng ban
        const depts = [...new Set(matrix.map(m => m.departmentName))];
        const seriesData = depts.map(dName => {
            const cells = matrix.filter(m => m.departmentName === dName);
            return {
                name: dName,
                data: cells.map(c => ({ x: c.periodOrMonth, y: c.score }))
            };
        });

        const options = {
            series: seriesData,
            chart: { height: 360, type: 'heatmap', toolbar: { show: false }, fontFamily: 'Inter, sans-serif' },
            plotOptions: {
                heatmap: {
                    shadeIntensity: 0.5,
                    radius: 6,
                    use тепловаяКарта: false,
                    colorScale: {
                        ranges: [
                            { from: 0, to: 59.9, name: '< 60 (Kém)', color: '#ef4444' },
                            { from: 60, to: 74.9, name: '60 - 74 (Đạt)', color: '#f59e0b' },
                            { from: 75, to: 89.9, name: '75 - 89 (Tốt)', color: '#3b82f6' },
                            { from: 90, to: 100, name: '>= 90 (Xuất sắc)', color: '#10b981' }
                        ]
                    }
                }
            },
            dataLabels: { enabled: true, formatter: v => v.toFixed(1) }
        };
        new ApexCharts(heatmapEl, options).render();
    }

    // --- CHART 7: SCATTER PLOT (DATAVIZ TAB) ---
    const scatterEl = document.querySelector("#chartScatterPlot");
    if (scatterEl && data.dataViz && data.dataViz.scatterPoints) {
        const sc = data.dataViz.scatterPoints;
        const options = {
            series: [{
                name: 'Điểm Cán Bộ',
                data: sc.map(p => [p.selfScore, p.managerScore])
            }],
            chart: { height: 360, type: 'scatter', toolbar: { show: false }, fontFamily: 'Inter, sans-serif' },
            xaxis: {
                title: { text: 'Điểm Tự Chấm (Self Score)' },
                min: 40, max: 100,
                labels: { formatter: v => v.toFixed(0) }
            },
            yaxis: {
                title: { text: 'Điểm Quản Lý Thẩm Định (Manager Score)' },
                min: 40, max: 100,
                labels: { formatter: v => v.toFixed(0) }
            },
            colors: ['#ef4444'],
            markers: { size: 7, hover: { size: 10 } },
            tooltip: {
                custom: function ({ series, seriesIndex, dataPointIndex, w }) {
                    const pt = sc[dataPointIndex];
                    if (!pt) return '';
                    return `
                        <div class="p-2 small">
                            <div class="fw-bold">${pt.fullName}</div>
                            <div class="text-muted">${pt.departmentName}</div>
                            <hr class="my-1"/>
                            <div>Tự chấm: <strong>${pt.selfScore.toFixed(1)}</strong></div>
                            <div>Thẩm định: <strong>${pt.managerScore.toFixed(1)}</strong></div>
                            <div class="text-${pt.isOutlier ? 'danger fw-bold' : 'success'}">${pt.isOutlier ? '⚠️ Điểm lệch pha >= 15' : 'Hài hòa'}</div>
                        </div>
                    `;
                }
            }
        };
        new ApexCharts(scatterEl, options).render();
    }

    // --- CHART 8: FUNNEL CHART (DATAVIZ TAB) ---
    const funnelEl = document.querySelector("#chartFunnel");
    if (funnelEl && data.dataViz && data.dataViz.funnelStages) {
        const st = data.dataViz.funnelStages;
        const options = {
            series: [{
                name: 'Số lượng nhân sự',
                data: st.map(s => s.count)
            }],
            chart: { type: 'bar', height: 360, toolbar: { show: false }, fontFamily: 'Inter, sans-serif' },
            plotOptions: {
                bar: {
                    borderRadius: 4,
                    horizontal: true,
                    barHeight: '65%',
                    isFunnel: true
                }
            },
            colors: ['#6366f1'],
            dataLabels: {
                enabled: true,
                formatter: function (val, opt) {
                    const pct = st[opt.dataPointIndex]?.percentage || 0;
                    return `${val} CB (${pct}%)`;
                },
                dropShadow: { enabled: false }
            },
            xaxis: { categories: st.map(s => s.stageName) }
        };
        new ApexCharts(funnelEl, options).render();
    }

    // --- CHART 9: BOX & WHISKER PLOT (DATAVIZ TAB) ---
    const boxPlotEl = document.querySelector("#chartBoxPlot");
    if (boxPlotEl && data.dataViz && data.dataViz.boxPlots && data.dataViz.boxPlots.length > 0) {
        const bp = data.dataViz.boxPlots;
        const options = {
            series: [{
                type: 'boxPlot',
                data: bp.map(b => ({
                    x: b.departmentName,
                    y: [b.min, b.q1, b.median, b.q3, b.max]
                }))
            }],
            chart: { type: 'boxPlot', height: 360, toolbar: { show: false }, fontFamily: 'Inter, sans-serif' },
            colors: ['#0284c7', '#38bdf8'],
            yaxis: { min: 40, max: 100, labels: { formatter: v => v.toFixed(0) + ' đ' } }
        };
        new ApexCharts(boxPlotEl, options).render();
    }

    function capitalize(s) {
        return s.charAt(0).toUpperCase() + s.slice(1);
    }
});
