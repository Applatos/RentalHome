using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Public.Pricing;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/pricing")]
public sealed class PricingController(IPricingQuoteService quoteService) : ControllerBase
{
    [HttpPost("quote")]
    public async Task<ActionResult<PriceQuoteResponseDto>> Quote([FromBody] PriceQuoteRequestDto request, CancellationToken ct)
    {
        var result = await quoteService.QuoteAsync(request, ct);
        return this.FromResult(result);
    }
}
