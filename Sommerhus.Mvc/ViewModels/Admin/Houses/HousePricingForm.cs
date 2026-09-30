using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Mvc.ViewModels.Admin.Houses;

public class HousePricingForm
{
    public Guid? PlanId { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Required, StringLength(3)]
    public string Currency { get; set; } = "DKK";

    public bool IsActive { get; set; }

    public List<SeasonPriceRow> SeasonPrices { get; set; } = new();
}

public class SeasonPriceRow
{
    public Guid? Id { get; set; }
    public Guid? RatePlanId { get; set; }
    public string Code { get; set; } = string.Empty;
    // A night must cost something: a zero price would be quoted as a free night. Empty removes the price.
    [Range(0.01, 1_000_000d, ErrorMessage = "Pages.AdminHouse.Pricing.PriceRange")]
    public decimal? NightlyPrice { get; set; }
}
