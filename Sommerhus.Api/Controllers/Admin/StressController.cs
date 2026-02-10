using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Data;
using Sommerhus.Core.Dtos.Security;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/stress")]
[Authorize(Roles = AdminRoles.Admin)]
public sealed class StressController(IStressDataGenerator generator, IHostEnvironment env) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<IActionResult> Seed([FromQuery] int houses = 500, CancellationToken ct = default)
    {
        if (!env.IsDevelopment() && !env.IsEnvironment("Testing"))
            return BadRequest("Stress seeding is only available in Development/Testing environments.");

        var options = new StressDataOptions { HouseCount = Math.Clamp(houses, 10, 10_000) };
        var result = await generator.SeedAsync(options, ct);

        return result.Success ? Ok(result) : Conflict(result);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        if (!env.IsDevelopment() && !env.IsEnvironment("Testing"))
            return BadRequest("Stress clearing is only available in Development/Testing environments.");

        var result = await generator.ClearAsync(ct);
        return Ok(result);
    }
}
