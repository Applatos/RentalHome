using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models.Pricing;

public class PricePlan : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid HouseId { get; set; }

    public string Name { get; set; } = "Standard";
    public string Currency { get; set; } = "DKK";
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }

    public ICollection<SeasonPrice> SeasonPrices { get; set; } = new List<SeasonPrice>();
}
