using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Dtos.Owner;
using Sommerhus.Core.Dtos.Security;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Owner;

namespace Sommerhus.Api.Controllers.Owner;

[ApiController]
[Route("api/owner/houses")]
[Authorize(Roles = $"{AppRoles.HouseOwner},{AppRoles.Admin}")]
public sealed class OwnerHouseController(IOwnerHouseService ownerHouseService) : ControllerBase
{
    private readonly IOwnerHouseService ownerHouseService = ownerHouseService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OwnerHouseListItemDto>>> List(CancellationToken ct)
    {
        var ownerId = GetUserId();
        if (ownerId is null) return Unauthorized();

        var items = await ownerHouseService.ListAsync(ownerId, ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminHouseDetailsDto>> GetDetails(Guid id, CancellationToken ct)
    {
        var ownerId = GetUserId();
        if (ownerId is null) return Unauthorized();

        var result = await ownerHouseService.GetDetailsAsync(ownerId, id, Request.BaseUrl(), ct);
        return this.FromResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] OwnerUpdateHouseDto dto, CancellationToken ct)
    {
        var ownerId = GetUserId();
        if (ownerId is null) return Unauthorized();

        var result = await ownerHouseService.UpdateAsync(ownerId, id, dto, ct);
        return this.FromResult(result);
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
