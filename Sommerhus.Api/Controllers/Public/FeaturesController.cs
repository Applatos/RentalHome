using Microsoft.AspNetCore.Mvc;
using Sommerhus.Application.Public.Features;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController(IFeatureQueryService features) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<FeatureDetailsDto>> GetAll(CancellationToken ct)
        => features.GetAllAsync(Request, ct);
}
