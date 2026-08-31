using BookingSystem.Application.Resources;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Infrastructure.Resources;

public class ResourceService(IResourceRepository resourceRepository) : IResourceService
{
    private static readonly TimeSpan BusinessHoursStart = TimeSpan.FromHours(8);
    private static readonly TimeSpan BusinessHoursEnd = TimeSpan.FromHours(20);

    public Task<IEnumerable<Resource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        resourceRepository.GetAllAsync(cancellationToken);

    public Task<Resource?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        resourceRepository.GetByIdAsync(id, cancellationToken);

    public Task<Resource> CreateAsync(Resource resource, CancellationToken cancellationToken = default) =>
        resourceRepository.CreateAsync(resource, cancellationToken);

    public Task<bool> UpdateAsync(Resource resource, CancellationToken cancellationToken = default) =>
        resourceRepository.UpdateAsync(resource, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        resourceRepository.DeleteAsync(id, cancellationToken);

    public async Task<AddSlotsResult> AddSlotsAsync(int resourceId, IEnumerable<Slot> slots, CancellationToken cancellationToken = default)
    {
        var slotList = slots.ToList();
        var errors = ValidateSlots(slotList);
        if (errors.Count > 0)
        {
            return AddSlotsResult.Failure(errors);
        }

        if (!await resourceRepository.ExistsAsync(resourceId, cancellationToken))
        {
            return AddSlotsResult.NotFound();
        }

        var created = await resourceRepository.AddSlotsAsync(resourceId, slotList, cancellationToken);
        return AddSlotsResult.Success(created);
    }

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
}
