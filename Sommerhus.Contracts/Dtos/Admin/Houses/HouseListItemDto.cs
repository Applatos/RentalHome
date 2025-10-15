namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public sealed record HouseListItemDto(
    Guid Id,
    string Name,
    string CityLabel,      // fx "8000 – Aarhus"
    string? AreaLabel,     // null hvis ikke sat
    string? CoverUrl,      // lille thumbnail i listen (absolut URL)
    DateTime CreatedUtc
);
