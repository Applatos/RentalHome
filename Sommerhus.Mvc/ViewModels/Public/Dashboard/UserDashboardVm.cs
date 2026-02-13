using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Public.Dashboard;

public sealed class UserDashboardVm
{
    public string UserName { get; init; } = "";
    public string Role { get; init; } = "";
    public IReadOnlyList<BookingListItemDto> UpcomingBookings { get; init; } = [];
    public IReadOnlyList<BookingListItemDto> PastBookings { get; init; } = [];
    public IReadOnlyList<FavoriteHouseDto> Favorites { get; init; } = [];
}
