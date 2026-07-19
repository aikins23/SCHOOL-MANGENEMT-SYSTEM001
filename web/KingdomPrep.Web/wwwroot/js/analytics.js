// Nyansapo Web Analytics Dashboard Chart.js Renderer
// Implements modern, vibrant visual design with rich aesthetics and micro-animations

window._nyansapoCharts = window._nyansapoCharts || {};

window.renderNyansapoAnalyticsCharts = async function (data) {
    if (typeof Chart === 'undefined') {
        await new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
            script.onload = resolve;
            script.onerror = () => {
                console.error('Failed to load Chart.js from CDN.');
                reject(new Error('Chart.js CDN load failed'));
            };
            document.head.appendChild(script);
        });
    }

    // Set global default font and animation styling
    Chart.defaults.font.family = "'Segoe UI', 'Inter', -apple-system, sans-serif";
    Chart.defaults.color = "#475569";
    Chart.defaults.scale.grid.color = "rgba(226, 232, 240, 0.6)";

    const palettes = {
        primary: ["#0b1f48", "#1e3a8a", "#3b82f6", "#60a5fa", "#93c5fd"],
        gold: ["#d4af37", "#f59e0b", "#fbbf24", "#fcd34d", "#fef08a"],
        emerald: ["#10b981", "#059669", "#34d399", "#6ee7b7", "#a7f3d0"],
        crimson: ["#f43f5e", "#e11d48", "#fb7185", "#fda4af", "#ffe4e6"],
        violet: ["#8b5cf6", "#7c3aed", "#a78bfa", "#c4b5fd", "#ede9fe"],
        cyan: ["#0ea5e9", "#0284c7", "#38bdf8", "#7dd3fc", "#bae6fd"],
        multicolor: [
            "#0b1f48", "#d4af37", "#10b981", "#f43f5e", "#8b5cf6",
            "#0ea5e9", "#f59e0b", "#6366f1", "#ec4899", "#14b8a6",
            "#64748b", "#84cc16", "#d97706", "#4f46e5", "#06b6d4"
        ]
    };

    function destroyChart(canvasId) {
        if (window._nyansapoCharts[canvasId]) {
            window._nyansapoCharts[canvasId].destroy();
            delete window._nyansapoCharts[canvasId];
        }
    }

    function createChart(canvasId, config) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        destroyChart(canvasId);
        const ctx = canvas.getContext('2d');
        window._nyansapoCharts[canvasId] = new Chart(ctx, config);
    }

    function getGradient(ctx, colorStart, colorEnd) {
        const gradient = ctx.createLinearGradient(0, 0, 0, 300);
        gradient.addColorStop(0, colorStart);
        gradient.addColorStop(1, colorEnd);
        return gradient;
    }

    // 1. Fee Collection Status (Doughnut)
    if (data.feeCollectionStatus) {
        createChart('chartFeeStatus', {
            type: 'doughnut',
            data: {
                labels: data.feeCollectionStatus.map(x => x.label),
                datasets: [{
                    data: data.feeCollectionStatus.map(x => x.value),
                    backgroundColor: ['#10b981', '#f43f5e'],
                    borderColor: ['#059669', '#e11d48'],
                    borderWidth: 2,
                    hoverOffset: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { padding: 15, font: { weight: 'bold' } } },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                let val = context.raw || 0;
                                return context.label + ': GHS ' + Number(val).toLocaleString();
                            }
                        }
                    }
                },
                cutout: '65%'
            }
        });
    }

    // 2. Student Enrollment by Class (Bar)
    if (data.enrollmentByClass) {
        const canvas = document.getElementById('chartEnrollment');
        let bg = '#0b1f48';
        if (canvas) {
            const ctx = canvas.getContext('2d');
            bg = getGradient(ctx, '#0b1f48', '#3b82f6');
        }
        createChart('chartEnrollment', {
            type: 'bar',
            data: {
                labels: data.enrollmentByClass.map(x => x.label),
                datasets: [{
                    label: 'Students',
                    data: data.enrollmentByClass.map(x => x.value),
                    backgroundColor: bg,
                    borderColor: '#0b1f48',
                    borderWidth: 1,
                    borderRadius: 6,
                    hoverBackgroundColor: '#d4af37'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { borderDash: [4, 4] } }
                }
            }
        });
    }

    // 3. Average Exam Score by Subject (Bar)
    if (data.examScoreBySubject) {
        createChart('chartExamScores', {
            type: 'bar',
            data: {
                labels: data.examScoreBySubject.map(x => x.label),
                datasets: [{
                    label: 'Avg Score (%)',
                    data: data.examScoreBySubject.map(x => x.value),
                    backgroundColor: '#0ea5e9',
                    borderColor: '#0284c7',
                    borderWidth: 1,
                    borderRadius: 6,
                    hoverBackgroundColor: '#6366f1'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, max: 100, grid: { borderDash: [4, 4] } }
                }
            }
        });
    }

    // 4. Monthly Fee Collection Trend (Line)
    if (data.monthlyFeeTrend) {
        const canvas = document.getElementById('chartFeeTrend');
        let bg = 'rgba(16, 185, 129, 0.15)';
        if (canvas) {
            const ctx = canvas.getContext('2d');
            bg = getGradient(ctx, 'rgba(16, 185, 129, 0.4)', 'rgba(16, 185, 129, 0.0)');
        }
        createChart('chartFeeTrend', {
            type: 'line',
            data: {
                labels: data.monthlyFeeTrend.map(x => x.label),
                datasets: [{
                    label: 'Collections (GHS)',
                    data: data.monthlyFeeTrend.map(x => x.value),
                    borderColor: '#10b981',
                    backgroundColor: bg,
                    borderWidth: 3,
                    fill: true,
                    tension: 0.35,
                    pointBackgroundColor: '#10b981',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 5,
                    pointHoverRadius: 7
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => 'Collected: GHS ' + Number(ctx.raw).toLocaleString()
                        }
                    }
                },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    // 5. Monthly Attendance Rate (%) (Line)
    if (data.monthlyAttendanceRate) {
        createChart('chartAttendanceTrend', {
            type: 'line',
            data: {
                labels: data.monthlyAttendanceRate.map(x => x.label),
                datasets: [{
                    label: 'Attendance Rate (%)',
                    data: data.monthlyAttendanceRate.map(x => x.value),
                    borderColor: '#38bdf8',
                    backgroundColor: 'rgba(56, 189, 248, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.3,
                    pointBackgroundColor: '#0284c7',
                    pointRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { min: 50, max: 100 } }
            }
        });
    }

    // 6. Income vs Expenses (Grouped Bar)
    if (data.incomeVsExpenses) {
        createChart('chartIncomeExpense', {
            type: 'bar',
            data: {
                labels: data.incomeVsExpenses.map(x => x.label),
                datasets: [
                    {
                        label: 'Income',
                        data: data.incomeVsExpenses.map(x => x.value),
                        backgroundColor: '#10b981',
                        borderRadius: 4
                    },
                    {
                        label: 'Expenses',
                        data: data.incomeVsExpenses.map(x => x.value2),
                        backgroundColor: '#f43f5e',
                        borderRadius: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    // 7. Staff Leave Status (Doughnut)
    if (data.staffLeaveStatus) {
        createChart('chartLeaveStatus', {
            type: 'doughnut',
            data: {
                labels: data.staffLeaveStatus.map(x => x.label),
                datasets: [{
                    data: data.staffLeaveStatus.map(x => x.value),
                    backgroundColor: ['#10b981', '#f59e0b', '#f43f5e', '#64748b'],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } },
                cutout: '60%'
            }
        });
    }

    // 8. Exam Grade Distribution (Bar)
    if (data.gradeDistribution) {
        createChart('chartGradeDist', {
            type: 'bar',
            data: {
                labels: data.gradeDistribution.map(x => x.label),
                datasets: [{
                    label: 'Students',
                    data: data.gradeDistribution.map(x => x.value),
                    backgroundColor: palettes.multicolor,
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    // 9. Attendance Rate by Class (Horizontal Bar)
    if (data.attendanceByClass) {
        createChart('chartAttendanceByClass', {
            type: 'bar',
            data: {
                labels: data.attendanceByClass.map(x => x.label),
                datasets: [{
                    label: 'Rate (%)',
                    data: data.attendanceByClass.map(x => x.value),
                    backgroundColor: '#8b5cf6',
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { x: { min: 60, max: 100 } }
            }
        });
    }

    // 10. Outstanding Fees by Class (Bar)
    if (data.outstandingByClass) {
        createChart('chartOutstandingByClass', {
            type: 'bar',
            data: {
                labels: data.outstandingByClass.map(x => x.label),
                datasets: [{
                    label: 'Outstanding (GHS)',
                    data: data.outstandingByClass.map(x => x.value),
                    backgroundColor: '#f43f5e',
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    // 11. Payment Method Breakdown (Doughnut)
    if (data.paymentMethodBreakdown) {
        createChart('chartPaymentMethod', {
            type: 'doughnut',
            data: {
                labels: data.paymentMethodBreakdown.map(x => x.label),
                datasets: [{
                    data: data.paymentMethodBreakdown.map(x => x.value),
                    backgroundColor: ['#0ea5e9', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899'],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } },
                cutout: '55%'
            }
        });
    }

    // 12. Staff by Department (Doughnut)
    if (data.staffByDepartment) {
        createChart('chartStaffDept', {
            type: 'doughnut',
            data: {
                labels: data.staffByDepartment.map(x => x.label),
                datasets: [{
                    data: data.staffByDepartment.map(x => x.value),
                    backgroundColor: ['#6366f1', '#0ea5e9', '#10b981', '#f59e0b', '#f43f5e'],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } }
            }
        });
    }

    // 13. Expense Breakdown by Category (Horizontal Bar)
    if (data.expenseByCategory) {
        createChart('chartExpenseCategory', {
            type: 'bar',
            data: {
                labels: data.expenseByCategory.map(x => x.label),
                datasets: [{
                    label: 'Amount (GHS)',
                    data: data.expenseByCategory.map(x => x.value),
                    backgroundColor: '#d4af37',
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { x: { beginAtZero: true } }
            }
        });
    }

    // 14. Top Absent Students (Horizontal Bar)
    if (data.topAbsentStudents) {
        createChart('chartTopAbsent', {
            type: 'bar',
            data: {
                labels: data.topAbsentStudents.map(x => x.label),
                datasets: [{
                    label: 'Absences',
                    data: data.topAbsentStudents.map(x => x.value),
                    backgroundColor: '#e11d48',
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { x: { beginAtZero: true } }
            }
        });
    }

    // 15. Class Average Score Comparison (Bar)
    if (data.classAverageScore) {
        createChart('chartClassAvg', {
            type: 'bar',
            data: {
                labels: data.classAverageScore.map(x => x.label),
                datasets: [{
                    label: 'Class Avg (%)',
                    data: data.classAverageScore.map(x => x.value),
                    backgroundColor: '#14b8a6',
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { min: 40, max: 100 } }
            }
        });
    }

    // 16. Student Gender Distribution (Pie)
    if (data.genderDistribution) {
        createChart('chartGender', {
            type: 'pie',
            data: {
                labels: data.genderDistribution.map(x => x.label),
                datasets: [{
                    data: data.genderDistribution.map(x => x.value),
                    backgroundColor: ['#0ea5e9', '#ec4899', '#f59e0b'],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } }
            }
        });
    }

    // 18. Admissions per Year (Bar)
    if (data.admissionsPerYear) {
        createChart('chartAdmissionsYear', {
            type: 'bar',
            data: {
                labels: data.admissionsPerYear.map(x => x.label),
                datasets: [{
                    label: 'Admissions',
                    data: data.admissionsPerYear.map(x => x.value),
                    backgroundColor: '#0b1f48',
                    borderColor: '#d4af37',
                    borderWidth: 2,
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    // 19. Active vs Rolled-out Students (Doughnut)
    if (data.activeVsRolledOut) {
        createChart('chartActiveRolledOut', {
            type: 'doughnut',
            data: {
                labels: data.activeVsRolledOut.map(x => x.label),
                datasets: [{
                    data: data.activeVsRolledOut.map(x => x.value),
                    backgroundColor: ['#10b981', '#64748b', '#f59e0b'],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } }
            }
        });
    }

    // 20. Salary Spend by Department (Bar)
    if (data.salaryByDepartment) {
        createChart('chartSalaryDept', {
            type: 'bar',
            data: {
                labels: data.salaryByDepartment.map(x => x.label),
                datasets: [{
                    label: 'Total Salary (GHS)',
                    data: data.salaryByDepartment.map(x => x.value),
                    backgroundColor: '#6366f1',
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { x: { beginAtZero: true } }
            }
        });
    }

    // 21. Subject Pass/Fail Rate (Grouped Bar)
    if (data.subjectPassFail) {
        createChart('chartSubjectPassFail', {
            type: 'bar',
            data: {
                labels: data.subjectPassFail.map(x => x.label),
                datasets: [
                    {
                        label: 'Pass (≥50%)',
                        data: data.subjectPassFail.map(x => x.value),
                        backgroundColor: '#10b981',
                        borderRadius: 4
                    },
                    {
                        label: 'Fail (<50%)',
                        data: data.subjectPassFail.map(x => x.value2),
                        backgroundColor: '#f43f5e',
                        borderRadius: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    console.log('All Nyansapo analytics charts rendered successfully.');
};
