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
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetProjectListAsync();
        var allTasks = await _taskService.GetTaskListAsync();

        var vm = _statisticsService.CalculateDashboard(projects, allTasks);
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
