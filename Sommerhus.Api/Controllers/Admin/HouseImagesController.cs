using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Application.Admin.Images;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses/{houseId:guid}/images")]
public sealed class HouseImagesController(IAdminHouseImageService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImageDto>>> Get(Guid houseId, CancellationToken ct)
        => this.FromResult(await service.ListAsync(houseId, Request, ct));

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid houseId, Guid imageId, CancellationToken ct)
        => this.FromResult(await service.DeleteAsync(houseId, imageId, ct));

    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<IReadOnlyList<ImageDto>>> Upload(Guid houseId, [FromForm] IFormFileCollection files, CancellationToken ct)
        => this.FromResult(await service.UploadAsync(houseId, files, Request, ct));

    [HttpPost("{imageId:guid}/set-kind")]
    public async Task<IActionResult> SetKind(Guid houseId, Guid imageId, [FromQuery] ImageKind kind, CancellationToken ct)
        => this.FromResult(await service.SetKindAsync(houseId, imageId, kind, ct));
}
