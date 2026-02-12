using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sommerhus.Api.Infrastructure.Auth;
using Sommerhus.Core.Dtos.Auth;

using Sommerhus.Core.Identity;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOptions<JwtOptions> jwtOptionsAccessor)
    : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager = userManager;
    private readonly SignInManager<ApplicationUser> signInManager = signInManager;
    private readonly JwtOptions jwtOptions = jwtOptionsAccessor.Value;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthTokenResponse>> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem();
        }

        await userManager.AddToRoleAsync(user, AppRoles.User);

        return Ok(await CreateTokenAsync(user));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthTokenResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized();

        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized();

        user.LastLoginAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return Ok(await CreateTokenAsync(user));
    }

    private async Task<AuthTokenResponse> CreateTokenAsync(ApplicationUser user)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes);

        var roles = await userManager.GetRolesAsync(user);
        var primaryRole = roles.Contains(AppRoles.Admin) ? AppRoles.Admin
            : roles.Contains(AppRoles.HouseOwner) ? AppRoles.HouseOwner
            : AppRoles.User;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthTokenResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            Role = primaryRole,
            Username = user.UserName ?? string.Empty
        };
    }
}
