using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Admin.Features;

public sealed class FeatureListVm
{
    public IReadOnlyList<FeatureDto> Features { get; init; } = [];
}
