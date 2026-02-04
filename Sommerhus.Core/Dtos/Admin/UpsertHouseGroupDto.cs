using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Admin;

public record UpsertHouseGroupDto
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = "";
}
