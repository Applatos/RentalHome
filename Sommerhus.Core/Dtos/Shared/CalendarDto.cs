using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Shared;

public sealed record CalendarDto(
    Guid Id,
    string Name,
    int? Year,
    bool IsTemplate,
    int SpanCount);

public sealed record UpsertCalendarDto
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = "";

    public int? Year { get; init; }

    public bool IsTemplate { get; init; }
}

public sealed record SetCalendarOverrideDto
{
    [Required]
    public Guid CalendarId { get; init; }
}

public sealed record CreateCalendarOverrideDto
{
    public string Name { get; init; } = "";
}
