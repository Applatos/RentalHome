using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Admin;

public record UpsertSeasonSpanDto
{
    [Required]
    public DateOnly StartDate { get; init; }

    [Required]
    public DateOnly EndDate { get; init; }

    [Required, MaxLength(10)]
    public string Code { get; init; } = "";
}
