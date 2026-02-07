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
    [Range(0.00, double.MaxValue)] public decimal? NightlyPrice { get; set; }
}
