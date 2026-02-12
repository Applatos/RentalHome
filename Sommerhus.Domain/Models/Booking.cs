using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class Booking : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid HouseId { get; set; }
    public VacationHouse House { get; set; } = null!;

    [Required, MaxLength(450)] public string UserId { get; set; } = "";

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    public int Guests { get; set; }

    [MaxLength(10)] public string Currency { get; set; } = "DKK";
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [MaxLength(500)] public string? GuestNote { get; set; }
    [MaxLength(500)] public string? OwnerNote { get; set; }

    public Guid? AvailabilityBlockId { get; set; }
    public AvailabilityBlock? AvailabilityBlock { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    [MaxLength(256)] public string? CancelledBy { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }
}
