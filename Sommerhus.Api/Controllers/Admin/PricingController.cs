using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Admin.Pricing;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Security;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
[Route("api/admin")]
public sealed class PricingController(IAdminPricingService service) : ControllerBase
{
    [HttpGet("calendar/groups/{groupId:guid}/spans")]
    public Task<IReadOnlyList<SeasonSpanDto>> GetGroupSpans(Guid groupId, CancellationToken ct)
        => service.GetSeasonSpansAsync(groupId, ct);

    [HttpPut("calendar/groups/{groupId:guid}/spans")]
    public async Task<ActionResult<IReadOnlyList<SeasonSpanDto>>> UpsertGroupSpans(Guid groupId, [FromBody] IReadOnlyList<SeasonSpanDto> spans, CancellationToken ct)
        => this.FromResult(await service.UpsertSeasonSpansAsync(groupId, spans, ct));

    [HttpGet("pricing/plans/{houseId:guid}")]
    public Task<IReadOnlyList<PricePlanDetailsDto>> GetRatePlans(Guid houseId, CancellationToken ct)
        => service.GetRatePlansAsync(houseId, ct);

    [HttpPost("pricing/plans/{planId:guid}/activate")]
    public async Task<IActionResult> ActivatePlan(Guid planId, CancellationToken ct)
        => this.FromResult(await service.ActivateRatePlanAsync(planId, ct));

    [HttpDelete("houses/{houseId:guid}/pricing/rate-plans/{ratePlanId:guid}")]
    public async Task<IActionResult> DeleteRatePlan(Guid houseId, Guid ratePlanId, CancellationToken ct)
        => this.FromResult(await service.DeleteRatePlanAsync(houseId, ratePlanId, ct));

    [HttpGet("pricing/season-codes")]
    public Task<IReadOnlyList<SeasonCodeDto>> ListSeasonCodes(CancellationToken ct)
        => service.ListSeasonCodesAsync(ct);

    [HttpPost("pricing/season-codes")]
    public async Task<ActionResult<SeasonCodeDto>> CreateSeasonCode([FromBody] SeasonCodeDto dto, CancellationToken ct)
    {
        var result = await service.CreateSeasonCodeAsync(dto, ct);
        if (result.Status == ServiceResultStatus.Success)
        {
            return Created($"pricing/season-codes/{result.Value!.Code}", result.Value);
        }

        return this.FromResult(result);
    }
}
