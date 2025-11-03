using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Repository;
using Sommerhus.Domain.Models;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/house-groups")]
public sealed class HouseGroupsController : ControllerBase
{
    private readonly AppDbContext _db;

    public HouseGroupsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LookupItem>>> List(CancellationToken ct)
    {
        var groups = await _db.HouseGroups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new LookupItem(g.Id, g.Name))
            .ToListAsync(ct);

        return Ok(groups);
    }

    [HttpPost]
    public async Task<ActionResult<LookupItem>> Create([FromBody] HouseGroupDto dto, CancellationToken ct)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.name))
        {
            return BadRequest("Navn er påkrævet.");
        }

        var name = dto.name.Trim();

        var exists = await _db.HouseGroups
            .AsNoTracking()
            .AnyAsync(g => g.Name == name, ct);

        if (exists)
        {
            return Conflict("En gruppe med dette navn findes allerede.");
        }

        var entity = new HouseGroup
        {
            Id = Guid.NewGuid(),
            Name = name
        };

        await _db.HouseGroups.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        var result = new LookupItem(entity.Id, entity.Name);
        return Created($"/api/admin/house-groups/{entity.Id}", result);
    }
}