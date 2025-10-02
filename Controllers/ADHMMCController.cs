using Jobick.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jobick.Controllers;
public class ADHMMCController(UserService _userService) : Controller
{
    public async Task<IActionResult> LoginAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool isRemeberLogin = false)
    {
        // Use UserService to validate user credentials
        var user = await _userService.LoginAsync(username, password);

        if (user != null)
        {
            // Determine role based on Write property
            var role = user.Write ? "Admin" : "Viewer";

            // Create user claims
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
                IsPersistent = isRemeberLogin // Remember login
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

    public IActionResult Error403()
    {
        return View();
    }

    public IActionResult Error500()
    {
        return View();
    }
}
