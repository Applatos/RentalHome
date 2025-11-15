using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Contracts.Security;
using Sommerhus.Application.Admin.Images;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
[Route("api/admin/areas/{areaId:guid}/images")]
public sealed class AreaImagesController(IAdminAreaImageService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImageDto>>> List(Guid areaId, CancellationToken ct)
        => this.FromResult(await service.ListAsync(areaId, Request, ct));

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<ImageDto>> Upload(Guid areaId, IFormFile file, CancellationToken ct)
    {
        var result = await service.UploadAsync(areaId, file, Request, ct);
        if (result.Status == ServiceResultStatus.Success)
        {
            return CreatedAtAction(nameof(List), new { areaId }, result.Value);
        }

        return this.FromResult(result);
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid areaId, Guid imageId, CancellationToken ct)
        => this.FromResult(await service.DeleteAsync(areaId, imageId, ct));
}
