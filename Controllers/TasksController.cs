using Jobick.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Jobick.Controllers;
public class TasksController(TaskService _tservice) : Controller
{
    [Authorize(Roles = "Admin")]
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

    [Authorize(Roles = "Admin")]
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
}
