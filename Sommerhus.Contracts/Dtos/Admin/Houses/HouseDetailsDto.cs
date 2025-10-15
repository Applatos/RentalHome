using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Contracts.Dtos.Admin.Houses;
// === READ-DTO til Admin (bruges i Edit GET) ===

public record HouseDetailsDto(
    Guid Id,
    string Name,
    Guid CityId,                 // til forvalg i dropdown
    string CityLabel,            // fx "8000 – Aarhus" (kun visning)
    Guid? AreaId,                // valgfri region/område
    string? AreaLabel,           // visningstekst hvis AreaId har værdi
    string? Address,
    string? Description,
    DateTime CreatedUtc,
    IReadOnlyList<FeatureDetailsDto>? Features,
    IReadOnlyList<ImageDto>? Images);

