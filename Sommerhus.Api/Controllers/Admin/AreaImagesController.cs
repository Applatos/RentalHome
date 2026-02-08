using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Dtos.Security;
using Sommerhus.Core.Services.Admin.Images;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
[Route("api/admin/areas/{areaId:guid}/images")]
public sealed class AreaImagesController(IAdminAreaImageService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImageDto>>> List(Guid areaId, CancellationToken ct)
        => this.FromResult(await service.ListAsync(areaId, Request.BaseUrl(), ct));

    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<IReadOnlyList<ImageDto>>> Upload(Guid areaId, [FromForm] IFormFileCollection files, CancellationToken ct)
        => this.FromResult(await service.UploadAsync(areaId, files, Request.BaseUrl(), ct));

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid areaId, Guid imageId, CancellationToken ct)
        => this.FromResult(await service.DeleteAsync(areaId, imageId, ct));
}
