namespace Sommerhus.Core.Dtos.Admin;

public record SeasonSpanDto(
    Guid Id, 
    DateOnly StartDate, 
    DateOnly EndDate, 
    string Code,
    string? SeasonName = null,
    string? Color = null);
