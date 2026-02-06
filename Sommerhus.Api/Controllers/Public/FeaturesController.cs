using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Public.Features;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class FeaturesController(IFeatureQueryService features) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<FeatureDto>> GetAll(CancellationToken ct)
        => features.GetAllAsync(Request.BaseUrl(), ct);
}
