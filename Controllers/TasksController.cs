using Jobick.Services;
using Jobick.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobick.Controllers;

/// <summary>
/// Handles creation, editing, deletion and attachment download for tasks.
/// Uses services to interact with EF Core context and keeps controller slim.
/// </summary>
public class TasksController(ITaskService _taskService, IProjectService _projectService) : Controller
{
    // Static, readonly map for common content types to file extensions used during downloads.
    private static readonly IReadOnlyDictionary<string, string> _contentTypeToExtension = new Dictionary<string, string>
    {
        { "application/pdf", ".pdf" },
        { "image/jpeg", ".jpg" },
        { "image/png", ".png" },
        { "application/msword", ".doc" },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
        { "application/vnd.ms-excel", ".xls" },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx" }
    };

    /// <summary>
    /// Returns the create form for a task with defaults and cost context in ViewBag.
    /// </summary>
    public async Task<IActionResult> CreateTask(int projectId)
    {
        var model = new Models.Task
        {
            ProjectId = projectId,
            ExpectedStartDate = DateTime.Now,
            ExpectedEndDate = DateTime.Now
        };

        var project = await _projectService.GetProjectAsync(projectId);
        var tasks = await _taskService.GetTaskListAsync();

        decimal existingCost = tasks.Where(t => t.ProjectId == projectId).Sum(t => t.Cost ?? 0);
        decimal existingWeight = tasks.Where(t => t.ProjectId == projectId).Sum(t => t.Weight ?? 0);

        ViewBag.ProjectTotalCost   = project?.TotalCost;                 // keep nullable
        ViewBag.HasProjectTotal    = project?.TotalCost.HasValue == true;
        ViewBag.ExistingTasksCost  = existingCost;
        // Sum of other tasks' weight (for Add it's all current tasks)
        ViewBag.OtherTasksWeight   = existingWeight;

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    /// <summary>
    /// Handles create task submission, including attachment requirement when DoneRatio is 100%.
    /// Converts DoneRatio from percentage (0..100) to fraction (0..1) before persistence.
    /// Also validates that the sum of weights across project tasks does not exceed 100.
    /// </summary>
    public async Task<IActionResult> PostTask(Models.Task model)
    {
        // Get the user ID from claims and parse to int
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int userId = 0;
        if (!string.IsNullOrEmpty(userIdClaim))
            int.TryParse(userIdClaim, out userId);

        // Remove validation for properties not posted in the form
        ModelState.Remove(nameof(Models.Task.Project));
        ModelState.Remove(nameof(Models.Task.CreatedByNavigation));
        ModelState.Remove(nameof(Models.Task.ManyDaysToComplete));
        ModelState.Remove(nameof(Models.Task.DefinationOfDone));
        ModelState.Remove(nameof(Models.Task.DoneRatio));
        ModelState.Remove(nameof(Models.Task.DepartmentResponsible));
        ModelState.Remove(nameof(Models.Task.ActualEndDate));
        ModelState.Remove(nameof(Models.Task.StageNameAr));
        ModelState.Remove(nameof(Models.Task.TaskAr));

        model.TaskAr = model.Task1!;
        model.StageNameAr = model.StageName!;

        // Handle Attachment
        var file = Request.Form.Files["Attachment"];
        if (model.DoneRatio == 100.0m) // 100% (already converted to fraction)
        {
            if (file == null || file.Length == 0)
            {
                // Repopulate ViewBag context for the UI before returning
                await PopulateCreateViewBagsAsync(model.ProjectId);
                ModelState.AddModelError("Attachment", "يرجى إرفاق ملف عند اكتمال المهمة.");
                return View("CreateTask", model);
            }
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                model.AttachmentData = ms.ToArray();
                model.AttachmentFileName = file.FileName.GetHashCode();
                model.AttachmentContentType = file.ContentType;
            }
        }
        else
        {
            model.AttachmentData = null;
            model.AttachmentFileName = null;
            model.AttachmentContentType = null;
        }

        if (!ModelState.IsValid)
        {
            // Repopulate ViewBag context for the UI before returning
            await PopulateCreateViewBagsAsync(model.ProjectId);
            return View("CreateTask", model);
        }

        // Convert DoneRatio from percentage to fraction and clamp to [0,100] then [0,1]
        model.DoneRatio = ClampToFraction(model.DoneRatio);

        // Validate total weight
        var tasks = await _taskService.GetTaskListAsync();
        var projectTasks = tasks.Where(t => t.ProjectId == model.ProjectId);
        decimal existingWeight = projectTasks.Sum(t => t.Weight ?? 0);
        decimal totalWeight = existingWeight + (model.Weight ?? 0);

        if (totalWeight > 100)
        {
            // Repopulate ViewBag context for the UI before returning
            await PopulateCreateViewBagsAsync(model.ProjectId);
            ModelState.AddModelError("Weight", "مجموع الأوزان لجميع المهام في المشروع يجب ألا يتجاوز 100%");
            return View("CreateTask", model);
        }

        if (model.Id == 0)
        {
            model.CreatedBy = userId;       
            model.CreatedDate = DateTime.Now;
            await _taskService.AddTaskAsync(model);
        }
        else
        {
            model.CreatedDate = DateTime.Now;
            await _taskService.UpdateTaskAsync(model);
        }

        return RedirectToAction("ProjectDetails", "Projects", new { id = model.ProjectId });
    }

    /// <summary>
    /// Loads the edit form for a task and prepares cost context for client-side warnings.
    /// Converts DoneRatio from stored fraction to percentage for display.
    /// </summary>
    public async Task<IActionResult> EditTask(int id)
    {
        var task = await _taskService.GetTaskAsync(id);
        if (task == null)
            return NotFound();

        if (task.DoneRatio.HasValue)
            task.DoneRatio = task.DoneRatio.Value * 100m;

        var project = await _projectService.GetProjectAsync(task.ProjectId);
        var tasks = await _taskService.GetTaskListAsync();

        decimal existingCostExcludingCurrent = tasks
            .Where(t => t.ProjectId == task.ProjectId && t.Id != task.Id)
            .Sum(t => t.Cost ?? 0);

        // Other tasks' weights (exclude current)
        decimal otherTasksWeight = tasks
            .Where(t => t.ProjectId == task.ProjectId && t.Id != task.Id)
            .Sum(t => t.Weight ?? 0);

        ViewBag.ProjectTotalCost   = project?.TotalCost;                 // keep nullable
        ViewBag.HasProjectTotal    = project?.TotalCost.HasValue == true;
        ViewBag.ExistingTasksCost  = existingCostExcludingCurrent;
        ViewBag.OtherTasksWeight   = otherTasksWeight;

        ViewData["Title"] = "Edit Task";
        return View("CreateTask", task);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    /// <summary>
    /// Handles task update submission with the same validations as creation.
    /// </summary>
    public async Task<IActionResult> EditTask(int id, Models.Task model)
    {
        if (id != model.Id)
            return BadRequest();

        decimal weights = _taskService.GetTotalTasksWeights(model.ProjectId);
        weights -= _taskService.GetTaskWeight(model.Id); // weights of other tasks

        // Remove validation for properties not posted in the form
        ModelState.Remove(nameof(Models.Task.Project));
        ModelState.Remove(nameof(Models.Task.CreatedByNavigation));
        ModelState.Remove(nameof(Models.Task.ManyDaysToComplete));
        ModelState.Remove(nameof(Models.Task.DefinationOfDone));
        ModelState.Remove(nameof(Models.Task.DoneRatio));
        ModelState.Remove(nameof(Models.Task.DepartmentResponsible));
        ModelState.Remove(nameof(Models.Task.StageNameAr));
        ModelState.Remove(nameof(Models.Task.TaskAr));

        model.TaskAr = model.Task1!;
        model.StageNameAr = model.StageName!;

        // Handle Attachment
        var file = Request.Form.Files["Attachment"];
        if (model.DoneRatio == 100.0m) // 100% (already converted to fraction)
        {
            if ((file == null || file.Length == 0) && (model.AttachmentData == null || model.AttachmentData.Length == 0))
            {
                await PopulateEditViewBagsAsync(model);
                ModelState.AddModelError("Attachment", "يرجى إرفاق ملف عند اكتمال المهمة.");
                return View("CreateTask", model);
            }
            if (file != null && file.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    model.AttachmentData = ms.ToArray();
                    model.AttachmentFileName = file.FileName.GetHashCode();
                    model.AttachmentContentType = file.ContentType;
                }
            }
            // else: keep existing attachment if present
        }
        else
        {
            model.AttachmentData = null;
            model.AttachmentFileName = null;
            model.AttachmentContentType = null;
        }

        if (!ModelState.IsValid)
        {
            await PopulateEditViewBagsAsync(model, weights);
            return View("CreateTask", model);
        }

        // Convert DoneRatio from percentage to fraction and clamp
        model.DoneRatio = ClampToFraction(model.DoneRatio);

        // Validate total weight
        decimal totalWeight = weights + (model.Weight ?? 0);

        if (totalWeight > 100)
        {
            await PopulateEditViewBagsAsync(model, weights);
            ModelState.AddModelError("Weight", "مجموع الأوزان لجميع المهام في المشروع يجب ألا يتجاوز 100%");
            return View("CreateTask", model);
        }

        // Copy the posted values into the tracked entity
        await _taskService.UpdateTaskAsync(model);
        return RedirectToAction("ProjectDetails", "Projects", new { id = model.ProjectId });
    }

    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Shows delete confirmation for a task.
    /// </summary>
    public async Task<IActionResult> DeleteTask(int id, int projectId)
    {
        var task = await _taskService.GetTaskAsync(id);
        if (task == null)
            return NotFound();
        return View(task);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    /// <summary>
    /// Executes deletion of a task.
    /// </summary>
    public async Task<IActionResult> PostDeleteTask(int id, int projectId)
    {
        await _taskService.DeleteTaskAsync(id);
        return RedirectToAction("ProjectDetails", "Projects", new { id = projectId });
    }

    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Allows users to download the stored attachment for the given task id.
    /// Chooses a best-effort file name based on the task id and content type.
    /// </summary>
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var task = await _taskService.GetTaskAsync(id);
        if (task?.AttachmentData == null || task.AttachmentData.Length == 0)
            return NotFound();

        // Try to get the extension from the content type
        string extension = "";
        if (!string.IsNullOrEmpty(task.AttachmentContentType))
        {
            if (_contentTypeToExtension.TryGetValue(task.AttachmentContentType, out var ext))
                extension = ext;
        }

        // If you stored the original file name, use its extension
        string fileName = "Attachment_" + id + extension;
        return File(task.AttachmentData, task.AttachmentContentType ?? "application/octet-stream", fileName);
    }

    /// <summary>
    /// Clamps a percentage value in [0,100] and returns a fraction in [0,1].
    /// Accepts null and returns 0.
    /// </summary>
    private static decimal ClampToFraction(decimal? percentage)
    {
        if (!percentage.HasValue) return 0m;
        var p = percentage.Value;
        if (p > 100m) p = 100m;
        if (p < 0m) p = 0m;
        return p / 100m;
    }

    // Helper: populate ViewBags for Create (POST error paths)
    private async Task PopulateCreateViewBagsAsync(int projectId)
    {
        var project = await _projectService.GetProjectAsync(projectId);
        var tasks = await _taskService.GetTaskListAsync();

        decimal existingCost = tasks.Where(t => t.ProjectId == projectId).Sum(t => t.Cost ?? 0);
        decimal existingWeight = tasks.Where(t => t.ProjectId == projectId).Sum(t => t.Weight ?? 0);

        ViewBag.ProjectTotalCost  = project?.TotalCost;
        ViewBag.HasProjectTotal   = project?.TotalCost.HasValue == true;
        ViewBag.ExistingTasksCost = existingCost;
        ViewBag.OtherTasksWeight  = existingWeight;
    }

    // Helper: populate ViewBags for Edit (POST error paths)
    private async Task PopulateEditViewBagsAsync(Models.Task model, decimal? otherWeightsOverride = null)
    {
        var project = await _projectService.GetProjectAsync(model.ProjectId);
        var tasks = await _taskService.GetTaskListAsync();

        decimal existingCostExcludingCurrent = tasks
            .Where(t => t.ProjectId == model.ProjectId && t.Id != model.Id)
            .Sum(t => t.Cost ?? 0);

        decimal otherTasksWeight = otherWeightsOverride ?? tasks
            .Where(t => t.ProjectId == model.ProjectId && t.Id != model.Id)
            .Sum(t => t.Weight ?? 0);

        ViewBag.ProjectTotalCost  = project?.TotalCost;
        ViewBag.HasProjectTotal   = project?.TotalCost.HasValue == true;
        ViewBag.ExistingTasksCost = existingCostExcludingCurrent;
        ViewBag.OtherTasksWeight  = otherTasksWeight;
    }
}
