namespace Sommerhus.Core.Dtos.Admin;

public record HouseGroupDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<SeasonSpanDto> Calendar
);
