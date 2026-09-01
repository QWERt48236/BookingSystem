namespace BookingSystem.Api.Contracts.Bookings;

public record BookingResponse(int Id, int SlotId, string UserId, DateOnly Date, DateTime CreatedAt);
