using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Shared;

public sealed record CalendarDto(
    Guid Id,
    string Name,
    int? Year,
    bool IsTemplate,
    int SpanCount);

public sealed class UpsertCalendarDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    public int? Year { get; set; }

    public bool IsTemplate { get; set; }
}

public sealed class SetCalendarOverrideDto
{
    [Required]
    public Guid CalendarId { get; set; }
}

public sealed class CreateCalendarOverrideDto
{
    public string Name { get; set; } = "";
}
