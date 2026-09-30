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
    // Paths rather than RedirectToAction("Index", "Houses"): Houses exists both publicly and in
    // admin, and where a user lands depends on the role, not on a controller name.
    private const string PublicLanding = "~/houses";
    private const string AdminLanding = "~/admin/houses";

    private readonly AdminAuthClient adminAuthClient = adminAuthClient;
    private readonly PublicAuthClient publicAuthClient = publicAuthClient;

    [AllowAnonymous]
    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        // A page rendered before signing in (another tab, the back button) can still link here with
        // a returnUrl that carries the visitor's choice, e.g. a booking with dates. Honouring it
        // cannot loop: missing access now goes to the access-denied page, not back to login.
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl, User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : null);

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
            return RedirectToLocal(model.ReturnUrl, token.Role);
        }

        // Fallback: try admin login (backward compatibility)
        var adminResponse = await adminAuthClient.LoginAsync(
            new AdminLoginRequest { Username = model.Username, Password = model.Password },
            HttpContext.RequestAborted);

        if (adminResponse.Ok && adminResponse.Data is { } adminToken && !string.IsNullOrWhiteSpace(adminToken.Token))
        {
            await SignInWithTokenAsync(model.Username, adminToken.Token, AppRoles.Admin, adminToken.ExpiresAt);
            return RedirectToLocal(model.ReturnUrl, AppRoles.Admin);
        }

        ModelState.AddModelError(string.Empty, "Invalid username or password.");
        model.Password = string.Empty;
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet("/account/register")]
    public IActionResult Register(string? returnUrl = null)
    {
        // Same reasoning as the login page.
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl, User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : null);

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
            return RedirectToLocal(model.ReturnUrl, token.Role);
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
        return LocalRedirect(PublicLanding);
    }

    // The cookie handler's AccessDeniedPath: a signed-in user lacks the role a page needs.
    [AllowAnonymous]
    [HttpGet("/account/access-denied")]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View();
    }

    // "Log in as another user" on the access-denied page.
    [AllowAnonymous]
    [HttpPost("/account/switch-user")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchUser(string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Url.IsLocalUrl(returnUrl)
            ? RedirectToAction(nameof(Login), new { returnUrl })
            : RedirectToAction(nameof(Login));
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

        // The cookie carries the API token, so it must not outlive it. Otherwise the user
        // looks logged in while every API call fails with 401. AllowRefresh = false stops
        // sliding expiration from extending the cookie past the token's own expiry.
        var properties = new AuthenticationProperties { AllowRefresh = false };
        if (expiresAt != default)
            properties.ExpiresUtc = expiresAt;

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    // role is the role just signed in with: User still holds the previous principal until the
    // next request.
    private IActionResult RedirectToLocal(string? returnUrl, string? role)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToLanding(role);
    }

    private IActionResult RedirectToLanding(string? role)
        => LocalRedirect(role == AppRoles.Admin ? AdminLanding : PublicLanding);
}
