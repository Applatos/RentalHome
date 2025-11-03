using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class VacationHouse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(140)] public string Title { get; set; } = "";
    [MaxLength(200)] public string? Address { get; set; }

    [Required] public Guid CityId { get; set; }
    public City City { get; set; } = null!;

    public string? Description { get; set; }
    public string? Facilities { get; set; }

    public Guid? CoverImageId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Area> Areas { get; set; } = new List<Area>();

    public List<HouseImage> Images { get; set; } = new();
    public List<HouseFeatureValue> HouseFeatures { get; set; } = new();

    public Guid? GroupId { get; set; }           // FK → HouseGroup
    public HouseGroup? Group { get; set; } 

}
