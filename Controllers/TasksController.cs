using Jobick.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobick.Controllers;
public class TasksController(ProjectService _pservice) : Controller
{
    [Authorize(Roles = "Admin")]

    public IActionResult CreateTask(int projectId)
    {
        var model = new Models.Task { ProjectId = projectId, ExpectedStartDate=DateTime.Now, ExpectedEndDate=DateTime.Now };
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult PostTask(Models.Task model)
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

        var project = _pservice.GetProjectList()
          .FirstOrDefault(p => p.Id == model.ProjectId);

        if (model.Id == 0)
        {
            model.CreatedDate = DateTime.Now;
            project?.Tasks.Add(model);
        }
        else
        {
            var taskremove = project?.Tasks.FirstOrDefault(t => t.Id == model.Id);
            project?.Tasks.Remove(taskremove);
            model.CreatedDate = DateTime.Now;
            project?.Tasks.Add(model);
        }


        // Redirect to ProjectDetails using the ProjectId
        return RedirectToAction("ProjectDetails", "Projects", new { id = model.ProjectId });
    }
}
