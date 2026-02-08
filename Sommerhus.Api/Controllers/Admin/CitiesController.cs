using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Services.Admin.Cities;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Security;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
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
