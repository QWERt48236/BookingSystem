using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Resources;

public interface IResourceService
{
    Task<IEnumerable<Resource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Resource?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Resource> CreateAsync(Resource resource, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Resource resource, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<AddSlotsResult> AddSlotsAsync(int resourceId, IEnumerable<Slot> slots, CancellationToken cancellationToken = default);
}
