using Jobick.Models;
using Jobick.Services;
using Jobick.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobick.Controllers;
public class TasksController (ProjectService _pservice) : Controller
{
    public IActionResult CreateTask(int projectId)
    {
        var model = new Models.Task { ProjectId = projectId };
        return View(model); 
    }

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
