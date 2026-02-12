using System.ComponentModel.DataAnnotations;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Dtos.Shared;

public sealed record BookingDto(
    Guid Id,
    Guid HouseId,
    string HouseTitle,
    string? HouseAddress,
    string UserId,
    string? UserName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Guests,
    string Currency,
    decimal TotalPrice,
    BookingStatus Status,
    string? GuestNote,
    string? OwnerNote,
    Guid? AvailabilityBlockId,
    DateTime? ConfirmedAtUtc,
    DateTime? CancelledAtUtc,
    string? CancelledBy,
    DateTime CreatedAtUtc);

public sealed record BookingListItemDto(
    Guid Id,
    Guid HouseId,
    string HouseTitle,
    string? UserName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Guests,
    string Currency,
    decimal TotalPrice,
    BookingStatus Status,
    DateTime CreatedAtUtc);

public sealed class CreateBookingDto
{
    [Required] public Guid HouseId { get; set; }
    [Required] public DateOnly CheckIn { get; set; }
    [Required] public DateOnly CheckOut { get; set; }
    [Range(1, 50)] public int Guests { get; set; }
    [MaxLength(500)] public string? GuestNote { get; set; }
}

public sealed class UpdateBookingStatusDto
{
    [Required] public BookingStatus Status { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
}
