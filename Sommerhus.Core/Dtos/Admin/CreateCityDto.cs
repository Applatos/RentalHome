using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Admin;

public record CreateCityDto(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(10)] string Zip,
    [MaxLength(2000)] string? Text);
