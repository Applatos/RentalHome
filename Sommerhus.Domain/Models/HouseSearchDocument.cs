using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public sealed class HouseSearchDocument
{
    [Key]
    public Guid HouseId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Summary { get; set; }

    [MaxLength(100)]
    public string? CityName { get; set; }

    [MaxLength(20)]
    public string? CityZip { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? AreaNames { get; set; }

    [MaxLength(500)]
    public string? SearchKeywords { get; set; }

    [MaxLength(8000)]
    public string? FeatureJson { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Draft;

    public decimal? MinNightlyPrice { get; set; }

    public decimal? MaxNightlyPrice { get; set; }

    [MaxLength(4)]
    public string? Currency { get; set; }

    public int? Bedrooms { get; set; }

    public int? MaxGuests { get; set; }

    public bool HasPool { get; set; }

    public bool PetFriendly { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [Required, MaxLength(8000)]
    public string SearchVector { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
