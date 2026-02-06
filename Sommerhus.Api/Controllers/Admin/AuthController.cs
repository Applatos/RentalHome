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
using Sommerhus.Core.Dtos.Security;
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

        if (!await userManager.IsInRoleAsync(user, AdminRoles.Admin))
        {
            return Forbid();
        }

        return Ok(CreateToken(user));
    }

    private AdminTokenResponse CreateToken(ApplicationUser user)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Role, AdminRoles.Admin)
        };

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
