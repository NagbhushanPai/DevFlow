using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DevFlow.Application.Common.Authorization;
using DevFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DevFlow.Admin.Controllers;

public sealed class AccountController(UserManager<ApplicationUser> userManager, ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return Fail(model, user is null ? "Invalid email or password." : "This account is temporarily locked. Try again later.");
        }

        if (!await userManager.CheckPasswordAsync(user, model.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Fail(model, "Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            logger.LogWarning("Non-admin user {UserId} attempted to sign in to the admin portal.", user.Id);
            return Fail(model, "This account does not have administrator access.");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, Roles.Admin),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private ViewResult Fail(LoginViewModel model, string message)
    {
        ModelState.AddModelError(string.Empty, message);
        model.Password = string.Empty;
        return View(model);
    }
}

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
