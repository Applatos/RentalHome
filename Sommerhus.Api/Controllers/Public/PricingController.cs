using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Admin.Pricing;
using Sommerhus.Core.Dtos.Shared;

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
