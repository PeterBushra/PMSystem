using Jobick.Models;
using Jobick.Services;
using Jobick.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Jobick.Controllers;

[Authorize]
public class ProjectsController(ProjectService _pservice, UserService _userservice) : Controller
{
    public async Task<IActionResult> Index()
    {
        var projects = await  _pservice.GetProjectListAsync();
        return View(projects);
    }

    [Authorize(Roles = "Admin")]

    public IActionResult CreateProject()
    {
        // Get the user ID from claims and parse to int
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int userId = 0;
        if (!string.IsNullOrEmpty(userIdClaim))
            int.TryParse(userIdClaim, out userId);

        Project project = new Project
        {
            StartSate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            CreatedBy = userId
            // Do not set CreatedByNavigation here; it should be set by EF when loading from DB
        };
        return View(project);
    }

    [Authorize(Roles = "Admin")]
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
            if (project.Id == 0)
            {
                // Create new project
                project.CreatedDate = DateTime.Now;
                _pservice.AddProject(project);
            }
            else
            {
                // Update existing project
                _pservice.UpdateProject(project);
            }
            return RedirectToAction(nameof(Index));
        }

        return View("CreateProject", project);
    }


    public IActionResult ProjectDetails(int id)
    {
        var project = _pservice.GetProject(id);
                
        if (project == null) return NotFound();

        var vm = new ProjectDetailsVM
        {
            Id = project.Id,
            Name = project.Name,
            NameAr = project.NameAr,
            Description = project.Description,
            DescriptionAr = project.DescriptionAr,
            ResponsibleForImplementing = project.ResponsibleForImplementing,
            SystemOwner = project.SystemOwner,
            ProjectGoal = project.ProjectGoal,
            StartSate = project.StartSate,
            EndDate = project.EndDate,
            TotalCost = project.TotalCost,
            Tasks = project.Tasks.ToList()
        };

        // Calculate KPIs
        vm.KPIs = CalculateProjectKPIs(project);

        return View(vm);
    }

    private ProjectKPIs CalculateProjectKPIs(Project project)
    {
        var kpis = new ProjectKPIs();
        var tasks = project.Tasks.ToList();
        var today = DateTime.Today;

        // Task Status KPIs
        kpis.TotalTasks = tasks.Count;
        kpis.CompletedTasks = tasks.Count(t => t.DoneRatio >= 1.0m);
        kpis.InProgressTasks = tasks.Count(t => t.DoneRatio > 0 && t.DoneRatio < 1.0m);
        kpis.NotStartedTasks = tasks.Count(t => t.DoneRatio == 0 || t.DoneRatio == null);
        kpis.OverdueTasks = tasks.Count(t => t.ExpectedEndDate < today && (t.DoneRatio < 1.0m || t.DoneRatio == null));

        // Completion Percentages
        if (kpis.TotalTasks > 0)
        {
            kpis.CompletionPercentage = Math.Round((decimal)kpis.CompletedTasks / kpis.TotalTasks * 100, 2);
            kpis.AverageTaskCompletion = Math.Round(
                tasks.Average(t => (t.DoneRatio ?? 0) * 100), 2);
        }

        // Department Distribution
        kpis.TasksByDepartment = tasks
            .Where(t => !string.IsNullOrEmpty(t.ImplementorDepartment))
            .GroupBy(t => t.ImplementorDepartment)
            .ToDictionary(g => g.Key, g => g.Count());

        // Stage Distribution
        kpis.TasksByStage = tasks
            .Where(t => !string.IsNullOrEmpty(t.StageName))
            .GroupBy(t => t.StageName)
            .ToDictionary(g => g.Key, g => g.Count());

        // Time-based KPIs
        kpis.TotalProjectDays = (project.EndDate - project.StartSate).Days;
        kpis.DaysRemaining = (project.EndDate - today).Days;

        if (kpis.TotalProjectDays > 0)
        {
            var daysElapsed = kpis.TotalProjectDays - kpis.DaysRemaining;
            kpis.ProjectProgressPercentage = Math.Round(
                (decimal)daysElapsed / kpis.TotalProjectDays * 100, 2);
        }

        return kpis;
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _pservice.GetProjectAsync(id);
        if (project == null)
            return NotFound();
        // Reuse the CreateProject view for editing
        ViewData["Title"] = "Edit Project";
        return View("CreateProject", project);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Project model)
    {
        if (id != model.Id)
            return BadRequest();

        ModelState.Remove(nameof(Project.CreatedDate));
        ModelState.Remove(nameof(Project.CreatedBy));
        ModelState.Remove(nameof(Project.CreatedByNavigation));
        ModelState.Remove(nameof(Project.Tasks));

        if (ModelState.IsValid)
        {
            try
            {
                await _pservice.UpdateProjectAsync(model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_pservice.ProjectExists(model.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Edit Project";
        return View("CreateProject", model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _pservice.GetProjectAsync(id);
        if (project == null)
            return NotFound();
        return View(project);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostDelete(int id)
    {
        await _pservice.DeleteProjectAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
