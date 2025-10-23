using Microsoft.AspNetCore.Mvc;
using Sommerhus.Contracts.Dtos.Pricing;


namespace Sommerhus.Api.Controllers
{
    [ApiController]
    public class PricingController : ControllerBase
    {
        [HttpPost("api/pricing/quote")]
        public ActionResult<PriceQuoteResult> Quote([FromBody] PriceQuoteRequest req)
            => StatusCode(501); // Not Implemented (WIP)
    }

}
