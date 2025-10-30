using Jobick.Models;
using Jobick.Services.Interfaces;
using Jobick.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Jobick.Services;
using Jobick.Extensions;

namespace Jobick.Controllers;

/// <summary>
/// Controller responsible for CRUD and reporting operations for projects.
/// All actions require authentication; some require Admin role.
/// </summary>
[Authorize]
public class ProjectsController(IProjectService _projectService, IProjectKpiService _kpiService, IAttachmentService _attachmentService) : Controller
{
    /// <summary>
    /// Lists all projects with their related data using the backing service.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var projects = await  _projectService.GetProjectListAsync();
        return View(projects);
    }

    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Returns the create project screen with sensible defaults.
    /// </summary>
    public IActionResult CreateProject()
    {
        // Get the user ID from claims and parse to int
        int userId = User.GetUserIdOrDefault();

        Project project = new Project
        {
            StartSate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            CreatedBy = userId
        };

        // Populate departments for dropdown
        ViewBag.Departments = DepartmentService.GetDepartments().ToSelectList();

        return View(project);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    /// <summary>
    /// Handles creation or update of a project based on whether <see cref="Project.Id"/> is zero.
    /// Keeps model state valid by removing properties that are not posted from the form.
    /// </summary>
    public async Task<IActionResult> PostProject(Project project)
    {
        ModelState.Remove(nameof(Project.Id));
        ModelState.Remove(nameof(Project.CreatedDate));
        ModelState.Remove(nameof(Project.CreatedBy));
        ModelState.Remove(nameof(Project.CreatedByNavigation));
        ModelState.Remove(nameof(Project.Tasks));
        ModelState.Remove(nameof(Project.NameAr));
        ModelState.Remove(nameof(Project.DescriptionAr));
        // Ensure Arabic fields mirror the provided English values if not explicitly provided
        project.MirrorArabicFromEnglish();

        if (ModelState.IsValid)
        {
            if (project.Id == 0)
            { 
                // Create new project
                project.CreatedDate = DateTime.Now;
                await _projectService.AddProjectAsync(project);
            }
            else
            {
                // Update existing project
                await _projectService.UpdateProjectAsync(project);
            }
            return RedirectToAction(nameof(Index));
        }

        // Repopulate departments on validation errors
        ViewBag.Departments = DepartmentService.GetDepartments().ToSelectList();

        return View("CreateProject", project);
    }


    /// <summary>
    /// Shows details for a single project including derived KPIs.
    /// </summary>
    public async Task<IActionResult> ProjectDetails(int id)
    {
        var project = await _projectService.GetProjectAsync(id);
                
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
            StrategicProgramme = project.StrategicProgramme,
            StrategicGoal= project.StrategicGoal,
            DelayReasons = project.DelayReasons,
            Tasks = project.Tasks.ToList()
        };

        // Calculate KPIs
        vm.KPIs = _kpiService.Calculate(project);

        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Returns the edit form reusing the create view for consistency.
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectService.GetProjectAsync(id);
        if (project == null)
            return NotFound();
        // Reuse the CreateProject view for editing
        ViewData["Title"] = "Edit Project";

        // Populate departments for dropdown
        ViewBag.Departments = DepartmentService.GetDepartments().ToSelectList();

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
                await _projectService.UpdateProjectAsync(model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_projectService.ProjectExists(model.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Edit Project";

        // Repopulate departments on validation errors
        ViewBag.Departments = DepartmentService.GetDepartments().ToSelectList();

        return View("CreateProject", model);
    }

    [Authorize(Roles = "Admin")]
    /// <summary>
    /// Shows confirmation for deletion of a project.
    /// </summary>
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _projectService.GetProjectAsync(id);
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
        await _projectService.DeleteProjectAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
