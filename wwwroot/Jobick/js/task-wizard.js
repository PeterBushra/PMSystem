(function () {
    'use strict';

    const form = document.querySelector('form.needs-validation');
    if (!form) return;

    // Wizard elements
    const steps = Array.from(document.querySelectorAll('.form-step'));
    const stepsNav = document.getElementById('stepsNav');
    const stepCaption = document.getElementById('stepCaption');
    const prevBtn = document.getElementById('prevStepBtn');
    const nextBtn = document.getElementById('nextStepBtn');
    const saveBtnTop = document.getElementById('saveBtn');
    let currentStep = 0;

    // Localization for HTML5 validation messages (Arabic)
    function setupArabicValidation() {
        function setMessage(input) {
            // Clear any previous custom message first
            input.setCustomValidity('');
            const v = input.validity;
            if (v.valueMissing) {
                input.setCustomValidity('يرجى تعبئة هذا الحقل.');
            } else if (v.typeMismatch) {
                if (input.type === 'email') input.setCustomValidity('يرجى إدخال بريد إلكتروني صالح.');
                else if (input.type === 'url') input.setCustomValidity('يرجى إدخال رابط صالح.');
                else input.setCustomValidity('القيمة المدخلة غير صالحة.');
            } else if (v.tooShort && input.minLength > -1) {
                input.setCustomValidity(`يجب إدخال ${input.minLength} أحرف على الأقل.`);
            } else if (v.tooLong && input.maxLength > -1) {
                input.setCustomValidity(`يجب ألا يتجاوز الإدخال ${input.maxLength} حرفًا.`);
            } else if (v.rangeUnderflow && input.min !== '') {
                input.setCustomValidity(`الحد الأدنى المسموح هو ${input.min}.`);
            } else if (v.rangeOverflow && input.max !== '') {
                input.setCustomValidity(`الحد الأقصى المسموح هو ${input.max}.`);
            } else if (v.stepMismatch) {
                input.setCustomValidity('القيمة غير مسموحة لهذه الخطوة.');
            } else if (v.patternMismatch) {
                input.setCustomValidity('القيمة لا تطابق النمط المطلوب.');
            }
        }

        const allInputs = form.querySelectorAll('input, select, textarea');
        allInputs.forEach((el) => {
            // When the browser detects invalid, set our Arabic message
            el.addEventListener('invalid', () => setMessage(el));
            // Clear the message on user edits
            el.addEventListener('input', () => { el.setCustomValidity(''); el.classList.remove('is-invalid'); });
            el.addEventListener('change', () => { el.setCustomValidity(''); el.classList.remove('is-invalid'); });
        });
    }

    setupArabicValidation();

    function showStep(index) {
        if (index < 0 || index >= steps.length) return;
        steps.forEach((s, i) => s.classList.toggle('d-none', i !== index));
        currentStep = index;
        stepsNav?.querySelectorAll('.nav-link').forEach((l, i) => {
            l.classList.toggle('active', i === currentStep);
            l.setAttribute('aria-current', i === currentStep ? 'step' : 'false');
        });
        if (stepCaption) stepCaption.textContent = `الخطوة ${currentStep + 1} من ${steps.length}`;
        if (prevBtn) prevBtn.disabled = currentStep === 0;
        if (nextBtn) nextBtn.classList.toggle('d-none', currentStep === steps.length - 1);
        if (saveBtnTop) saveBtnTop.classList.toggle('d-none', currentStep !== steps.length - 1);
    }

    stepsNav?.addEventListener('click', (e) => {
        const btn = e.target.closest('button[data-step]');
        if (!btn) return;
        const to = parseInt(btn.dataset.step);
        if (!isNaN(to)) showStep(to);
    });

    prevBtn?.addEventListener('click', () => showStep(currentStep - 1));
    nextBtn?.addEventListener('click', () => {
        const pane = steps[currentStep];
        const inputs = Array.from(pane.querySelectorAll('input[required], textarea[required], select[required]'));
        let ok = true;
        for (const i of inputs) {
            // Reset to allow browser to recompute validity and use our localized messages
            i.setCustomValidity('');
            if (!i.checkValidity()) {
                // Trigger showing the message (will use our custom message set in invalid handler)
                i.reportValidity?.();
                i.classList.add('is-invalid');
                ok = false;
                break;
            }
        }
        if (!ok) return;
        showStep(currentStep + 1);
    });

    // Cache UI elements
    const els = {
        weightInput: document.getElementById('weightInput'),
        fillWeightBtn: document.getElementById('fillWeightBtn'),
        weightError: document.getElementById('weightError'),
        weightProgressBar: document.getElementById('weightProgressBar'),
        totalWeightWithCurrent: document.getElementById('totalWeightWithCurrent'),
        otherWeightSpan: document.getElementById('otherWeightSpan'),
        remainingWeightSpan: document.getElementById('remainingWeightSpan'),
        saveBtn: document.getElementById('saveBtn'),
        doneRatioInput: document.getElementById('doneRatioInput'),
        doneProgressBar: document.getElementById('doneProgressBar'),
        attachmentContainer: document.getElementById('attachmentContainer'),
        actualEndDateInput: document.getElementById('actualEndDateInput'),
        attachmentInput: document.getElementById('Attachment'),
        attachmentError: document.getElementById('attachmentError'),
        attachmentInfo: document.getElementById('attachmentInfo'),
        costInput: document.getElementById('costInput'),
        fillCostBtn: document.getElementById('fillCostBtn'),
        costWarning: document.getElementById('costWarning'),
        remainingBeforeSpan: document.getElementById('remainingBeforeSpan'),
        logsTableBody: document.querySelector('#logsTable tbody'),
        addLogRowBtn: document.getElementById('addLogRowBtn'),
        completeTodayBtn: document.getElementById('completeTodayBtn'),
        logsError: document.getElementById('logsError'),
        logsTotalSpan: document.getElementById('logsTotalSpan'),
        expectedStart: document.getElementById('expectedStart'),
        expectedEnd: document.getElementById('expectedEnd'),
        autoDaysChk: document.getElementById('autoDaysChk'),
        computedDays: document.getElementById('computedDays'),
        daysToComplete: document.getElementById('daysToComplete')
    };

    const state = {
        otherWeight: parseFloat(form.dataset.otherWeightSum) || 0,
        weightLimit: 100,
        hasTotal: form.dataset.hasTotal === 'true',
        projectTotal: parseFloat(form.dataset.projectTotal) || 0,
        existingCost: parseFloat(form.dataset.existingCost) || 0,
        hasExistingAttachment: form.dataset.hasExistingAttachment === 'true',
        weightInvalid: false,
        completionInvalid: false,
        logsInvalid: false
    };

    const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10 MB

    // Helpers
    function fmtPercent(n) { try { return n.toLocaleString('ar-EG', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); } catch { return (Math.round(n * 100) / 100).toString(); } }
    function fmtNumber(n) { try { return n.toLocaleString('ar-EG', { minimumFractionDigits: 0, maximumFractionDigits: 0 }); } catch { return Math.round(n).toString(); } }
    function applySaveState() { if (els.saveBtn) els.saveBtn.disabled = !!(state.weightInvalid || state.completionInvalid || state.logsInvalid); }

    // Dates helpers
    function parseDate(value) { if (!value) return null; const d = new Date(value + 'T00:00:00'); return isNaN(d.getTime()) ? null : d; }
    function addDays(date, days) { const d = new Date(date); d.setDate(d.getDate() + days); return d; }
    function toYMD(date) { const yyyy = date.getFullYear(); const mm = String(date.getMonth() + 1).padStart(2, '0'); const dd = String(date.getDate()).padStart(2, '0'); return `${yyyy}-${mm}-${dd}`; }
    function workingDaysBetween(startStr, endStr) {
        const s = parseDate(startStr); const e = parseDate(endStr);
        if (!s || !e || e < s) return 0;
        let days = 0; const cur = new Date(s);
        while (cur <= e) { const day = cur.getDay(); if (day !== 5 && day !== 6) { days++; } cur.setDate(cur.getDate() + 1); }
        return days;
    }
    function syncDaysFromDates() {
        if (!els.expectedStart || !els.expectedEnd) return;
        const d = workingDaysBetween(els.expectedStart.value, els.expectedEnd.value);
        if (els.computedDays) els.computedDays.textContent = d.toString();
        if (els.autoDaysChk?.checked && els.daysToComplete) { els.daysToComplete.value = d || 0; }
    }
    function updateDaysEditing() {
        if (!els.daysToComplete) return;
        const auto = !!els.autoDaysChk?.checked;
        els.daysToComplete.readOnly = auto;
        els.daysToComplete.classList.toggle('bg-light', auto);
        els.daysToComplete.classList.toggle('text-muted', auto);
        els.daysToComplete.setAttribute('aria-readonly', auto ? 'true' : 'false');
    }

    // Weight validation
    function validateWeight() {
        if (!els.weightInput) return;
        const entered = parseFloat(els.weightInput.value);
        const weight = isNaN(entered) ? 0 : entered;
        const total = state.otherWeight + weight;
        const remainingBefore = Math.max(state.weightLimit - state.otherWeight, 0);
        if (els.remainingWeightSpan) els.remainingWeightSpan.textContent = fmtPercent(remainingBefore);
        if (els.totalWeightWithCurrent) els.totalWeightWithCurrent.textContent = fmtPercent(Math.min(total, 100));
        if (els.weightProgressBar) {
            const width = Math.max(0, Math.min(total, 100));
            els.weightProgressBar.style.width = width + '%';
            els.weightProgressBar.classList.toggle('bg-danger', total > state.weightLimit);
        }
        if (total > state.weightLimit) {
            const exceededBy = total - state.weightLimit;
            els.weightInput.classList.add('is-invalid');
            els.weightInput.setCustomValidity('إجمالي الأوزان يتجاوز 100%');
            if (els.weightError) els.weightError.textContent = 'خطأ: مجموع الأوزان (' + fmtPercent(total) + '%) يتجاوز الحد الأقصى 100%. مقدار التجاوز: ' + fmtPercent(exceededBy) + '%. ';
            state.weightInvalid = true;
        } else {
            els.weightInput.classList.remove('is-invalid');
            els.weightInput.setCustomValidity('');
            if (els.weightError) els.weightError.textContent = '';
            state.weightInvalid = false;
        }
        applySaveState();
    }

    // Cost warning
    function updateCostWarning() {
        if (!els.costInput || !els.costWarning) return;
        const entered = parseFloat(els.costInput.value) || 0;
        if (!state.hasTotal) {
            els.costWarning.classList.remove('d-none', 'alert-warning');
            els.costWarning.classList.add('alert-info');
            els.costWarning.textContent = 'لم يتم تحديد التكلفة الإجمالية للمشروع. لن يتم التحقق من تجاوز الميزانية. يُفضل تحديدها من تفاصيل المشروع أولاً.';
            if (els.remainingBeforeSpan) els.remainingBeforeSpan.textContent = 'غير محدد';
            return;
        }
        const newTotal = state.existingCost + entered;
        const remaining = Math.max(state.projectTotal - state.existingCost, 0);
        if (els.remainingBeforeSpan) els.remainingBeforeSpan.textContent = fmtNumber(remaining);
        if (newTotal > state.projectTotal) {
            const exceededBy = newTotal - state.projectTotal;
            els.costWarning.classList.remove('d-none', 'alert-info');
            els.costWarning.classList.add('alert-warning');
            els.costWarning.innerHTML = 'تنبيه: مجموع تكاليف المهام بعد إضافة هذه المهمة (' + fmtNumber(newTotal) + ') سيتجاوز تكلفة المشروع (' + fmtNumber(state.projectTotal) + '). مقدار التجاوز: ' + fmtNumber(exceededBy) + '.';
        } else {
            els.costWarning.classList.add('d-none');
            els.costWarning.classList.remove('alert-warning', 'alert-info');
            els.costWarning.textContent = '';
        }
    }

    // Logs helpers
    function parseRows() {
        const rows = Array.from(els.logsTableBody?.querySelectorAll('tr') || []);
        return rows.map(r => {
            const p = r.querySelector('.log-progress');
            const d = r.querySelector('.log-date');
            const n = r.querySelector('.log-notes');
            const progress = parseFloat(p?.value || '');
            return { row: r, progress: isNaN(progress) ? null : progress, date: d?.value || '', notes: n?.value || '' };
        });
    }
    function recomputeFromLogs() {
        const rows = parseRows();
        let total = 0;
        let finishedDate = null;
        let error = '';
        let hasInvalid = false;
        for (const item of rows) {
            const pInput = item.row.querySelector('.log-progress');
            const dInput = item.row.querySelector('.log-date');
            const hasP = item.progress !== null;
            const hasD = !!item.date;
            pInput?.classList.remove('is-invalid');
            dInput?.classList.remove('is-invalid');
            if (!hasP && !hasD && !(item.notes && item.notes.trim().length > 0)) continue;
            if (!hasD) { hasInvalid = true; dInput?.classList.add('is-invalid'); }
            if (!hasP) { hasInvalid = true; pInput?.classList.add('is-invalid'); }
            if (hasP) {
                if (item.progress < 0 || item.progress > 100) { hasInvalid = true; pInput?.classList.add('is-invalid'); }
                else { total += item.progress; if (!finishedDate && hasD && total >= 100) { finishedDate = item.date; } }
            }
        }
        if (total > 100) { hasInvalid = true; error = 'خطأ: مجموع نسب السجل (' + fmtPercent(total) + '%) يتجاوز 100%.'; }
        if (els.logsTotalSpan) els.logsTotalSpan.textContent = fmtPercent(Math.min(total, 100));
        if (els.doneRatioInput) els.doneRatioInput.value = (Math.min(total, 100)).toFixed(2);
        if (els.doneProgressBar) { const width = Math.max(0, Math.min(total, 100)); els.doneProgressBar.style.width = width + '%'; els.doneProgressBar.setAttribute('aria-valuenow', width.toString()); }
        const show = total >= 100;
        if (els.attachmentContainer) {
            els.attachmentContainer.style.display = show ? 'block' : 'none';
            els.attachmentContainer.classList.toggle('border-success', show);
            if (!show && els.attachmentInput) { els.attachmentInput.value = ''; if (els.attachmentInfo) els.attachmentInfo.textContent = ''; }
            if (!show && els.attachmentError) { els.attachmentError.textContent = ''; }
        }
        if (els.actualEndDateInput) { els.actualEndDateInput.value = finishedDate || ''; }
        if (show) {
            const attachOk = state.hasExistingAttachment || (els.attachmentInput && els.attachmentInput.files && els.attachmentInput.files.length > 0);
            if (els.attachmentError) els.attachmentError.textContent = attachOk ? '' : 'المرفق مطلوب عند اكتمال المهمة بنسبة 100%.';
            state.completionInvalid = !attachOk;
        } else { state.completionInvalid = false; }
        if (els.completeTodayBtn) {
            const disabled = total >= 100; els.completeTodayBtn.disabled = disabled; els.completeTodayBtn.classList.toggle('disabled', disabled);
            els.completeTodayBtn.title = disabled ? 'إجمالي السجل 100%، لا يمكن إضافة اكتمال اليوم' : 'إضافة صف بنسبة المتبقي للوصول إلى 100% بتاريخ اليوم';
        }
        state.logsInvalid = hasInvalid; if (els.logsError) els.logsError.textContent = hasInvalid ? (error || 'يرجى إدخال تاريخ ونسبة صحيحة لكل صف.') : '';
        applySaveState();
    }
    function attachRowHandlers(tr) {
        tr.querySelector('.log-progress')?.addEventListener('input', (e) => {
            const t = e.target; const v = parseFloat(t.value);
            if (!isNaN(v)) { if (v < 0) t.value = '0'; if (v > 100) t.value = '100'; }
            recomputeFromLogs();
        });
        tr.querySelector('.log-date')?.addEventListener('input', recomputeFromLogs);
        tr.querySelector('.log-notes')?.addEventListener('input', () => { /* notes do not affect totals */ });
        tr.querySelector('.remove-log')?.addEventListener('click', function () { tr.remove(); recomputeFromLogs(); });
    }
    Array.from(els.logsTableBody?.querySelectorAll('tr') || []).forEach(attachRowHandlers);

    // Add new row
    const addRow = () => {
        const tr = document.createElement('tr');
        tr.innerHTML = '<td><input type="date" name="LogDate[]" class="form-control log-date" /></td>' +
            '<td><input type="number" name="LogProgress[]" class="form-control log-progress" min="0" max="100" step="0.01" /></td>' +
            '<td><input type="text" name="LogNotes[]" class="form-control log-notes" /></td>' +
            '<td class="text-end"><button type="button" class="btn btn-sm btn-outline-danger remove-log">حذف</button></td>';
        els.logsTableBody?.appendChild(tr);
        attachRowHandlers(tr);
        const dateInput = tr.querySelector('.log-date');
        if (dateInput && !dateInput.value) { dateInput.valueAsDate = new Date(); dateInput.dispatchEvent(new Event('input', { bubbles: true })); }
        recomputeFromLogs();
    };

    els.addLogRowBtn?.addEventListener('click', function (e) {
        e.preventDefault(); e.stopPropagation();
        addRow();
    });

    function currentFilledTotal() {
        return parseRows().reduce((sum, item) => { const hasD = !!item.date; const hasP = item.progress !== null; return (hasD && hasP && item.progress >= 0 && item.progress <= 100) ? sum + item.progress : sum; }, 0);
    }
    els.completeTodayBtn?.addEventListener('click', function (e) {
        e.preventDefault(); e.stopPropagation(); if (els.completeTodayBtn.disabled) return;
        const tr = document.createElement('tr');
        const today = new Date(); const yyyy = today.getFullYear(); const mm = String(today.getMonth() + 1).padStart(2, '0'); const dd = String(today.getDate()).padStart(2, '0');
        const totalNow = currentFilledTotal(); let remaining = Math.max(0, 100 - totalNow); remaining = Math.max(0, Math.min(remaining, 100)); const remainingStr = remaining.toFixed(2);
        tr.innerHTML = `<td><input type="date" name="LogDate[]" class="form-control log-date" value="${yyyy}-${mm}-${dd}" /></td>` +
            `<td><input type="number" name="LogProgress[]" class="form-control log-progress" min="0" max="100" step="0.01" value="${remainingStr}" /></td>` +
            '<td><input type="text" name="LogNotes[]" class="form-control log-notes" /></td>' +
            '<td class="text-end"><button type="button" class="btn btn-sm btn-outline-danger remove-log">حذف</button></td>';
        els.logsTableBody?.appendChild(tr); attachRowHandlers(tr); recomputeFromLogs();
    });

    // Default empty dates to today on load
    document.querySelectorAll('.log-date').forEach(function (input) { if (!input.value) { input.valueAsDate = new Date(); } });

    // Attachment change
    els.attachmentInput?.addEventListener('change', function () {
        const f = els.attachmentInput.files?.[0];
        if (els.attachmentInfo) { els.attachmentInfo.textContent = f ? (`${f.name} — ${(f.size / (1024 * 1024)).toFixed(2)} MB`) : ''; }
        if (f && f.size > MAX_FILE_SIZE) { if (els.attachmentError) els.attachmentError.textContent = 'حجم الملف يتجاوز الحد الأقصى 10MB.'; state.completionInvalid = true; }
        else { if (els.attachmentError) els.attachmentError.textContent = ''; }
        recomputeFromLogs();
    });

    // Dates interaction
    function syncEndMin() {
        if (els.expectedStart && els.expectedEnd) {
            if (els.expectedStart.value) {
                // End date must be strictly greater than start date
                const s = parseDate(els.expectedStart.value);
                if (s) {
                    const minEnd = addDays(s, 1);
                    els.expectedEnd.min = toYMD(minEnd);
                    // Auto-correct if invalid
                    const e = parseDate(els.expectedEnd.value);
                    if (e && e < minEnd) {
                        els.expectedEnd.value = toYMD(minEnd);
                    }
                }
            } else {
                els.expectedEnd.removeAttribute('min');
            }
        }
    }
    els.expectedStart?.addEventListener('input', function () { syncEndMin(); syncDaysFromDates(); });
    els.expectedEnd?.addEventListener('input', function () { syncDaysFromDates(); });
    els.autoDaysChk?.addEventListener('change', function () { syncDaysFromDates(); updateDaysEditing(); });

    // Fill helpers
    els.fillWeightBtn?.addEventListener('click', function () { const remainingBefore = Math.max(state.weightLimit - state.otherWeight, 0); if (els.weightInput) { els.weightInput.value = remainingBefore.toFixed(2); validateWeight(); } });
    els.fillCostBtn?.addEventListener('click', function () { if (!state.hasTotal) return; const remaining = Math.max(state.projectTotal - state.existingCost, 0); if (els.costInput) { els.costInput.value = remaining.toFixed(2); updateCostWarning(); } });

    // Clamp weight on blur and validate
    els.weightInput?.addEventListener('blur', function () { const v = parseFloat(els.weightInput.value); if (!isNaN(v)) { els.weightInput.value = Math.max(0, Math.min(v, 100)).toFixed(2); } validateWeight(); });
    els.weightInput?.addEventListener('input', validateWeight);

    // Cost warn on change
    els.costInput?.addEventListener('input', updateCostWarning);

    // Clean empty rows on submit
    form.addEventListener('submit', function () {
        const rows = parseRows(); let removedAny = false;
        for (const item of rows) {
            const pInput = item.row.querySelector('.log-progress');
            const dInput = item.row.querySelector('.log-date');
            const nInput = item.row.querySelector('.log-notes');
            const progressRaw = pInput?.value ?? ''; const dateRaw = dInput?.value ?? ''; const notesRaw = nInput?.value ?? '';
            const isEmpty = (!progressRaw || progressRaw.trim() === '') && (!dateRaw || dateRaw.trim() === '') && (!notesRaw || notesRaw.trim() === '');
            if (isEmpty) { item.row.remove(); removedAny = true; }
        }
        if (removedAny) { recomputeFromLogs(); }
    });

    // Initialize defaults
    if (els.expectedStart && !els.expectedStart.value) els.expectedStart.valueAsDate = new Date();
    if (els.expectedEnd && !els.expectedEnd.value) els.expectedEnd.valueAsDate = new Date();
    syncEndMin();
    validateWeight();
    updateCostWarning();
    syncDaysFromDates();
    updateDaysEditing();
    recomputeFromLogs();
    showStep(0);
})();
