using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Models;

public class VacationHouse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(140)] public string Title { get; set; } = "";
    [MaxLength(240)] public string? Subtitle { get; set; }

    [MaxLength(200)] public string? Address { get; set; }
    [MaxLength(80)] public string? City { get; set; }
    [MaxLength(10)] public string? Zip { get; set; }

    public string? Description { get; set; }
    public string? Facilities { get; set; }

    public Guid? CoverImageId { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<HouseImage> Images { get; set; } = new();
    public List<HouseFeatureValue> HouseFeatures { get; set; } = new();
}
