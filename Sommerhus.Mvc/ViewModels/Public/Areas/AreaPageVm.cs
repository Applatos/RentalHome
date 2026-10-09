namespace Sommerhus.Mvc.ViewModels.Public.Areas;

/// <summary>An area with its published houses as cards.</summary>
public sealed class AreaPageVm
{
    public required AreaDetailsDto Area { get; init; }

    public IReadOnlyList<PublicHouseListItemDto> Houses { get; init; } = [];
}
