using Jobick.Services;
using Jobick.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobick.Controllers;

/// <summary>
/// Handles authentication and dashboard statistics (KPIs) for projects.
/// </summary>
public class ADHMMCController(UserService _userService, ProjectService _projectService, TaskService _taskService) : Controller
{
    /// <summary>
    /// Displays login page and ensures any previous cookie is cleared.
    /// </summary>
    public async Task<IActionResult> LoginAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return View();
    }

    /// <summary>
    /// Performs cookie-based authentication when credentials are valid.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool isRemeberLogin = false)
    {
        var user = await _userService.LoginAsync(username, password);

        if (user != null)
        {
            // Map Write permission to Admin role; otherwise Viewer.
            var role = user.Write ? "Admin" : "Viewer";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isRemeberLogin
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", "خطأ في كلمة المرور او اسم المستخدم");
        return View();
    }

    /// <summary>
    /// Displays the dashboard with aggregate KPIs.
    /// KPIs include project status distribution, counts per year, budgets for not fully done projects,
    /// budgets by year, and overdue projects with incomplete tasks.
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetProjectListAsync();
        var allTasks = await _taskService.GetTaskListAsync();
        var now = DateTime.Today;
        var thisYear = now.Year;

        // First KPI: Project status distribution
        // Only include projects that contain at least one task (matches UI hint)
        // Classify by task progress: all 100% => Done, all 0%/null => Not Started, otherwise In Progress
        int inProgress = 0, notStarted = 0, done = 0;
        foreach (var p in projects)
        {
            if (p.Tasks == null || p.Tasks.Count == 0)
                continue; // exclude projects without tasks

            bool allDone = p.Tasks.All(t => t.DoneRatio == 1.0m);
            bool noneStarted = p.Tasks.All(t => (t.DoneRatio ?? 0m) == 0m);

            if (allDone)
            {
                done++;
            }
            else if (noneStarted)
            {
                notStarted++;
            }
            else
            {
                inProgress++;
            }
        }

        // Second KPI: Projects count by year (EndDate.Year)
        var projectsCountByYear = projects
            .GroupBy(p => p.EndDate.Year)
            .ToDictionary(g => g.Key, g => g.Count());

        // Third KPI: Budgets for projects not fully done (use TotalCost if set, else sum task costs)
        var budgetsExceptFullyDone = new Dictionary<int, decimal>();
        var projectNames = new Dictionary<int, string>();

        foreach (var p in projects)
        {
            bool fullyDone = p.Tasks.Count > 0 && p.Tasks.All(t => t.DoneRatio == 1.0m);
            if (fullyDone) continue;

            // Prefer project.TotalCost, fallback to sum of task costs
            decimal budget = p.TotalCost ?? p.Tasks.Sum(t => t.Cost ?? 0m);
            if (budget < 0) budget = 0; // Clamp negative to zero for stability
            budgetsExceptFullyDone[p.Id] = budget;
            projectNames[p.Id] = p.Name;
        }

        // Fourth KPI: Budgets by EndDate.Year (include all years)
        var budgetsByYear = projects
            .GroupBy(p => p.EndDate.Year)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalCost ?? 0m));

        // Fifth KPI: Projects with incomplete tasks and EndDate passed
        var overdueProjects = projects
            .Where(p => p.EndDate < now && p.Tasks.Any(t => t.DoneRatio < 1.0m))
            .Select(p => new ProjectStatisticsVM.ProjectInfo
            {
                ProjectId = p.Id,
                Name = p.Name,
                EndDate = p.EndDate,
                IncompleteTasksCount = p.Tasks.Count(t => t.DoneRatio < 1.0m)
            })
            .ToList();

        var vm = new ProjectStatisticsVM
        {
            InProgressProjects = inProgress,
            NotStartedProjects = notStarted,
            DoneProjects = done,
            ProjectsCountByYear = projectsCountByYear,
            AllProjectsBudgetsExceptFullyDone = budgetsExceptFullyDone,
            ProjectNames = projectNames,
            ProjectsBudgetsByYear = budgetsByYear,
            OverdueProjectsWithIncompleteTasks = overdueProjects
        };

        return View(vm);
    }

    /// <summary>
    /// Returns 403 page.
    /// </summary>
    public IActionResult Error403()
    {
        return View();
    }

    /// <summary>
    /// Returns 500 page.
    /// </summary>
    public IActionResult Error500()
    {
        return View();
    }
}
