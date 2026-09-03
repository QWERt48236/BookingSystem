namespace BookingSystem.Application.Bookings;

public record AdminBookingSummary(
    int Id,
    int SlotId,
    string ResourceName,
    TimeSpan SlotStartTime,
    TimeSpan SlotEndTime,
    string UserId,
    string? UserEmail,
    DateOnly Date,
    DateTime CreatedAt);
