using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Resources;

public class AddSlotsResult
{
    public bool Succeeded { get; init; }
    public bool ResourceNotFound { get; init; }
    public IEnumerable<Slot> Slots { get; init; } = [];
    public IEnumerable<string> Errors { get; init; } = [];

    public static AddSlotsResult Success(IEnumerable<Slot> slots) => new() { Succeeded = true, Slots = slots };
    public static AddSlotsResult NotFound() => new() { Succeeded = false, ResourceNotFound = true };
    public static AddSlotsResult Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors };
}
