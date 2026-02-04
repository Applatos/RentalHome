using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Admin.Areas;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin.Areas;
using Sommerhus.Core.Dtos.Shared;
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
        => this.FromResult(await service.GetDetailsAsync(id, Request, ct));


    [HttpPost]
    public async Task<ActionResult<AreaDetailsDto>> Create([FromBody] UpsertAreaDto dto, CancellationToken ct)
    {
        var result = await service.CreateAsync(dto, Request, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value),
            ServiceResultStatus.Invalid => ValidationProblem((ValidationProblemDetails)result.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unable to create area.")
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertAreaDto dto, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, dto, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => NoContent(),
            ServiceResultStatus.NotFound => NotFound(),
            ServiceResultStatus.Invalid => ValidationProblem((ValidationProblemDetails)result.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unable to update area.")
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => NoContent(),
            ServiceResultStatus.NotFound => NotFound(),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unable to delete area.")
        };
    }
}
