using Jobick.Models;
using Jobick.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobick.Controllers;

/// <summary>
///  Projects Controller
/// </summary>
public class PMController (ProjectService _pservice) : Controller
{
    // GET: Create Project
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
            return RedirectToAction(nameof(Projects)); 
        }

        return View("CreateProject",project);
    }

    public IActionResult Projects()
    {
        var projects = _pservice.GetProjectList();
        return View(projects);
    }

    public IActionResult ProjectDetails()
    {
        var projects = _pservice.GetProjectList();
        return View(projects);
    }
}
