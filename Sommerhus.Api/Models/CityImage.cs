// Sommerhus.Api/Models/CityImage.cs
using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Models;

public class CityImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CityId { get; set; }

    [Required, MaxLength(200)]
    public string FileName { get; set; } = "";

    public int SortOrder { get; set; }

    public City? City { get; set; }
}
