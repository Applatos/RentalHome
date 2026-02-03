using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Application.Admin.Houses;
using Sommerhus.Application.Admin.HouseGroups;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Contracts.Security;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
[Route("api/admin/houses")]
public sealed class HousesController(
    IAdminHouseService houseService,
    IAdminHouseFeatureService featureService,
    IAdminHousePricingService pricingService,
    IAdminHouseGroupService houseGroupService) : ControllerBase
{
    [HttpGet]
    public Task<PageResult<HouseListItemDto>> Search([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        => houseService.SearchAsync(query, page, pageSize, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
        => this.FromResult(await houseService.GetDetailsAsync(id, Request, ct));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertHouseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await houseService.CreateAsync(dto, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => CreatedAtAction(nameof(Get), new { id = result.Value }, result.Value),
            ServiceResultStatus.Invalid => ValidationProblem((ValidationProblemDetails)result.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unable to create house.")
        };
    }


    // PUT: /api/admin/houses/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertHouseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await houseService.UpdateAsync(id, dto, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => NoContent(),
            ServiceResultStatus.NotFound => NotFound(),
            ServiceResultStatus.Invalid => ValidationProblem((ValidationProblemDetails)result.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unable to update house.")
        };
    }


    // DELETE: /api/admin/houses/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await houseService.DeleteAsync(id, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => NoContent(),
            ServiceResultStatus.NotFound => NotFound(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unable to delete house.")
        };
    }


    [HttpPost("{houseId:guid}/features")]
    public async Task<IActionResult> UpsertFeatures(Guid houseId, [FromBody] IEnumerable<PostFeatureValueDto>? values, CancellationToken ct)
    {
        if (values is null)
        {
            return BadRequest("Feature values are required.");
        }

        var outcome = await featureService.UpsertFeaturesAsync(houseId, values, ct);
        if (!outcome.HouseFound)
        {
            return NotFound();
        }
        if (outcome.HasMissingFeatures)
        {
            return BadRequest("One or more feature IDs are invalid.");  
        }

        return NoContent();
    }

    [HttpPut("{houseId:guid}/pricing")]
    public async Task<IActionResult> UpsertPricing(Guid houseId, [FromBody] PricePlanDetailsDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await pricingService.UpsertPricingAsync(houseId, dto, ct);

        return result.Status switch
        {
            ServiceResultStatus.Success => NoContent(),
            ServiceResultStatus.NotFound => NotFound(),
            ServiceResultStatus.Invalid => ValidationProblem("Invalid pricing data."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unable to upsert pricing.")
        };
    }

    // House season span management (when house has a group)
    [HttpPost("{houseId:guid}/calendar")]
    public async Task<ActionResult<SeasonSpanDto>> AddSeasonSpan(Guid houseId, [FromBody] UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var result = await houseGroupService.AddHouseSeasonSpanAsync(houseId, dto, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => Created($"/api/admin/houses/{houseId}/calendar/{result.Value!.Id}", result.Value),
            _ => this.FromResult(result),
        };
    }

    [HttpPut("{houseId:guid}/calendar/{spanId:guid}")]
    public async Task<ActionResult<SeasonSpanDto>> UpdateSeasonSpan(Guid houseId, Guid spanId, [FromBody] UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var result = await houseGroupService.UpdateHouseSeasonSpanAsync(houseId, spanId, dto, ct);
        return this.FromResult(result);
    }

    [HttpDelete("{houseId:guid}/calendar/{spanId:guid}")]
    public async Task<ActionResult> DeleteSeasonSpan(Guid houseId, Guid spanId, CancellationToken ct)
    {
        var result = await houseGroupService.DeleteHouseSeasonSpanAsync(houseId, spanId, ct);
        return this.FromResult(result);
    }
}