using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Favorites;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/me/favorites")]
[Authorize]
public sealed class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FavoriteHouseDto>>> List(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var items = await favoriteService.ListAsync(userId, ct);
        return Ok(items);
    }

    [HttpPost("{houseId:guid}")]
    public async Task<IActionResult> Add(Guid houseId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await favoriteService.AddAsync(userId, houseId, ct);
        return this.FromResult(result);
    }

    [HttpDelete("{houseId:guid}")]
    public async Task<IActionResult> Remove(Guid houseId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await favoriteService.RemoveAsync(userId, houseId, ct);
        return this.FromResult(result);
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
