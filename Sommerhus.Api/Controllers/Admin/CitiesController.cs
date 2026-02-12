using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Services.Admin.Cities;
using Sommerhus.Core.Dtos.Shared;


namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/cities")]
public sealed class CitiesController(IAdminCityService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CityDto>> GetAll(CancellationToken ct)
        => service.GetAllAsync(ct);

    [HttpGet("lookup")]
    public Task<IReadOnlyList<LookupItem>> Lookup(CancellationToken ct)
        => service.GetLookupAsync(ct);
}
