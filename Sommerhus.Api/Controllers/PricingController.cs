using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Sommerhus.Contracts.Dtos.Pricing;
using Sommerhus.Pricing.Abstractions;
using System.Linq;

namespace Sommerhus.Api.Controllers;

[ApiController]
[Route("api/pricing")]
public class PricingController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public PricingController(IConfiguration configuration) => _configuration = configuration;

    [HttpPost("quote")]
    public async Task<ActionResult<PriceQuoteResponseDto>> Quote([FromBody] PriceQuoteRequestDto requestDto, [FromServices] IPricingPipeline pipeline, CancellationToken ct)
    {
        if (!_configuration.GetValue("Pricing:EnabledV1", true))
        {
            return StatusCode(503, "Pricing midlertidigt deaktiveret");
        }

        if (requestDto.Arrival >= requestDto.Departure)
        {
            return BadRequest("Ugyldigt dato-interval");
        }

        var request = new PriceQuoteRequest(requestDto.HouseId, requestDto.Arrival, requestDto.Departure, requestDto.Guests, requestDto.AreaId);
        var result = await pipeline.QuoteAsync(request, ct);

        var response = new PriceQuoteResponseDto(
            result.Currency,
            result.Nights,
            result.Items.Select(i => new PriceQuoteLineItemDto(i.Code, i.Text, i.Amount)).ToList(),
            result.Subtotal,
            result.Tax,
            result.Total);

        return Ok(response);
    }
}