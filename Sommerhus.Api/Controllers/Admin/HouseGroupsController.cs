using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Application.Admin.HouseGroups;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(AuthenticationSchemes = "AdminBasic")]
[Route("api/admin/house-groups")]
public sealed class HouseGroupsController(IAdminHouseGroupService service) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<LookupItem>> List(CancellationToken ct)
        => await service.GetAllAsync(ct);

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
}
