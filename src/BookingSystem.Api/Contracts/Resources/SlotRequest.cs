namespace BookingSystem.Api.Contracts.Resources;

public record SlotRequest(TimeSpan StartTime, TimeSpan EndTime);
