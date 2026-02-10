using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Dtos.Shared;

public sealed record AvailabilityBlockDto(
    Guid Id,
    Guid HouseId,
    DateOnly StartDate,
    DateOnly EndDate,
    AvailabilityStatus Status,
    AvailabilitySource Source,
    string? Note,
    DateTime CreatedAtUtc,
    string? CreatedBy);

public sealed record UpsertAvailabilityBlockDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public AvailabilityStatus Status { get; init; } = AvailabilityStatus.Blocked;
    public string? Note { get; init; }
}

public sealed record AvailabilityQueryDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
}
