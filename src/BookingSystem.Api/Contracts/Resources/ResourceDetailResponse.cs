namespace BookingSystem.Api.Contracts.Resources;

public record ResourceDetailResponse(int Id, string Name, IEnumerable<SlotResponse> Slots);
