using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Auth;
using Sommerhus.Core.Identity;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Account;

namespace Sommerhus.Mvc.Controllers;

public sealed class AccountController(AdminAuthClient adminAuthClient, PublicAuthClient publicAuthClient) : Controller
{
    private readonly AdminAuthClient adminAuthClient = adminAuthClient;
    private readonly PublicAuthClient publicAuthClient = publicAuthClient;

    [AllowAnonymous]
    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Try public login first (works for all roles)
        var response = await publicAuthClient.LoginAsync(
            new LoginRequest { Username = model.Username, Password = model.Password },
            HttpContext.RequestAborted);

        if (response.Ok && response.Data is { } token && !string.IsNullOrWhiteSpace(token.Token))
        {
            await SignInWithTokenAsync(model.Username, token.Token, token.Role, token.ExpiresAt);
            return RedirectToLocal(model.ReturnUrl);
        }

        // Fallback: try admin login (backward compatibility)
        var adminResponse = await adminAuthClient.LoginAsync(
            new AdminLoginRequest { Username = model.Username, Password = model.Password },
            HttpContext.RequestAborted);

        if (adminResponse.Ok && adminResponse.Data is { } adminToken && !string.IsNullOrWhiteSpace(adminToken.Token))
        {
            await SignInWithTokenAsync(model.Username, adminToken.Token, AppRoles.Admin, adminToken.ExpiresAt);
            return RedirectToLocal(model.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid username or password.");
        model.Password = string.Empty;
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet("/account/register")]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/account/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var response = await publicAuthClient.RegisterAsync(
            new RegisterRequest
            {
                Username = model.Username,
                Email = model.Email,
                Password = model.Password,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone
            },
            HttpContext.RequestAborted);

        if (response.Ok && response.Data is { } token && !string.IsNullOrWhiteSpace(token.Token))
        {
            await SignInWithTokenAsync(model.Username, token.Token, token.Role, token.ExpiresAt);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (response.HasValidationErrors)
        {
            foreach (var (field, messages) in response.Errors)
                foreach (var msg in messages)
                    ModelState.AddModelError(field, msg);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Registration failed.");
        }

        model.Password = string.Empty;
        model.ConfirmPassword = string.Empty;
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

    private async Task SignInWithTokenAsync(string username, string jwtToken, string role, DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new(SommerhusClaimTypes.AdminAccessToken, jwtToken)
        };

        if (expiresAt != default)
            claims.Add(new Claim(ClaimTypes.Expiration, expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Houses");
    }
}
