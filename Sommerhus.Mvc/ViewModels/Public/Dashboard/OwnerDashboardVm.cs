using Sommerhus.Core.Dtos.Owner;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Public.Dashboard;

public sealed class OwnerDashboardVm
{
    public string UserName { get; init; } = "";
    public IReadOnlyList<OwnerHouseListItemDto> Houses { get; init; } = [];
    public IReadOnlyList<BookingListItemDto> UpcomingBookings { get; init; } = [];
}
