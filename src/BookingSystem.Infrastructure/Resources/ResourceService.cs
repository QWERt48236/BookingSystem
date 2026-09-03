using BookingSystem.Application.Common;
using BookingSystem.Application.Resources;
using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Resources;

public class ResourceService(IResourceRepository resourceRepository) : IResourceService
{
    private static readonly TimeSpan BusinessHoursStart = TimeSpan.FromHours(8);
    private static readonly TimeSpan BusinessHoursEnd = TimeSpan.FromHours(20);

    public Task<IEnumerable<Resource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        resourceRepository.GetAllAsync(cancellationToken);

    public async Task<Result<Resource>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var resource = await resourceRepository.GetByIdAsync(id, cancellationToken);
        return resource is null ? Result<Resource>.NotFound() : Result<Resource>.Success(resource);
    }

    public async Task<Result<Resource>> CreateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        var created = await resourceRepository.CreateAsync(resource, cancellationToken);
        return Result<Resource>.Success(created);
    }

    public async Task<Result> UpdateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        var updated = await resourceRepository.UpdateAsync(resource, cancellationToken);
        return updated ? Result.Success() : Result.NotFound();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await resourceRepository.DeleteAsync(id, cancellationToken);
            return deleted ? Result.Success() : Result.NotFound();
        }
        catch (DbUpdateException)
        {
            return Result.Conflict("Cannot delete a resource that still has slots.");
        }
    }

    public async Task<Result<IEnumerable<Slot>>> AddSlotsAsync(int resourceId, IEnumerable<Slot> slots, CancellationToken cancellationToken = default)
    {
        var slotList = slots.ToList();
        var errors = ValidateSlots(slotList);
        if (errors.Count > 0)
        {
            return Result<IEnumerable<Slot>>.Validation(errors);
        }

        var resource = await resourceRepository.GetByIdAsync(resourceId, cancellationToken);
        if (resource is null)
        {
            return Result<IEnumerable<Slot>>.NotFound();
        }

        var overlapErrors = ValidateNoOverlaps(slotList, resource.Slots);
        if (overlapErrors.Count > 0)
        {
            return Result<IEnumerable<Slot>>.Validation(overlapErrors);
        }

        var created = await resourceRepository.AddSlotsAsync(resourceId, slotList, cancellationToken);
        return Result<IEnumerable<Slot>>.Success(created);
    }

    public Task<IReadOnlySet<int>> GetBookedSlotIdsAsync(int resourceId, DateOnly date, CancellationToken cancellationToken = default) =>
        resourceRepository.GetBookedSlotIdsAsync(resourceId, date, cancellationToken);

    private static List<string> ValidateSlots(IEnumerable<Slot> slots)
    {
        var errors = new List<string>();

        foreach (var slot in slots)
        {
            if (slot.StartTime >= slot.EndTime)
            {
                errors.Add($"Slot {slot.StartTime}-{slot.EndTime}: start time must be before end time.");
                continue;
            }

            if (slot.StartTime < BusinessHoursStart || slot.EndTime > BusinessHoursEnd)
            {
                errors.Add($"Slot {slot.StartTime}-{slot.EndTime}: must be within business hours {BusinessHoursStart}-{BusinessHoursEnd}.");
            }
        }

        return errors;
    }

    private static List<string> ValidateNoOverlaps(IReadOnlyList<Slot> newSlots, IEnumerable<Slot> existingSlots)
    {
        var errors = new List<string>();
        var existingList = existingSlots.ToList();

        for (var i = 0; i < newSlots.Count; i++)
        {
            var slot = newSlots[i];

            for (var j = i + 1; j < newSlots.Count; j++)
            {
                if (Overlaps(slot, newSlots[j]))
                {
                    errors.Add($"Slot {slot.StartTime}-{slot.EndTime} overlaps with slot {newSlots[j].StartTime}-{newSlots[j].EndTime}.");
                }
            }

            foreach (var existing in existingList)
            {
                if (Overlaps(slot, existing))
                {
                    errors.Add($"Slot {slot.StartTime}-{slot.EndTime} overlaps with existing slot {existing.StartTime}-{existing.EndTime}.");
                }
            }
        }

        return errors;
    }

    private static bool Overlaps(Slot a, Slot b) => a.StartTime < b.EndTime && b.StartTime < a.EndTime;
}
