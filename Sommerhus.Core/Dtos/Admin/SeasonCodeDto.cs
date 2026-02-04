namespace Sommerhus.Core.Dtos.Admin;

public record SeasonCodeDto(
    string Code,
    string? Label,
    string? Color,
    int SortOrder);
