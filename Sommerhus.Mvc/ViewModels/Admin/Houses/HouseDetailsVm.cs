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


    // Tab-specific data (populated only when needed)
    public IReadOnlyList<FeatureDto> AllFeatures { get; init; } = [];
    public string? FeaturesError { get; init; }

    public IReadOnlyList<SeasonCodeDto> SeasonCodes { get; init; } = [];
    public string? SeasonCodesError { get; init; }

    public IReadOnlyList<AuditEntryDto> AuditEntries { get; init; } = [];
    public string? AuditError { get; init; }

    public IReadOnlyList<AuditEntryDto> PricingAuditEntries { get; init; } = [];
    public string? PricingAuditError { get; init; }

    public IReadOnlyList<CalendarDto> AvailableCalendars { get; init; } = [];
}
