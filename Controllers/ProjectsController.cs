using Jobick.Models;
using Jobick.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobick.Controllers;

[Authorize]
public class ProjectsController(ProjectService _pservice) : Controller
{
    public IActionResult Index()
    {
        var projects = _pservice.GetProjectList();
        return View(projects);
    }


    public IActionResult CreateProject()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PostProject(Project project)
    {
        ModelState.Remove(nameof(Project.Id));
        ModelState.Remove(nameof(Project.CreatedDate));
        ModelState.Remove(nameof(Project.CreatedBy));
        ModelState.Remove(nameof(Project.CreatedByNavigation));
        ModelState.Remove(nameof(Project.Tasks));

        if (ModelState.IsValid)
        {
            project.CreatedDate = DateTime.Now;
            _pservice.AddProject(project);
            return RedirectToAction(nameof(Index));
        }

        return View("CreateProject", project);
    }


    public IActionResult ProjectDetails(int id)
    {
        var project = _pservice.GetProjectList()
            .FirstOrDefault(p => p.Id == id);

        if (project == null)
            return NotFound();

        return View(project);
    }
}
