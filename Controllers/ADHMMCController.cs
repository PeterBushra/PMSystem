using Jobick.Services;
using Jobick.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobick.Controllers;
public class ADHMMCController(UserService _userService, ProjectService _projectService, TaskService _taskService) : Controller
{
    public async Task<IActionResult> LoginAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool isRemeberLogin = false)
    {
        var user = await _userService.LoginAsync(username, password);

        if (user != null)
        {
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

        ModelState.AddModelError("", "Invalid username or password");
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetProjectListAsync();
        var allTasks = await _taskService.GetTaskListAsync();
        var now = DateTime.Today;
        var thisYear = now.Year;

        // First KPI
        int inProgress = 0, notStarted = 0, done = 0;
        foreach (var p in projects)
        {
            bool allDone = p.Tasks.Count > 0 && p.Tasks.All(t => t.DoneRatio == 1.0m);
            if (allDone)
            {
                done++;
            }
            else if (p.StartSate > now)
            {
                notStarted++;
            }
            else if (p.StartSate <= now && p.EndDate > now)
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

            decimal budget = p.TotalCost ?? p.Tasks.Sum(t => t.Cost ?? 0m);
            if (budget < 0) budget = 0;
            budgetsExceptFullyDone[p.Id] = budget;
            projectNames[p.Id] = p.Name;
        }

        // Fourth KPI: Budgets by year (EndDate.Year), only years >= this year
        var budgetsByYear = projects
            .Where(p => p.EndDate.Year >= thisYear)
            .GroupBy(p => p.EndDate.Year)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalCost ?? 0));

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

    public IActionResult Error403()
    {
        return View();
    }

    public IActionResult Error500()
    {
        return View();
    }
}
