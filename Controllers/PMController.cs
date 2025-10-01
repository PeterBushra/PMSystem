using Jobick.Models;
using Jobick.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobick.Controllers;

/// <summary>
///  Projects Controller
/// </summary>
public class PMController : Controller
{
    static List<ProjectViewModel> projects = new List<ProjectViewModel>
{
    new ProjectViewModel
    {
        Id = 1,
        Name = "Project A",
        NameAr = "مشروع أ",
        Description = "Description A",
        DescriptionAr = "وصف أ",
        ResponsibleForImplementing = "Team X",
        SystemOwner = "Owner Y",
        ProjectGoal = "Goal 1",
        StartDate = new DateTime(2025, 1, 1),
        EndDate = new DateTime(2025, 12, 31),
        TotalCost = 10000
    },
    new ProjectViewModel
    {
        Id = 2,
        Name = "Project B",
        NameAr = "مشروع ب",
        Description = "Description B",
        DescriptionAr = "وصف ب",
        ResponsibleForImplementing = "Team Z",
        SystemOwner = "Owner W",
        ProjectGoal = "Goal 2",
        StartDate = new DateTime(2025, 2, 1),
        EndDate = new DateTime(2025, 11, 30),
        TotalCost = 20000
    }
};


    // GET: Create Project
    public IActionResult CreateProject()
    {
        return View();
    }

    // POST: Create Project
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Project project)
    {
        //if (ModelState.IsValid)
        //{
        //    _context.Projects.Add(project);
        //    _context.SaveChanges();
        //    return RedirectToAction(nameof(Index));
        //}
        return View(project);
    }

    public IActionResult Projects()
    {
        
        return View(projects);
    }
}
