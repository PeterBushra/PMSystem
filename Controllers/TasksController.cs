using Jobick.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jobick.Controllers;
public class TasksController (ProjectService _pservice) : Controller
{
    public IActionResult CreateTask()
    {
        //// Always pass a new Task instance with ProjectId
        var task = new Jobick.Models.Task
        {
            ProjectId = 1,
            StageName = "",
            Task1 = "",
            ImplementorDepartment = "",
            ExpectedStartDate = DateTime.Today,
            ExpectedEndDate = DateTime.Today.AddDays(1),
            DoneRatio = 0
        };
        return View("CreateTask", task);
    }

    [HttpPost]
    public IActionResult PostTask(Models.Task model)
    {
        if (!ModelState.IsValid)
            return View("TaskDetails", model);

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
