using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Auth;
using Sommerhus.Core.Dtos.Security;
using Sommerhus.Core.Identity;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class UserProfileController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager = userManager;

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var primaryRole = roles.Contains(AppRoles.Admin) ? AppRoles.Admin
            : roles.Contains(AppRoles.HouseOwner) ? AppRoles.HouseOwner
            : AppRoles.User;

        return Ok(new UserProfileDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.Address,
            primaryRole,
            user.CreatedAtUtc,
            user.LastLoginAtUtc));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return NotFound();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Address = request.Address;

        if (!string.IsNullOrWhiteSpace(request.Email))
            user.Email = request.Email;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem();
        }

        return NoContent();
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }
}
