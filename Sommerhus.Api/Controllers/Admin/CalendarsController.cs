using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;

using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Calendars;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/calendars")]
public sealed class CalendarsController(IAdminCalendarService calendarService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CalendarDto>>> List(CancellationToken ct)
        => this.FromResult(await calendarService.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CalendarDto>> Get(Guid id, CancellationToken ct)
        => this.FromResult(await calendarService.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CalendarDto>> Create([FromBody] UpsertCalendarDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.FromResult(await calendarService.CreateAsync(dto, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CalendarDto>> Update(Guid id, [FromBody] UpsertCalendarDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return this.FromResult(await calendarService.UpdateAsync(id, dto, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => this.FromResult(await calendarService.DeleteAsync(id, ct));
}
