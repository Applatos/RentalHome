using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Owner;
using Sommerhus.Domain.Models;

namespace Sommerhus.Mvc.Services;

public sealed class UserApiClient
{
    private readonly HttpClient http;

    public UserApiClient(HttpClient http) => this.http = http;

    // Bookings
    public Task<ApiResponse<BookingDto?>> CreateBookingAsync(CreateBookingDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<CreateBookingDto, BookingDto?>(http, "api/bookings", dto, ct);

    public Task<ApiResponse<IReadOnlyList<BookingListItemDto>?>> ListBookingsAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<BookingListItemDto>?>(http, "api/bookings", ct);

    public Task<ApiResponse<BookingDto?>> GetBookingAsync(Guid id, CancellationToken ct)
        => ApiHttp.GetAsync<BookingDto?>(http, $"api/bookings/{id}", ct);

    public Task<ApiResponse<object?>> CancelBookingAsync(Guid id, CancellationToken ct)
        => ApiHttp.PostAsync<object, object?>(http, $"api/bookings/{id}/cancel", new { }, ct);

    // Favorites
    public Task<ApiResponse<IReadOnlyList<FavoriteHouseDto>?>> ListFavoritesAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<FavoriteHouseDto>?>(http, "api/me/favorites", ct);

    public Task<ApiResponse<object?>> AddFavoriteAsync(Guid houseId, CancellationToken ct)
        => ApiHttp.PostAsync<object, object?>(http, $"api/me/favorites/{houseId}", new { }, ct);

    public Task<ApiResponse<object?>> RemoveFavoriteAsync(Guid houseId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/me/favorites/{houseId}", ct);

    // Owner bookings
    public Task<ApiResponse<IReadOnlyList<BookingListItemDto>?>> ListOwnerBookingsAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<BookingListItemDto>?>(http, "api/owner/bookings", ct);

    public Task<ApiResponse<object?>> ConfirmBookingAsync(Guid id, CancellationToken ct)
        => ApiHttp.PostAsync<object, object?>(http, $"api/owner/bookings/{id}/confirm", new { }, ct);

    public Task<ApiResponse<object?>> RejectBookingAsync(Guid id, CancellationToken ct)
        => ApiHttp.PostAsync<object, object?>(http, $"api/owner/bookings/{id}/reject", new { }, ct);

    // Owner houses
    public Task<ApiResponse<IReadOnlyList<OwnerHouseListItemDto>?>> ListOwnerHousesAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<OwnerHouseListItemDto>?>(http, "api/owner/houses", ct);
}
