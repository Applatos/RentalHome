using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Admin.Houses;

public sealed class HouseDetailsVm
{
    public required AdminHouseDetailsDto House { get; init; }
    public required IReadOnlyList<SelectListItem> Cities { get; init; }
    public required IReadOnlyList<SelectListItem> Areas { get; init; }
    public required IReadOnlyList<SelectListItem> HouseGroups { get; init; }
    public string ActiveTab { get; init; } = "overview";

    /// <summary>What stops the house from getting a price. Shown on the overview, pricing and calendar tabs.</summary>
    public required HousePricingCheck PricingCheck { get; init; }

    /// <summary>The name of the house's saved group, when it has one.</summary>
    public string? GroupName { get; init; }

    /// <summary>
    /// The saved group and calendar override. They can differ from <see cref="House"/>, which holds
    /// the posted form values after a failed save; warnings and calendar links follow what is saved.
    /// </summary>
    public Guid? SavedGroupId { get; init; }
    public Guid? SavedCalendarOverrideId { get; init; }

    // Tab-specific data (populated only when needed)
    public IReadOnlyList<FeatureDto> AllFeatures { get; init; } = [];
    public string? FeaturesError { get; init; }

    public IReadOnlyList<SeasonCodeDto> SeasonCodes { get; init; } = [];
    public string? SeasonCodesError { get; init; }

    public IReadOnlyList<AvailabilityBlockDto> AvailabilityBlocks { get; init; } = [];
    public string? AvailabilityError { get; init; }
    public DateOnly AvailabilityFrom { get; init; }
    public DateOnly AvailabilityTo { get; init; }

    public IReadOnlyList<AuditEntryDto> AuditEntries { get; init; } = [];
    public string? AuditError { get; init; }

    public IReadOnlyList<AuditEntryDto> PricingAuditEntries { get; init; } = [];
    public string? PricingAuditError { get; init; }

    public IReadOnlyList<CalendarDto> AvailableCalendars { get; init; } = [];
}
