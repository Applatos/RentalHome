using Microsoft.AspNetCore.Mvc;
using Sommerhus.Application.Public.ZipCodes;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class ZipCodesController(IZipCodeQueryService zipCodes) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<string>> Find([FromQuery(Name = "q")] string query, CancellationToken ct)
        => zipCodes.FindAsync(query, ct);
}
