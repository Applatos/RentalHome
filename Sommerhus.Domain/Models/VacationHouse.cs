using System.ComponentModel.DataAnnotations;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Domain.Models;

public class VacationHouse : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(200)] public string? Address { get; set; }

    [Required] public Guid CityId { get; set; }
    public City City { get; set; } = null!;

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? SearchKeywords { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Draft;
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }

    public ICollection<Area> Areas { get; set; } = new List<Area>();

    public List<HouseImage> Images { get; set; } = new();
    public List<HouseFeatureValue> HouseFeatures { get; set; } = new();

    public Guid? GroupId { get; set; }           // FK → HouseGroup
    public HouseGroup? Group { get; set; }

    public Guid? CalendarOverrideId { get; set; }  // FK → SeasonCalendar (null = use group default)
    public SeasonCalendar? CalendarOverride { get; set; }

    public List<AvailabilityBlock> AvailabilityBlocks { get; set; } = new();

    [MaxLength(450)] public string? OwnerId { get; set; }  // FK → ApplicationUser.Id
}
