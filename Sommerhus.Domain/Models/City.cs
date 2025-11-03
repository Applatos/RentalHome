using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class City
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Zip { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<VacationHouse> Houses { get; set; } = new();
    public List<CityImage> Images { get; set; } = new();

    public ICollection<Area> Areas { get; set; } = new List<Area>();
}
