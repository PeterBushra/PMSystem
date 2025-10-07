using Jobick.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;
using System.Text;

namespace Jobick.Controllers;

/// <summary>
/// Handles creation, editing, deletion and attachment download for tasks.
/// Uses services to interact with EF Core context and keeps controller slim.
/// </summary>
public class TasksController(ITaskService _taskService, IProjectService _projectService, IWebHostEnvironment _env, IAttachmentService _attachmentService) : Controller
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

    private string GetAttachmentsRoot()
    {
        // Create under content root: <project>/Attachments
        var root = Path.Combine(_env.ContentRootPath, "Attachments");
        if (!Directory.Exists(root))
        {
            Directory.CreateDirectory(root);
        }
        return root;
    }

    private static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        // Take only the file name part and replace invalid chars
        var name = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        // collapse spaces
        var sanitized = sb.ToString().Trim();
        if (sanitized.Length == 0)
            sanitized = "file";

        // guard against extremely long names
        return sanitized.Length > 180 ? sanitized[^180..] : sanitized; // keep last 180 chars to preserve extension
    }

    private async System.Threading.Tasks.Task<string> SaveAttachmentAsync(IFormFile file)
    {
        // Delegate to the shared service
        return await _attachmentService.SaveAsync(file);
    }

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

        decimal existingCost = _taskService.GetTotalTasksCost(projectId);
        decimal existingWeight = _taskService.GetTotalTasksWeights(projectId);

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
    /// Expects DoneRatio from the form as a percentage (0..100) and converts it to fraction (0..1) before persistence.
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
        if (model.DoneRatio == 100.0m) // 100% (from form as percentage)
        {
            if (file == null || file.Length == 0)
            {
                await PopulateCreateViewBagsAsync(model.ProjectId);
                ModelState.AddModelError("Attachment", "يرجى إرفاق ملف عند اكتمال المهمة.");
                return View("CreateTask", model);
            }
            // Save to disk and store relative path
            model.AttachmentFilePath = await SaveAttachmentAsync(file);
            // Clear legacy fields (no DB binary storage going forward)
            model.AttachmentFileName = null;
        }
        else
        {
            // Detach any previous association; do not delete any file
            model.AttachmentFilePath = null;
            model.AttachmentFileName = null;
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
        decimal existingWeight = _taskService.GetTotalTasksWeights(model.ProjectId);
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

        decimal existingCostExcludingCurrent = _taskService.GetTotalTasksCostExcluding(task.ProjectId, task.Id);
        // Other tasks' weights (exclude current)
        decimal otherTasksWeight = _taskService.GetTotalTasksWeights(task.ProjectId) - _taskService.GetTaskWeight(task.Id);

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
        if (model.DoneRatio == 100.0m) // 100% (from form as percentage)
        {
            if ((file == null || file.Length == 0) && string.IsNullOrWhiteSpace(model.AttachmentFilePath))
            {
                await PopulateEditViewBagsAsync(model);
                ModelState.AddModelError("Attachment", "يرجى إرفاق ملف عند اكتمال المهمة.");
                return View("CreateTask", model);
            }
            if (file != null && file.Length > 0)
            {
                // Save a new file; do not delete existing file
                model.AttachmentFilePath = await SaveAttachmentAsync(file);
            }
            // Clear legacy fields
            model.AttachmentFileName = null;
        }
        else
        {
            // Disassociate file when not complete (do not delete physical file)
            model.AttachmentFilePath = null;
            model.AttachmentFileName = null;
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
    /// Chooses a best-effort file name based on the task id and stored path.
    /// </summary>
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var task = await _taskService.GetTaskAsync(id);
        var relPath = task?.AttachmentFilePath;
        if (!_attachmentService.TryGetDownloadInfo(relPath, id, out var info) || info == null)
            return NotFound();

        var stream = new System.IO.FileStream(info.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, info.ContentType, info.DownloadName);
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

        decimal existingCost = _taskService.GetTotalTasksCost(projectId);
        decimal existingWeight = _taskService.GetTotalTasksWeights(projectId);

        ViewBag.ProjectTotalCost  = project?.TotalCost;
        ViewBag.HasProjectTotal   = project?.TotalCost.HasValue == true;
        ViewBag.ExistingTasksCost = existingCost;
        ViewBag.OtherTasksWeight  = existingWeight;
    }

    // Helper: populate ViewBags for Edit (POST error paths)
    private async Task PopulateEditViewBagsAsync(Models.Task model, decimal? otherWeightsOverride = null)
    {
        var project = await _projectService.GetProjectAsync(model.ProjectId);

        decimal existingCostExcludingCurrent = _taskService.GetTotalTasksCostExcluding(model.ProjectId, model.Id);
        decimal otherTasksWeight = otherWeightsOverride ?? (_taskService.GetTotalTasksWeights(model.ProjectId) - _taskService.GetTaskWeight(model.Id));

        ViewBag.ProjectTotalCost  = project?.TotalCost;
        ViewBag.HasProjectTotal   = project?.TotalCost.HasValue == true;
        ViewBag.ExistingTasksCost = existingCostExcludingCurrent;
        ViewBag.OtherTasksWeight  = otherTasksWeight;
    }
}
