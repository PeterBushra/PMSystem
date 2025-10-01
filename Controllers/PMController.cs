using Jobick.Models;
using Jobick.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Jobick.Controllers;

/// <summary>
///  Projects Controller
/// </summary>

public class PMController (ProjectService _pservice) : Controller
{

    public IActionResult Login()
    {
        return View();
    }



    // GET: Create Project
    [Authorize] // Only logged-in users
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


    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        // TODO: Replace with your user validation logic
        if (username == "admin" && password == "1234")
        {
            // Create user claims
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Admin") // Example role
                };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true // Remember login
            };

            // Sign in the user
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            return RedirectToAction("Index", "Jobick"); // Redirect after login
        }

        ModelState.AddModelError("", "Invalid username or password");
        return View();
    }

    // Logout
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
