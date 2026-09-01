using BookingSystem.Application.Common;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Resources;

public interface IResourceService
{
    Task<IEnumerable<Resource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<Resource>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Resource>> CreateAsync(Resource resource, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Resource resource, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<Slot>>> AddSlotsAsync(int resourceId, IEnumerable<Slot> slots, CancellationToken cancellationToken = default);
}
