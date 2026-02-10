using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class AvailabilityBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid HouseId { get; set; }
    public VacationHouse House { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public AvailabilityStatus Status { get; set; } = AvailabilityStatus.Blocked;
    public AvailabilitySource Source { get; set; } = AvailabilitySource.Manual;

    [MaxLength(500)] public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
}
