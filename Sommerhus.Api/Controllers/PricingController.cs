using Microsoft.AspNetCore.Mvc;
using Sommerhus.Contracts.Dtos.Pricing;


namespace Sommerhus.Api.Controllers
{
    [ApiController]
    public class PricingController : ControllerBase
    {
        //[HttpPost("api/pricing/quote")]
        //public async Task<ActionResult<PriceQuoteResult>> Quote([FromBody] PriceQuoteRequest req, [FromServices] IPricingPipeline pipeline, CancellationToken ct)
        //{
        //    // Feature flag
        //    if (!builder.Configuration.GetValue("Pricing:EnabledV1", true))
        //        return StatusCode(503, "Pricing midlertidigt deaktiveret");

        //    // Basal validering: datoer og min. nætter
        //    if (req.Arrival >= req.Departure) return BadRequest("Ugyldigt dato-interval");

        //    var res = await pipeline.QuoteAsync(req, ct);
        //    return Ok(res);
        //}
    }
}