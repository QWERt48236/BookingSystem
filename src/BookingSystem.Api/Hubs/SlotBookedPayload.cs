namespace BookingSystem.Api.Hubs;

public record SlotBookedPayload(int ResourceId, int SlotId, DateOnly Date, int BookingId);
