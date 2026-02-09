using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Mvc.ViewModels.Admin.Houses;

public sealed class HouseListVm
{
    public required PageResult<AdminHouseListItemDto> Houses { get; init; }
    public string? SearchQuery { get; init; }
    public EntityStatus? StatusFilter { get; init; }
}
