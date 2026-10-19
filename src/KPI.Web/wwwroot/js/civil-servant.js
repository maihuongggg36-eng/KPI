/**
 * Civil Servant Decree 90/2020 & 48/2023 Module
 * Script for Index view: Gauge Meter (BR-06) & Dynamic interactions
 */

document.addEventListener('DOMContentLoaded', function () {
    initQuotaGauge();
});

function initQuotaGauge() {
    const gaugeEl = document.querySelector('#excellenceQuotaGauge');
    if (!gaugeEl || !window.quotaData) return;

    const data = window.quotaData;
    const rate = parseFloat(data.rate) || 0;
    const isExceeded = data.isExceeded;

    // Tính % lấp đầy theo hạn mức trần 20% (ví dụ: 20% là 100% hạn mức)
    // Nếu rate = 20% -> fillRate = 100%. Nếu rate = 25% -> fillRate = 125%
    const relativeToCapPercent = Math.min(100, Math.round((rate / 20) * 100));

    // Lựa chọn màu sắc dựa trên quy tắc BR-06
    let strokeColor = '#10b981'; // Xanh lá khi an toàn <= 15%
    if (rate > 15 && rate <= 20) {
        strokeColor = '#f59e0b'; // Vàng cam khi tiệm cận trần 15-20%
    }
    if (isExceeded || rate > 20) {
        strokeColor = '#ef4444'; // Đỏ rực khi vượt trần 20%
    }

    const options = {
        series: [rate],
        chart: {
            height: 230,
            type: 'radialBar',
            sparkline: {
                enabled: true
            },
            animations: {
                enabled: true,
                easing: 'easeinout',
                speed: 1000,
                animateGradually: {
                    enabled: true,
                    delay: 150
                },
                dynamicAnimation: {
                    enabled: true,
                    speed: 350
                }
            }
        },
        plotOptions: {
            radialBar: {
                startAngle: -135,
                endAngle: 135,
                hollow: {
                    margin: 0,
                    size: '72%',
                    background: 'transparent',
                    dropShadow: {
                        enabled: false
                    }
                },
                track: {
                    background: '#e2e8f0',
                    strokeWidth: '97%',
                    margin: 5,
                    dropShadow: {
                        enabled: false
                    }
                },
                dataLabels: {
                    name: {
                        show: true,
                        fontSize: '12px',
                        fontFamily: 'Inter, sans-serif',
                        fontWeight: 600,
                        color: '#64748b',
                        offsetY: -10
                    },
                    value: {
                        offsetY: 5,
                        fontSize: '26px',
                        fontFamily: 'Plus Jakarta Sans, sans-serif',
                        fontWeight: 700,
                        color: strokeColor,
                        formatter: function (val) {
                            return val + '%';
                        }
                    }
                }
            }
        },
        fill: {
            type: 'gradient',
            gradient: {
                shade: 'dark',
                type: 'horizontal',
                shadeIntensity: 0.5,
                gradientToColors: isExceeded ? ['#dc2626'] : ['#3b82f6'],
                inverseColors: true,
                opacityFrom: 1,
                opacityTo: 1,
                stops: [0, 100]
            }
        },
        colors: [strokeColor],
        stroke: {
            lineCap: 'round',
            dashArray: 0
        },
        labels: [isExceeded ? 'VƯỢT TRẦN 20%' : 'Tỷ lệ Xuất sắc']
    };

    const chart = new ApexCharts(gaugeEl, options);
    chart.render();
}
