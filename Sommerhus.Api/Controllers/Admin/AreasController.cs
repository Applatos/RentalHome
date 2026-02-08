using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Admin.Areas;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Security;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
[Route("api/admin/areas")]
public sealed class AreasController(IAdminAreaService service) : ControllerBase
{

    [HttpGet]
    public Task<IReadOnlyList<AreaListItemDto>> GetAll(CancellationToken ct)
        => service.GetAllAsync(ct);


    [HttpGet("lookup")]
    public Task<IReadOnlyList<LookupItem>> Lookup(CancellationToken ct)
        => service.GetLookupAsync(ct);


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AreaDetailsDto>> Get(Guid id, CancellationToken ct)
        => this.FromResult(await service.GetDetailsAsync(id, Request.BaseUrl(), ct));


    [HttpPost]
    public async Task<ActionResult<AreaDetailsDto>> Create([FromBody] UpsertAreaDto dto, CancellationToken ct)
        => this.FromResult(await service.CreateAsync(dto, Request.BaseUrl(), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertAreaDto dto, CancellationToken ct)
        => this.FromResult(await service.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => this.FromResult(await service.DeleteAsync(id, ct));
}
