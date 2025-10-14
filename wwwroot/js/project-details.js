// Project Details Page Script (extracted from Razor view)
(function(){
    'use strict';

    const root = document.getElementById('project-details-root');
    let cfg = {};
    if (root) {
        try { cfg = JSON.parse(root.getAttribute('data-config') || '{}'); } catch { cfg = {}; }
    }

    // ===================== Utilities =====================
    function initTooltips() {
        const list = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        list.map(el => new bootstrap.Tooltip(el));
    }

    function animateCounters() {
        const counters = document.querySelectorAll('.count');
        counters.forEach(el => {
            const target = parseInt(el.textContent || '0');
            let current = 0;
            const stepsTarget = 50; // configurable base
            const step = Math.max(1, Math.round(target / stepsTarget));
            const intervalMs = 40;
            const intv = setInterval(() => {
                current += step;
                if (current >= target) { current = target; clearInterval(intv); }
                el.textContent = current.toString();
            }, intervalMs);
        });
    }

    function initTasksTable() {
        if (!window.jQuery || !$.fn.DataTable) return;
        $('#tasksTable').DataTable({
            order: [],
            responsive: true,
            columnDefs: [{ targets: [-1, -2], orderable: false }],
            language: {
                search: 'بحث:',
                searchPlaceholder: 'اكتب للبحث...',
                lengthMenu: 'إظهار _MENU_ مُدخل',
                paginate: { previous: '&lt;', next: '&gt;' },
                info: 'إظهار _START_ إلى _END_ من أصل _TOTAL_ مُدخل',
                infoEmpty: 'لا توجد بيانات لعرضها',
                zeroRecords: 'لا توجد نتائج مطابقة',
                infoFiltered: '(مصفاة من إجمالي _MAX_ مُدخل)'
            }
        });
    }

    function initImportExcel() {
        const btn = document.getElementById('btnImportExcel');
        const fileInput = document.getElementById('excelFileInput');
        const modalEl = document.getElementById('importPreviewModal');
        if (!btn || !fileInput || !modalEl || !cfg.uploadExcelUrl || !cfg.projectId) return;
        const modal = new bootstrap.Modal(modalEl);
        btn.addEventListener('click', () => fileInput.click());
        fileInput.addEventListener('change', () => {
            if (!fileInput.files || fileInput.files.length === 0) return;
            const formData = new FormData();
            formData.append('projectId', cfg.projectId);
            formData.append('file', fileInput.files[0]);
            fetch(cfg.uploadExcelUrl, { method: 'POST', body: formData })
                .then(async r => { if (!r.ok) throw new Error(await r.text() || 'فشل رفع الملف'); return r.json(); })
                .then(data => { document.getElementById('importPreviewContent').innerHTML = data.html; bindTasksImportConfirm(); modal.show(); })
                .catch(err => alert('خطأ: ' + (err.message || 'فشل معالجة الملف'))) 
                .finally(() => { fileInput.value = ''; });
        });
    }

    function bindTasksImportConfirm() {
        const btn = document.getElementById('confirmImportBtn');
        if (!btn) return;
        const showError = msg => {
            const box = document.querySelector('#importPreviewModal #importErrorBox');
            if (box) { box.textContent = msg || 'حدث خطأ أثناء الحفظ'; box.classList.remove('d-none'); box.scrollIntoView({behavior:'smooth'}); }
        };
        const clearError = () => { const box = document.querySelector('#importPreviewModal #importErrorBox'); if (box){ box.textContent=''; box.classList.add('d-none'); } };
        btn.onclick = async () => {
            if (btn.classList.contains('disabled') || btn.hasAttribute('disabled')) return;
            clearError();
            const projectId = parseInt(btn.getAttribute('data-project-id')) || 0;
            const url = btn.getAttribute('data-confirm-url');
            const inputs = document.querySelectorAll('#importPreviewModal table input[data-index]');
            let max = -1; inputs.forEach(i=>{ const idx=parseInt(i.getAttribute('data-index')); if(!isNaN(idx)) max=Math.max(max,idx); });
            const getVal = (i,f) => (document.querySelector('input[data-index="'+i+'"][data-field="'+f+'"]')||{}).value || '';
            const rows = [];
            for (let i=0;i<=max;i++) {
                rows.push({ stageName:getVal(i,'StageName'), taskName:getVal(i,'TaskName'), implementorDepartment:getVal(i,'ImplementorDepartment'), departmentResponsible:getVal(i,'DepartmentResponsible'), definitionOfDone:getVal(i,'DefinitionOfDone'), manyDaysToComplete: parseInt(getVal(i,'ManyDaysToComplete'))||null, expectedStartDate:getVal(i,'ExpectedStartDate')||null, expectedEndDate:getVal(i,'ExpectedEndDate')||null, doneRatio: parseFloat(getVal(i,'DoneRatio'))||0, plannedPercent: parseFloat(getVal(i,'PlannedPercent'))||null, plannedCost: parseFloat(getVal(i,'PlannedCost'))||null });
            }
            try {
                const r = await fetch(url, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ projectId, rows }) });
                if (!r.ok) { const msg = (await r.json().catch(()=>({message:'فشل الحفظ'}))).message || 'فشل الحفظ'; showError(msg); return; }
                bootstrap.Modal.getInstance(document.getElementById('importPreviewModal'))?.hide();
                window.location.reload();
            } catch(err) { showError(err?.message || 'حدث خطأ أثناء الحفظ'); }
        };
    }

    function initYearWeightChart() {
        if (!cfg.years || !cfg.weights) return;
        const years = cfg.years;
        const weights = cfg.weights;
        const total = cfg.totalAllYears || 0;
        if (!Array.isArray(years) || !Array.isArray(weights) || total <= 0) {
            const host = document.getElementById('stacked-bar-chart');
            if (host) host.innerHTML = '<div class="text-muted small text-center py-3">لا توجد بيانات لعرضها</div>';
            return;
        }
        const items = years.map((y,i)=>({year:String(y), value:Number(weights[i]||0)})).filter(it=>it.value>0);
        if (items.length === 0) { const host = document.getElementById('stacked-bar-chart'); if (host) host.innerHTML='<div class="text-muted small text-center py-3">لا توجد بيانات لعرضها</div>'; return; }
        const colors = ['#1abc9c','#3498db','#9b59b6','#f1c40f','#e67e22','#e74c3c','#2ecc71','#7f8c8d'];
        const series = items.map(it => [{ value: it.value, meta: it.year }]);
        let segments = [];
        const chart = new Chartist.Bar('#stacked-bar-chart', { labels: [''], series }, {
            stackBars: true,
            horizontalBars: true,
            fullWidth: true,
            seriesBarDistance: 0,
            axisX: { low: 0, high: total, onlyInteger: true, labelInterpolationFnc: () => '', showGrid:false },
            axisY: { offset: 10, showGrid:false },
            chartPadding: { top: 70, right: 18, bottom: 18, left: 12 },
            height: '240px',
            plugins: [ Chartist.plugins.tooltip({ appendToBody: true, transformTooltipTextFnc: v => (Number(v)||0) + '%' }) ]
        });
        chart.on('draw', data => {
            if (data.type !== 'bar') return;
            const idx = data.seriesIndex;
            const color = colors[idx % colors.length];
            const value = items[idx].value;
            data.element.attr({ style: 'stroke:' + color + ';stroke-width:34px;stroke-linecap:round;filter:drop-shadow(0 1px 3px rgba(0,0,0,.12));' });
            const segW = Math.max(0, data.x2 - data.x1);
            const cx = data.x1 + segW / 2;
            segments.push({ x: cx, color, year: items[idx].year, value });
        });
        chart.on('created', ctx => {
            const layer = ctx.svg.elem('g', { class: 'ct-labels-above' });
            const baseAbove = ctx.chartRect.y2 - 28;
            const rowGap = 20;
            segments.sort((a,b)=>a.x-b.x);
            const placed = [];
            segments.forEach(s => {
                const text = s.year + ' • ' + s.value + '%';
                const approxWidth = Math.max(48, text.length * 7.2);
                const half = approxWidth / 2;
                let row = 0; let x1, x2; let tries=0;
                while(true){ x1=s.x-half; x2=s.x+half; const collides=placed.some(p=>p.row===row && !(x2<p.x1 || x1>p.x2)); if(!collides){placed.push({row,x1,x2}); break;} row++; if(++tries>50) break; }
                const y = baseAbove - (row * rowGap);
                layer.elem('line', { x1:s.x, y1:y+6, x2:s.x, y2:ctx.chartRect.y2 - 6 }).attr({ style:'stroke:'+s.color+';stroke-width:1;opacity:.95;' });
                layer.elem('path', { d:'M '+s.x+' '+(ctx.chartRect.y2-6)+' L '+(s.x-3)+' '+(ctx.chartRect.y2-13)+' L '+(s.x+3)+' '+(ctx.chartRect.y2-13)+' Z' }).attr({ style:'fill:'+s.color+';opacity:.95;' });
                layer.elem('text', { x:s.x, y:y, style:'fill:#2b2f33;font-size:12px;font-weight:600;text-anchor:middle;' }).text(text);
            });
            segments = [];
        });
    }

    function initPieCharts() {
        if (!window.ApexCharts) return;
        new ApexCharts(document.querySelector('#tasksPieChart'), {
            chart: { type: 'pie', height: 250 },
            series: [cfg.completed, cfg.inProgress, cfg.notStarted, cfg.overdue],
            labels: ['مكتملة','قيد التنفيذ','لم تبدأ','متأخرة'],
            colors: ['#198754','#0dcaf0','#ffc107','#dc3545'],
            legend: { position: 'bottom', fontFamily: 'inherit', labels: { colors: 'inherit' } },
            dataLabels: { enabled: true, formatter: (val,opts) => opts.w.globals.series[opts.seriesIndex] }
        }).render();
        new ApexCharts(document.querySelector('#costPieChart'), {
            chart: { type: 'pie', height: 250 },
            series: [ cfg.totalTasksCostPercent || 0, cfg.remainingCostPercent || 0 ],
            labels: ['تكلفة المهام','بقية تكلفة المشروع'],
            colors: ['#00c292','#e0cb09'],
            legend: { position: 'bottom', fontFamily: 'inherit', labels: { colors: 'inherit' } },
            dataLabels: { enabled: true, formatter: val => val + '%' }
        }).render();
    }

    document.addEventListener('DOMContentLoaded', function(){
        initTooltips();
        animateCounters();
        initTasksTable();
        initImportExcel();
        initPieCharts();
        initYearWeightChart();
    });
})();
