using Microsoft.AspNetCore.Authorization;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Services.Admin.DemoContent;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/demo-content")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class DemoContentController(IDemoContentImportService service) : ControllerBase
{
    /// <summary>
    /// Creates whatever demo content is missing; running it again changes nothing. Answers 200
    /// even when the import stops part-way, because the report then says what it did before the
    /// error, and that is what the admin needs to see.
    /// </summary>
    [HttpPost("import")]
    public Task<DemoContentImportResultDto> Import(CancellationToken ct)
        => service.ImportAsync(ct);
}
