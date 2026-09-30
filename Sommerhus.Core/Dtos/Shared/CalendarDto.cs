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
    /// <summary>Optional; a blank name gets a default based on the house title.</summary>
    public string? Name { get; set; }

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
    /// <summary>Optional; a blank name gets a default based on the house title.</summary>
    public string? Name { get; set; }
}
