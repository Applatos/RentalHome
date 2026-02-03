using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Contracts.Dtos.Admin.Pricing;

public record HouseGroupListItemDto(Guid Id, string Name, int HouseCount);

public record HouseGroupDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<SeasonSpanDto> Calendar
);

public record UpsertHouseGroupDto
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = "";
}

public record UpsertSeasonSpanDto
{
    [Required]
    public DateOnly StartDate { get; init; }

    [Required]
    public DateOnly EndDate { get; init; }

    [Required, MaxLength(10)]
    public string Code { get; init; } = "";
}
