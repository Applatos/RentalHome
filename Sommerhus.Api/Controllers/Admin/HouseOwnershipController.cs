using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Security;
using Sommerhus.Core.Identity;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class HouseOwnershipController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly AppDbContext db = db;
    private readonly UserManager<ApplicationUser> userManager = userManager;

    [HttpPut("{id:guid}/owner")]
    public async Task<IActionResult> AssignOwner(Guid id, [FromBody] AssignOwnerRequest request, CancellationToken ct)
    {
        var house = await db.Houses.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (house is null) return NotFound();

        if (request.OwnerId is not null)
        {
            var owner = await userManager.FindByIdAsync(request.OwnerId);
            if (owner is null)
                return BadRequest("User not found.");

            if (!await userManager.IsInRoleAsync(owner, AppRoles.HouseOwner) &&
                !await userManager.IsInRoleAsync(owner, AppRoles.Admin))
                return BadRequest("User must have HouseOwner or Admin role.");
        }

        house.OwnerId = request.OwnerId;
        house.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpGet("{id:guid}/owner")]
    public async Task<IActionResult> GetOwner(Guid id, CancellationToken ct)
    {
        var house = await db.Houses.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);
        if (house is null) return NotFound();

        if (house.OwnerId is null)
            return Ok(new { ownerId = (string?)null, ownerName = (string?)null });

        var owner = await userManager.FindByIdAsync(house.OwnerId);
        return Ok(new
        {
            ownerId = house.OwnerId,
            ownerName = owner?.UserName
        });
    }
}
