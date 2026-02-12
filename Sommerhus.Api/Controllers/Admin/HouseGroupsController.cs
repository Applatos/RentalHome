using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Admin.HouseGroups;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;


namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/house-groups")]
public sealed class HouseGroupsController(IAdminHouseGroupService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HouseGroupDto>>> List(CancellationToken ct)
        => this.FromResult(await service.ListAsync(ct));

    [HttpGet("lookup")]
    public Task<IReadOnlyList<LookupItem>> Lookup(CancellationToken ct)
        => service.GetAllAsync(ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseGroupDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return this.FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<LookupItem>> Create([FromBody] HouseGroupDto dto, CancellationToken ct)
    {
        var result = await service.CreateAsync(dto, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => Created($"/api/admin/house-groups/{result.Value!.Id}", result.Value),
            _ => this.FromResult(result),
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HouseGroupDto>> Update(Guid id, [FromBody] UpsertHouseGroupDto dto, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, dto, ct);
        return this.FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return this.FromResult(result);
    }

    [HttpPost("{groupId:guid}/calendar")]
    public async Task<ActionResult<SeasonSpanDto>> AddSeasonSpan(Guid groupId, [FromBody] UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var result = await service.AddSeasonSpanAsync(groupId, dto, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => Created($"/api/admin/house-groups/{groupId}/calendar/{result.Value!.Id}", result.Value),
            _ => this.FromResult(result),
        };
    }

    [HttpPut("{groupId:guid}/calendar/{spanId:guid}")]
    public async Task<ActionResult<SeasonSpanDto>> UpdateSeasonSpan(Guid groupId, Guid spanId, [FromBody] UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var result = await service.UpdateSeasonSpanAsync(groupId, spanId, dto, ct);
        return this.FromResult(result);
    }

    [HttpDelete("{groupId:guid}/calendar/{spanId:guid}")]
    public async Task<ActionResult> DeleteSeasonSpan(Guid groupId, Guid spanId, CancellationToken ct)
    {
        var result = await service.DeleteSeasonSpanAsync(groupId, spanId, ct);
        return this.FromResult(result);
    }
}
