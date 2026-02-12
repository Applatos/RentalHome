using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sommerhus.Api.Infrastructure.Auth;
using Sommerhus.Core.Dtos.Admin;

using Sommerhus.Core.Identity;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public sealed class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptionsAccessor)
    : ControllerBase
{
    private readonly SignInManager<ApplicationUser> signInManager = signInManager;
    private readonly UserManager<ApplicationUser> userManager = userManager;
    private readonly JwtOptions jwtOptions = jwtOptionsAccessor.Value;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AdminTokenResponse>> Login([FromBody] AdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return Forbid();
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return Ok(await CreateTokenAsync(user));
    }

    private async Task<AdminTokenResponse> CreateTokenAsync(ApplicationUser user)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes);

        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
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

        return new AdminTokenResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }
}
