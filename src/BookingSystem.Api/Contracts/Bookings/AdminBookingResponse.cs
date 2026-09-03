namespace BookingSystem.Api.Contracts.Bookings;

public record AdminBookingResponse(
    int Id,
    int SlotId,
    string ResourceName,
    TimeSpan SlotStartTime,
    TimeSpan SlotEndTime,
    string UserId,
    string? UserEmail,
    DateOnly Date,
    DateTime CreatedAt);
