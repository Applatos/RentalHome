namespace Sommerhus.Core.Dtos.Shared;

public record ImageDto(
    Guid Id,
    string Url,
    string? Alt,
    string Kind);
