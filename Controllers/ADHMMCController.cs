using Jobick.Services;
using Jobick.Services.Interfaces;
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
public class ADHMMCController(IUserService _userService, IProjectService _projectService, ITaskService _taskService, IStatisticsService _statisticsService) : Controller
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
    /// Results are filtered by ResponsibleForImplementing when provided.
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Index(string? responsible = null)
    {
        const string AllOption = "كل المشاريع";

        var projects = await _projectService.GetProjectListAsync();
        var allTasks = await _taskService.GetTaskListAsync();

        // Build distinct list of ResponsibleForImplementing values (non-empty)
        var responsibleList = projects
            .Select(p => p.ResponsibleForImplementing)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        // Ensure the "All projects" option exists at the top
        if (!responsibleList.Contains(AllOption))
        {
            responsibleList.Insert(0, AllOption);
        }

        // Default selection: All projects if none provided
        string? selectedResponsible = string.IsNullOrWhiteSpace(responsible) ? AllOption : responsible;

        // Filter projects based on selected responsible; when "All projects" is chosen, do not filter
        var filteredProjects = string.Equals(selectedResponsible, AllOption, StringComparison.Ordinal)
            ? projects
            : projects.Where(p => string.Equals(p.ResponsibleForImplementing, selectedResponsible, StringComparison.Ordinal)).ToList();

        // Calculate dashboard for filtered projects
        var vm = _statisticsService.CalculateDashboard(filteredProjects, allTasks);

        // Pass filter data to the view
        ViewBag.ResponsibleList = responsibleList;
        ViewBag.SelectedResponsible = selectedResponsible;
        ViewBag.HasResponsibleOptions = responsibleList.Count > 0;

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
