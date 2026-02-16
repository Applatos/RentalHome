using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Admin;

public sealed class UpsertAreaDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NameEn { get; set; }

    public List<Guid> CityIds { get; set; } = new();


    [MaxLength(2000)]
    public string? Description { get; set; } = null;
}