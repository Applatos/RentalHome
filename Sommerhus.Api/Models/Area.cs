using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Models;

public class Area
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(50)]
    public string Name { get; set; } = "";

    [Required, StringLength(140)]
    public string Slug { get; set; } = string.Empty;

    public Guid? CityId { get; set; }
    public City? City { get; set; }

    public string? Description { get; set; }

    public List<VacationHouse> Houses { get; set; } = new();
    public List<AreaImage> AreaImages { get; set; } = new();
}
