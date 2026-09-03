namespace BookingSystem.Api.Contracts.Resources;

public record SlotResponse(int Id, TimeSpan StartTime, TimeSpan EndTime, bool IsBooked);
