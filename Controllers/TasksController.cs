using Jobick.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Jobick.Controllers;
public class TasksController( TaskService _tservice) : Controller
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
        // remove validation for properties not posted
        ModelState.Remove("ProjectId");
        ModelState.Remove("CreatedDate");
        ModelState.Remove("CreatedBy");
        ModelState.Remove("Project");
        ModelState.Remove("CreatedByNavigation");
        ModelState.Remove("ActualEndDate");
        ModelState.Remove("ManyDaysToComplete");
        ModelState.Remove("DefinationOfDone");

        if (!ModelState.IsValid)
            return View("CreateTask", model);

        if (model.Id == 0)
        {
            model.CreatedDate = DateTime.Now;
            await _tservice.AddTaskAsync(model);
        }
        else
        {
            model.CreatedDate = DateTime.Now;
            await _tservice.UpdateTaskAsync(model);
        }

        // Redirect to ProjectDetails using the ProjectId
        return RedirectToAction("ProjectDetails", "Projects", new { id = model.ProjectId });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> EditTask(int id)
    {
        var task = await _tservice.GetTaskAsync(id);
        if (task == null)
            return NotFound();
        return View(task);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> EditTask(Models.Task model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _tservice.UpdateTaskAsync(model);
        return RedirectToAction("ProjectDetails", "Projects", new { id = model.ProjectId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeleteTask(int id, int projectId)
    {
        await _tservice.DeleteTaskAsync(id);
        return RedirectToAction("ProjectDetails", "Projects", new { id = projectId });
    }
}
