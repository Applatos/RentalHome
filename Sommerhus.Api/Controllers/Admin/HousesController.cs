using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Domain.Models;
using Microsoft.AspNetCore.Http;
using Sommerhus.Api.Services.Admin.Houses;
using Sommerhus.Api.Services.Shared;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Pricing.Models;
using System.Numerics;
using System.Reflection.Metadata;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses")]
public sealed class HousesController(AdminHouseService service) : ControllerBase
{
    [HttpGet]
    public Task<PageResult<HouseListItemDto>> Search([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        => service.SearchAsync(query, page, pageSize, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
        => this.FromResult(await service.GetDetailsAsync(id, Request, ct));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertHouseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await service.CreateAsync(dto, ct);
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

        var result = await service.UpdateAsync(id, dto, ct);
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
        var result = await service.DeleteAsync(id, ct);
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

        var outcome = await service.UpsertFeaturesAsync(houseId, values, ct);
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

        var result = await service.UpsertPricingAsync(houseId, dto, ct);

        return result.Status switch
        {
            ServiceResultStatus.Success => NoContent(),
            ServiceResultStatus.NotFound => NotFound(),
            ServiceResultStatus.Invalid => ValidationProblem("Invalid pricing data."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unable to upsert pricing.")
        };
    }

    //private static PricePlanDetailsDto MapPlan(PricePlan plan)
    //{
    //    var rates = plan.SeasonPrices
    //         .OrderBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
    //         .Select(s => new SeasonPriceDto(
    //            s.Id,
    //            s.PricePlanId,
    //            s.Code,
    //            s.NightlyPrice))
    //        .ToList();

    //    return new PricePlanDetailsDto(
    //        plan.Id,
    //        plan.HouseId,
    //        plan.Name,
    //        plan.Currency,
    //        plan.IsActive,
    //        plan.CreatedUtc,
    //        plan.UpdatedUtc,
    //        rates);
    //}

}