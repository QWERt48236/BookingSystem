namespace BookingSystem.Api.Contracts.Bookings;

public record BookingRequest(int SlotId, DateOnly Date);
