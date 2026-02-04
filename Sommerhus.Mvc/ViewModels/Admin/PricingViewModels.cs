using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Mvc.ViewModels.Admin;

public sealed class PricingAdminVm
{
    public IReadOnlyList<LookupItem> Groups { get; init; } = Array.Empty<LookupItem>();
    public IReadOnlyList<SeasonCodeDto> SeasonCodes { get; init; } = Array.Empty<SeasonCodeDto>();
    public CreateHouseGroupForm GroupForm { get; init; } = new();
    public CreateSeasonCodeForm SeasonCodeForm { get; init; } = new();
    public string? GroupError { get; init; }
    public string? SeasonError { get; init; }
}

public sealed class CreateHouseGroupForm
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateSeasonCodeForm
{
    [Required, StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Label { get; set; }

    [StringLength(7)]
    public string? Color { get; set; }

    [Range(0, 1000)]
    public int SortOrder { get; set; }
}
