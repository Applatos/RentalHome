using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Application.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/pricing")]
public sealed class PricingController(IAdminPricingService pricingService) : ControllerBase
{
    [HttpPost("quote")]
    public async Task<ActionResult<PriceQuoteResponseDto>> Quote([FromBody] PriceQuoteRequestDto request, CancellationToken ct)
    {
        var result = await pricingService.QuoteAsync(request, ct);
        return this.FromResult(result);
    }
}
