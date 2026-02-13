namespace Sommerhus.Core.Dtos.Shared;

public sealed record FavoriteHouseDto(
    Guid HouseId,
    string HouseTitle,
    string? CoverUrl,
    string? City,
    DateTime FavoritedAtUtc);
