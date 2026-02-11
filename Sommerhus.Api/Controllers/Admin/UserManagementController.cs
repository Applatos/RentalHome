using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Security;
using Sommerhus.Core.Identity;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UserManagementController(
    UserManager<ApplicationUser> userManager,
    AppDbContext db) : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager = userManager;
    private readonly AppDbContext db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List(CancellationToken ct)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);

        var result = new List<UserListItemDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var primaryRole = roles.Contains(AppRoles.Admin) ? AppRoles.Admin
                : roles.Contains(AppRoles.HouseOwner) ? AppRoles.HouseOwner
                : roles.Contains(AppRoles.User) ? AppRoles.User
                : "None";

            result.Add(new UserListItemDto(
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                primaryRole,
                user.CreatedAtUtc,
                user.LastLoginAtUtc));
        }

        return Ok(result);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeUserRoleRequest request, CancellationToken ct)
    {
        if (!AppRoles.All.Contains(request.Role))
            return BadRequest($"Invalid role. Must be one of: {string.Join(", ", AppRoles.All)}");

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await userManager.RemoveFromRolesAsync(user, currentRoles);

        var addResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
        {
            foreach (var error in addResult.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem();
        }

        return NoContent();
    }
}
