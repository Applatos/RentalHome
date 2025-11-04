// Sommerhus.Api/Controllers/Admin/FeaturesController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Application.Admin.Features;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(AuthenticationSchemes = "AdminBasic")]
[Route("api/admin/features")]
public sealed class FeaturesController(IAdminFeatureService service) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<FeatureDetailsDto>> GetAll(CancellationToken ct)
        => await service.GetAllAsync(Request, ct);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertFeatureDto dto, CancellationToken ct)
        => this.FromResult(await service.CreateAsync(dto, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertFeatureDto dto, CancellationToken ct)
        => this.FromResult(await service.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => this.FromResult(await service.DeleteAsync(id, ct));

    // ===== Ikon upload/slet =====

    [HttpPost("{id:guid}/icon")]
    public async Task<IActionResult> UploadIcon(Guid id, IFormFile file, CancellationToken ct)
    {
        var result = await service.UploadIconAsync(id, file, Request, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(new { iconUrl = result.Value }),
        };
    }

    [HttpDelete("{id:guid}/icon")]
    public async Task<IActionResult> DeleteIcon(Guid id, CancellationToken ct)
        => this.FromResult(await service.DeleteIconAsync(id, ct));
}
