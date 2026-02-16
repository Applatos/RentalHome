using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models.Pricing;

public sealed class PriceQuote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseId { get; set; }

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; }

    public int Nights { get; set; }

    [MaxLength(2000)]
    public string NightlyBreakdown { get; set; } = "[]";

    [MaxLength(2000)]
    public string Modifiers { get; set; } = "[]";

    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    [MaxLength(4)]
    public string Currency { get; set; } = "DKK";

    public Guid? PricePlanId { get; set; }
    public Guid? CalendarId { get; set; }

    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
}
