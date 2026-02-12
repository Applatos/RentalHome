using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Admin.Images;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;


namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/houses/{houseId:guid}/images")]
public sealed class HouseImagesController(IAdminHouseImageService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImageDto>>> Get(Guid houseId, CancellationToken ct)
        => this.FromResult(await service.ListAsync(houseId, Request.BaseUrl(), ct));

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid houseId, Guid imageId, CancellationToken ct)
        => this.FromResult(await service.DeleteAsync(houseId, imageId, ct));

    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<IReadOnlyList<ImageDto>>> Upload(Guid houseId, [FromForm] IFormFileCollection files, CancellationToken ct)
        => this.FromResult(await service.UploadAsync(houseId, files, Request.BaseUrl(), ct));

    [HttpPost("{imageId:guid}/set-kind")]
    public async Task<IActionResult> SetKind(Guid houseId, Guid imageId, [FromQuery] ImageKind kind, CancellationToken ct)
        => this.FromResult(await service.SetKindAsync(houseId, imageId, kind, ct));
}
