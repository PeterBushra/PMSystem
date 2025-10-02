using Jobick.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Add this for IFormFile
using System.Threading.Tasks;
using System.IO;

namespace Jobick.Controllers;
public class TasksController(TaskService _tservice) : Controller
{
    public IActionResult CreateTask(int projectId)
    {
        var model = new Models.Task { ProjectId = projectId, ExpectedStartDate = DateTime.Now, ExpectedEndDate = DateTime.Now };
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
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
            return View("CreateTask", model);

        // Convert DoneRatio from percentage to fraction
        if (model.DoneRatio > 100) model.DoneRatio = 100;
        if (model.DoneRatio < 0) model.DoneRatio = 0;
        model.DoneRatio = model.DoneRatio / 100m;

        if (model.Id == 0)
        {
            model.CreatedBy = userId;       
            model.CreatedDate = DateTime.Now;
            await _tservice.AddTaskAsync(model);
        }
        else
        {
            model.CreatedDate = DateTime.Now;
            await _tservice.UpdateTaskAsync(model);
        }

        return RedirectToAction("ProjectDetails", "Projects", new { id = model.ProjectId });
    }

    public async Task<IActionResult> EditTask(int id)
    {
        var task = await _tservice.GetTaskAsync(id);
        if (task == null)
            return NotFound();

        // Convert DoneRatio from fraction to percentage for the form
        if (task.DoneRatio.HasValue)
            task.DoneRatio = task.DoneRatio.Value * 100m;

        ViewData["Title"] = "Edit Task";
        return View("CreateTask", task);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTask(int id, Models.Task model)
    {
        if (id != model.Id)
            return BadRequest();

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
            return View("CreateTask", model);

        // Convert DoneRatio from percentage to fraction
        if (model.DoneRatio > 100) model.DoneRatio = 100;
        if (model.DoneRatio < 0) model.DoneRatio = 0;
        model.DoneRatio = model.DoneRatio / 100m;

        await _tservice.UpdateTaskAsync(model);
        return RedirectToAction("ProjectDetails", "Projects", new { id = model.ProjectId });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTask(int id, int projectId)
    {
        var task = await _tservice.GetTaskAsync(id);
        if (task == null)
            return NotFound();
        return View(task);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostDeleteTask(int id, int projectId)
    {
        await _tservice.DeleteTaskAsync(id);
        return RedirectToAction("ProjectDetails", "Projects", new { id = projectId });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var task = await _tservice.GetTaskAsync(id);
        if (task?.AttachmentData == null || task.AttachmentData.Length == 0)
            return NotFound();

        // Try to get the extension from the content type
        string extension = "";
        if (!string.IsNullOrEmpty(task.AttachmentContentType))
        {
            // Simple mapping for common types
            var map = new Dictionary<string, string>
            {
                { "application/pdf", ".pdf" },
                { "image/jpeg", ".jpg" },
                { "image/png", ".png" },
                { "application/msword", ".doc" },
                { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
                { "application/vnd.ms-excel", ".xls" },
                { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx" }
            };
            if (map.TryGetValue(task.AttachmentContentType, out var ext))
                extension = ext;
        }

        // If you stored the original file name, use its extension
        string fileName = "Attachment_" + id + extension;
        return File(task.AttachmentData, task.AttachmentContentType ?? "application/octet-stream", fileName);
    }
}
