using Jobick.Models;
using Jobick.Services;
using Jobick.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Jobick.Controllers;

/// <summary>
/// Controller responsible for CRUD and reporting operations for projects.
/// All actions require authentication; some require Admin role.
/// </summary>
[Authorize]
public class ProjectsController(ProjectService _pservice) : Controller
{
    /// <summary>
    /// Lists all projects with their related data using the backing service.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var projects = await  _pservice.GetProjectListAsync();
        return View(projects);
    }

    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Returns the create project screen with sensible defaults.
    /// </summary>
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
    /// <summary>
    /// Handles creation or update of a project based on whether <see cref="Project.Id"/> is zero.
    /// Keeps model state valid by removing properties that are not posted from the form.
    /// </summary>
    public IActionResult PostProject(Project project)
    {
        ModelState.Remove(nameof(Project.Id));
        ModelState.Remove(nameof(Project.CreatedDate));
        ModelState.Remove(nameof(Project.CreatedBy));
        ModelState.Remove(nameof(Project.CreatedByNavigation));
        ModelState.Remove(nameof(Project.Tasks));
        ModelState.Remove(nameof(Project.NameAr));
        ModelState.Remove(nameof(Project.DescriptionAr));
        // Ensure Arabic fields mirror the provided English values if not explicitly provided
        project.NameAr = project.Name;
        project.DescriptionAr = project.Description;

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


    /// <summary>
    /// Shows details for a single project including derived KPIs.
    /// </summary>
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

    /// <summary>
    /// Calculates common KPIs for a given project.
    /// The method uses only in-memory task data to avoid extra queries and keeps
    /// output stable with explicit clamping and rounding where appropriate.
    /// </summary>
    /// <remarks>
    /// KPI definitions:
    /// - CompletedTasks: t.DoneRatio >= 1.0 (DoneRatio stored as 0..1 fraction)
    /// - InProgressTasks: 0 &lt; DoneRatio &lt; 1
    /// - NotStartedTasks: null or 0
    /// - OverdueTasks: ExpectedEndDate &lt; Today and not completed
    /// - CompletionPercentage: CompletedTasks / TotalTasks * 100
    /// - AverageTaskCompletion: Average of task DoneRatio values scaled to 0..100
    /// - StageCompletionByWeight: For each stage, Σ(w_i * done_i) / Σ(w_i) as a fraction in [0,1]
    /// - ProjectProgressPercentage: elapsedDays / totalProjectDays * 100
    /// </remarks>
    private ProjectKPIs CalculateProjectKPIs(Project project)
    {
        var kpis = new ProjectKPIs();
        var tasks = project.Tasks.ToList();
        var today = DateTime.Today;

        // Task status counts
        kpis.TotalTasks = tasks.Count;
        kpis.CompletedTasks = tasks.Count(t => t.DoneRatio >= 1.0m);
        kpis.InProgressTasks = tasks.Count(t => t.DoneRatio is > 0m and < 1.0m);
        kpis.NotStartedTasks = tasks.Count(t => t.DoneRatio == 0m || t.DoneRatio == null);
        kpis.OverdueTasks = tasks.Count(t => t.ExpectedEndDate < today && (t.DoneRatio == null || t.DoneRatio < 1.0m));

        if (kpis.TotalTasks > 0)
        {
            // Overall completion percentage based on completed task count (0..100)
            kpis.CompletionPercentage = Math.Round((decimal)kpis.CompletedTasks / kpis.TotalTasks * 100m, 2);
            // Average of task DoneRatio values (convert from fraction to percentage)
            kpis.AverageTaskCompletion = Math.Round(tasks.Average(t => (t.DoneRatio ?? 0m) * 100m), 2);
        }

        // Department distribution
        kpis.TasksByDepartment = tasks
            .Where(t => !string.IsNullOrWhiteSpace(t.ImplementorDepartment))
            .GroupBy(t => t.ImplementorDepartment!.Trim())
            .ToDictionary(g => g.Key, g => g.Count());

        // Stage counts (raw)
        kpis.StageTaskCounts = tasks
            .Where(t => !string.IsNullOrWhiteSpace(t.StageName))
            .GroupBy(t => t.StageName!.Trim())
            .ToDictionary(g => g.Key, g => g.Count());

        // Per-stage weighted completion (normalized per stage to make each stage max at 100%)
        kpis.StageCompletionByWeight = new Dictionary<string, decimal>();
        var stageGroups = tasks
            .Where(t => !string.IsNullOrWhiteSpace(t.StageName))
            .GroupBy(t => t.StageName!.Trim());

        foreach (var g in stageGroups)
        {
            // Σ w_i within the stage
            decimal sumStageWeights = g.Sum(t => t.Weight ?? 0m);
            // Σ (w_i * done_i) where done_i is a fraction in [0,1]
            decimal weightedDone = g.Sum(t => (t.Weight ?? 0m) * (t.DoneRatio ?? 0m));
            // Relative completion within this stage (fraction 0..1); protects against divide by zero
            decimal relative = (sumStageWeights > 0m) ? (weightedDone / sumStageWeights) : 0m;

            // Clamp for safety to keep values within [0,1]
            if (relative < 0m) relative = 0m;
            if (relative > 1m) relative = 1m;

            kpis.StageCompletionByWeight[g.Key] = relative; // fraction 0..1
        }

        // Timeline KPIs
        kpis.TotalProjectDays = (project.EndDate - project.StartSate).Days;
        kpis.DaysRemaining = (project.EndDate - today).Days;
        if (kpis.TotalProjectDays > 0)
        {
            var elapsed = kpis.TotalProjectDays - kpis.DaysRemaining;
            kpis.ProjectProgressPercentage = Math.Round((decimal)elapsed / kpis.TotalProjectDays * 100m, 2);
        }

        return kpis;
    }

    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Returns the edit form reusing the create view for consistency.
    /// </summary>
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
    /// <summary>
    /// Handles the edit postback using optimistic concurrency through EF Core.
    /// </summary>
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
    /// <summary>
    /// Shows confirmation for deletion of a project.
    /// </summary>
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
    /// <summary>
    /// Executes the deletion of a project, ensuring related tasks are also removed by the service.
    /// </summary>
    public async Task<IActionResult> PostDelete(int id)
    {
        await _pservice.DeleteProjectAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
