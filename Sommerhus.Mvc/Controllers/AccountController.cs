using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Security;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Account;

namespace Sommerhus.Mvc.Controllers;

public sealed class AccountController(AdminAuthClient authClient) : Controller
{
    private readonly AdminAuthClient adminAuthClient = authClient;

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

        var response = await adminAuthClient.LoginAsync(
            new AdminLoginRequest
            {
                Username = model.Username,
                Password = model.Password
            },
            HttpContext.RequestAborted);

        if (response.Ok && response.Data is { } token && !string.IsNullOrWhiteSpace(token.Token))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, AdminRoles.Admin),
                new Claim(SommerhusClaimTypes.AdminAccessToken, token.Token)
            };

            if (token.ExpiresAt != default)
            {
                claims.Add(new Claim(ClaimTypes.Expiration, token.ExpiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToLocal(model.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid username or password.");
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

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Admin");
    }
}
