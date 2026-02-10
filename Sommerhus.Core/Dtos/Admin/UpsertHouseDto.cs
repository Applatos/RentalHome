using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Admin;

public sealed class UpsertHouseDto
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title must be at most 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    public Guid CityId { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [MaxLength(200, ErrorMessage = "Address must be at most 200 characters.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Search keywords must be at most 500 characters.")]
    public string? SearchKeywords { get; set; }

    public List<Guid> AreaIds { get; set; } = new();
}
