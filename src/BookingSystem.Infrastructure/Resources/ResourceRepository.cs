using BookingSystem.Application.Resources;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Resources;

public class ResourceRepository(ApplicationDbContext dbContext) : IResourceRepository
{
    public async Task<IEnumerable<Resource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Resources.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Resource?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Resources
            .AsNoTracking()
            .Include(r => r.Slots)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Resource> CreateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        dbContext.Resources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
        return resource;
    }

    public async Task<bool> UpdateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Resources.FirstOrDefaultAsync(r => r.Id == resource.Id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        existing.Name = resource.Name;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Resources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        dbContext.Resources.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Resources.AnyAsync(r => r.Id == id, cancellationToken);

    public async Task<IEnumerable<Slot>> AddSlotsAsync(int resourceId, IEnumerable<Slot> slots, CancellationToken cancellationToken = default)
    {
        var slotList = slots.ToList();
        foreach (var slot in slotList)
        {
            slot.ResourceId = resourceId;
        }

        dbContext.Slots.AddRange(slotList);
        await dbContext.SaveChangesAsync(cancellationToken);
        return slotList;
    }
}
