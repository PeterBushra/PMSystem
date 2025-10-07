using ClosedXML.Excel;
using Jobick.Models;
using Jobick.Services.Interfaces;
using Jobick.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Security.Claims;

namespace Jobick.Controllers;

[Authorize(Roles = "Admin")]
public class ImportController(ITaskService taskService, IProjectService projectService) : Controller
{
    private static readonly string[] ExpectedHeaders = new[]
    {
        "المراحل",
        "المهام",
        "القسم المنفذ",
        "الإدارة المسؤولة",
        "المخرجات (DOD)",
        "المدة المطلوبة للإنتهاء (أيام عمل)",
        "تاريخ البدء",
        "تاريخ الانتهاء",
        "النسبة الفعلية",
        "النسبة المخطط",
        "الصرف المخطط"
    };

    [HttpPost]
    public IActionResult UploadExcel(int projectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "يرجى اختيار ملف إكسل صالح." });

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null)
            return BadRequest(new { message = "لم يت    م العثور على ورقة عمل صالحة في الملف." });

        // Read header row (row 1)
        var headers = new List<string>();
        foreach (var cell in ws.Row(1).CellsUsed())
        {
            headers.Add((cell.GetString() ?? string.Empty).Trim());
        }

        // Validate presence and order of expected headers
        var normalizedHeaders = headers.Select(h => h.Replace("\u00A0", " ").Trim()).ToList();
        var expected = ExpectedHeaders.Select(h => h.Trim()).ToList();
        if (normalizedHeaders.Count < expected.Count || !expected.SequenceEqual(normalizedHeaders.Take(expected.Count)))
        {
            return BadRequest(new { message = "تنسيق ملف الإكسل غير صحيح أو الأعمدة مفقودة/غير مرتبة. يرجى الالتزام بالعناوين المحددة تمامًا." });
        }

        // Parse rows starting from row 2
        var rows = new List<TaskImportRow>();
        int r = 2;
        while (true)
        {
            var taskName = ws.Cell(r, 2).GetString(); // "المهام"
            var stage = ws.Cell(r, 1).GetString();
            var isRowEmpty = string.IsNullOrWhiteSpace(taskName) && string.IsNullOrWhiteSpace(stage);
            if (isRowEmpty) break; // stop at first empty row

            var  row = new TaskImportRow
            {
                StageName = stage?.Trim(),
                TaskName = taskName?.Trim(),
                ImplementorDepartment = ws.Cell(r, 3).GetString()?.Trim(),
                DepartmentResponsible = ws.Cell(r, 4).GetString()?.Trim(),
                DefinitionOfDone = ws.Cell(r, 5).GetString()?.Trim(),
                ManyDaysToCompleteRaw = ws.Cell(r, 6).GetString()?.Trim(),
                ExpectedStartDateRaw = ws.Cell(r, 7).GetString()?.Trim(),
                ExpectedEndDateRaw = ws.Cell(r, 8).GetString()?.Trim(),
                DoneRatioRaw = ws.Cell(r, 9).GetString()?.Trim(),
                PlannedPercentRaw = ws.Cell(r, 10).GetString()?.Trim(),
                PlannedCostRaw = ws.Cell(r, 11).GetString()?.Trim()
            };

            // Try parse numbers and dates with safe fallbacks
            var errors = new List<string>();

            if (int.TryParse(row.ManyDaysToCompleteRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days))
                row.ManyDaysToComplete = days;
            else if (double.TryParse(row.ManyDaysToCompleteRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var daysD))
                row.ManyDaysToComplete = (int)Math.Round(daysD);
            else if (!string.IsNullOrWhiteSpace(row.ManyDaysToCompleteRaw))
                errors.Add("قيمة المدة المطلوبة غير صالحة");

            // Dates: try native, or using Date serials
            row.ExpectedStartDate = TryGetCellDate(ws.Cell(r, 7), row.ExpectedStartDateRaw, errors, "تاريخ البدء");
            row.ExpectedEndDate = TryGetCellDate(ws.Cell(r, 8), row.ExpectedEndDateRaw, errors, "تاريخ الانتهاء");

            // Percentages and cost
            if (TryParseDecimalPercent(row.DoneRatioRaw, out var done)) row.DoneRatio = done;
            else if (!string.IsNullOrWhiteSpace(row.DoneRatioRaw)) errors.Add("قيمة النسبة الفعلية غير صالحة");

            if (TryParseDecimalPercent(row.PlannedPercentRaw, out var planned)) row.PlannedPercent = planned * 100;
            else if (!string.IsNullOrWhiteSpace(row.PlannedPercentRaw)) errors.Add("قيمة النسبة المخطط غير صالحة");

            if (TryParseDecimalNumber(row.PlannedCostRaw, out var cost)) row.PlannedCost = cost;
            else if (!string.IsNullOrWhiteSpace(row.PlannedCostRaw)) errors.Add("قيمة الصرف المخطط غير صالحة");

            row.Errors = errors;
            rows.Add(row);
            r++;
        }

        var preview = new TaskImportPreviewVM
        {
            ProjectId = projectId,
            Rows = rows
        };

        // Render partial view to string and return as JSON
        var html = this.RenderPartialViewToString("_TasksImportPreview", preview);
        return Json(new { html, hasErrors = rows.Any(x => x.Errors.Count > 0) });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async System.Threading.Tasks.Task<IActionResult> ConfirmImport([FromBody] TaskImportConfirmRequest request)
    {
        if (request == null || request.ProjectId <= 0 || request.Rows == null || request.Rows.Count == 0)
            return BadRequest(new { message = "بيانات الاستيراد غير صالحة." });

        // Ensure project exists
        var project = await projectService.GetProjectAsync(request.ProjectId);
        if (project == null)
            return NotFound(new { message = "المشروع غير موجود." });

        // Validate: no row at 100% (attachments required in TasksController when 100%)
        var completedRows = request.Rows
            .Select((r, i) => new { r, i })
            .Where(x => (x.r.DoneRatio ?? 0m) >= 100m)
            .ToList();


        // Validate total weights: existing + new <= 100 (same rule as TasksController)
        decimal existingWeight = taskService.GetTotalTasksWeights(request.ProjectId);
        decimal importWeightSum = request.Rows.Sum(r => (r.PlannedPercent ?? 0m));

        if (existingWeight + importWeightSum > 100m)
        {
            var over = existingWeight + importWeightSum - 100m;
            return BadRequest(new { message = $"مجموع الأوزان لجميع المهام يجب ألا يتجاوز 100%. التجاوز: {over:N2}%." });
        }

        // current user id if available
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdClaim, out var userId);

        // Map and save
        foreach (var row in request.Rows)
        {
            var model = new Jobick.Models.Task
            {
                ProjectId = request.ProjectId,
                StageName = row.StageName?.Trim(),
                Task1 = row.TaskName?.Trim(),
                ImplementorDepartment = row.ImplementorDepartment?.Trim(),
                DepartmentResponsible = row.DepartmentResponsible?.Trim(),
                DefinationOfDone = row.DefinitionOfDone?.Trim(),
                ManyDaysToComplete = row.ManyDaysToComplete ?? 0,
                ExpectedStartDate = (row.ExpectedStartDate ?? DateTime.Now).Date,
                ExpectedEndDate = (row.ExpectedEndDate ?? DateTime.Now).Date,
                DoneRatio = row.DoneRatio,
                Weight = row.PlannedPercent, // still stored as percent in your app
                Cost = row.PlannedCost,
                CreatedDate = DateTime.Now,
                CreatedBy = userId
            };
            // Arabic mirrors
            model.StageNameAr = model.StageName!;
            model.TaskAr = model.Task1!;

            await taskService.AddTaskAsync(model);
        }

        return Ok(new { message = "تم استيراد المهام بنجاح." });
    }

    private static DateTime? TryGetCellDate(IXLCell cell, string? raw, List<string> errors, string fieldLabel)
    {
        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var dt)) return dt.Date;
        if (cell.DataType == XLDataType.Number)
        {
            try
            {
                var dtFromSerial = DateTime.FromOADate(cell.GetDouble());
                return dtFromSerial.Date;
            }
            catch { }
        }
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt2)) return dt2.Date;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt3)) return dt3.Date;
            errors.Add($"قيمة {fieldLabel} غير صالحة");
        }
        return null;
    }

    private static bool TryParseDecimalPercent(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Replace("%", string.Empty).Trim();
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            value = d;
            return true;
        }
        return false;
    }

    private static bool TryParseDecimalNumber(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        // accept comma as thousands separator for Arabic locales
        var normalized = raw.Replace(",", "").Trim();
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            value = d;
            return true;
        }
        return false;
    }
}
