// Sommerhus.Api/Controllers/Admin/FeaturesController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Admin.Features;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Security;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
[Route("api/admin/features")]
public sealed class FeaturesController(IAdminFeatureService service) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<FeatureDto>> GetAll(CancellationToken ct)
        => await service.GetAllAsync(Request.BaseUrl(), ct);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertFeatureDto dto, CancellationToken ct)
        => this.FromResult(await service.CreateAsync(dto, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertFeatureDto dto, CancellationToken ct)
        => this.FromResult(await service.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => this.FromResult(await service.DeleteAsync(id, ct));

    [HttpPost("{id:guid}/icon")]
    public async Task<IActionResult> UploadIcon(Guid id, IFormFile file, CancellationToken ct)
    {
        var result = await service.UploadIconAsync(id, file, Request.BaseUrl(), ct);
        if (result.Status == ServiceResultStatus.Success)
            return Ok(new { iconUrl = result.Value });

        return this.FromResult((ServiceResult)result);
    }

    [HttpDelete("{id:guid}/icon")]
    public async Task<IActionResult> DeleteIcon(Guid id, CancellationToken ct)
        => this.FromResult(await service.DeleteIconAsync(id, ct));
}
