using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Admin;

using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Audit;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/audit")]
public sealed class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<AuditEntryDto>>> Query(
        [FromQuery] string? entity,
        [FromQuery] string? entityId,
        [FromQuery] string? changedBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => Ok(await auditService.QueryAsync(entity, entityId, changedBy, page, pageSize, ct));
}
