namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public record HousesPageDto(
    string Query,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<HouseRowDto> Items);
