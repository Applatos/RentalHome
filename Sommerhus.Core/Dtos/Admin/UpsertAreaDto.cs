using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Admin;

public record UpsertAreaDto
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public List<Guid> CityIds { get; set; } = new();


    [MaxLength(2000)]
    public string? Description { get; set; } = null;
   
}