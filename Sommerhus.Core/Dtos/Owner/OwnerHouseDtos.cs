using System.ComponentModel.DataAnnotations;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Dtos.Owner;

public sealed record OwnerHouseListItemDto(
    Guid Id,
    string Title,
    string? Address,
    string CityName,
    EntityStatus Status,
    int ImageCount,
    DateTime CreatedAtUtc);

public sealed record OwnerUpdateHouseDto
{
    [MaxLength(200)]
    public string? Description { get; init; }

    [MaxLength(500)]
    public string? SearchKeywords { get; init; }
}
