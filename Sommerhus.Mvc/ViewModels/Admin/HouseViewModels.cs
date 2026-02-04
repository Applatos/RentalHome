using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Shared;
using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Mvc.ViewModels.Admin;

public sealed class HouseEditVm
{
    public UpsertHouseDto House { get; set; } = new();
    public IEnumerable<SelectListItem> Cities { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Areas { get; set; } = Enumerable.Empty<SelectListItem>();
}

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
