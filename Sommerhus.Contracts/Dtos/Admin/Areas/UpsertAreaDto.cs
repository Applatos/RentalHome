using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record UpsertAreaDto(
    [Required, MaxLength(100)] string Name,
    IReadOnlyCollection<Guid> CityIds,
    [MaxLength(2000)] string? Description = null,
    IReadOnlyList<string>? Images = null);
