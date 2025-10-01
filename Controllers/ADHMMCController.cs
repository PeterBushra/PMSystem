using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobick.Controllers;
public class ADHMMCController : Controller
{
    public async Task<IActionResult> LoginAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return View();
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

            return RedirectToAction(nameof(Index)); // Redirect after login
        }

        ModelState.AddModelError("", "Invalid username or password");
        return View();
    }

    [Authorize] // Only logged-in users
    public IActionResult Index()
    {
        return View();
    }
}
