namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public sealed record HouseListItemDto(
    Guid Id,
    string Name,
    string CityLabel,      // fx "8000 – Aarhus"
    IReadOnlyList<string> AreaLabels,    
    string? CoverUrl,      // lille thumbnail i listen (absolut URL)
    DateTime CreatedUtc
);
