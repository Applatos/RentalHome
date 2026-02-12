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

public sealed class UpsertAvailabilityBlockDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public AvailabilityStatus Status { get; set; } = AvailabilityStatus.Blocked;
    public string? Note { get; set; }
}

public sealed class AvailabilityQueryDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}
