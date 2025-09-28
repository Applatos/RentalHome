namespace Sommerhus.Contracts.Dtos.Shared;

public record ImageDto(
    Guid Id,
    string Url,
    string? Alt,
    string Kind);
