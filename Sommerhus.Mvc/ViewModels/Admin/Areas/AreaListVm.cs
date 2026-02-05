using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Admin.Areas;

public sealed class AreaListVm
{
    public IReadOnlyList<AreaListItemDto> Areas { get; init; } = [];
}
