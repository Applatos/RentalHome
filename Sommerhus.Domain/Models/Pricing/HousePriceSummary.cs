using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models.Pricing;

public sealed class HousePriceSummary
{
    [Key]
    public Guid HouseId { get; set; }

    public decimal? MinNightlyPrice { get; set; }
    public decimal? MaxNightlyPrice { get; set; }

    [MaxLength(4)]
    public string Currency { get; set; } = "DKK";

    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}
