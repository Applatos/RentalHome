using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Contracts.Dtos.Admin.Features;

public record UpsertFeatureDto(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(50)] string Key,
    [Required] string ValueType,
    [MaxLength(20)] string? Unit,
    [MaxLength(500)] string? IconUrl);
