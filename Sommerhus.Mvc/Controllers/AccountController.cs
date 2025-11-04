using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sommerhus.Mvc.Infrastructure;

namespace Sommerhus.Mvc.Controllers;

public sealed class AccountController(IOptions<AdminAuthOptions> options) : Controller
{
    private readonly AdminAuthOptions adminOptions = options.Value;

    [AllowAnonymous]
    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (IsValid(model.Username, model.Password))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, model.Username)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToLocal(model.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Forkert brugernavn eller adgangskode.");
        model.Password = string.Empty;
        return View(model);
    }

    [Authorize]
    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Houses");
    }

    private bool IsValid(string username, string password)
        => string.Equals(username, adminOptions.Username, StringComparison.Ordinal) &&
           string.Equals(password, adminOptions.Password, StringComparison.Ordinal);

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Admin");
    }
}

public sealed class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
