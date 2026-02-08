using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class City : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Zip { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }

    public List<VacationHouse> Houses { get; set; } = new();
    public List<CityImage> Images { get; set; } = new();

    public ICollection<Area> Areas { get; set; } = new List<Area>();
}
