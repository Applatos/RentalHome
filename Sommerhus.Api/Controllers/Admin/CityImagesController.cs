using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Dtos.Security;
using Sommerhus.Core.Services.Admin.Images;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
[Route("api/admin/cities/{cityId:guid}/images")]
public sealed class CityImagesController(IAdminCityImageService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImageDto>>> List(Guid cityId, CancellationToken ct)
        => this.FromResult(await service.ListAsync(cityId, Request.BaseUrl(), ct));

    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ImageDto>> Upload(Guid cityId, IFormFile file, [FromForm] string? alt, CancellationToken ct)
        => this.FromResult(await service.UploadAsync(cityId, file, alt, Request.BaseUrl(), ct));

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid cityId, Guid imageId, CancellationToken ct)
        => this.FromResult(await service.DeleteAsync(cityId, imageId, ct));
}
