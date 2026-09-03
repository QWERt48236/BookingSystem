using BookingSystem.Application.Bookings;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Bookings;

public class BookingRepository(ApplicationDbContext dbContext) : IBookingRepository
{
    public async Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        await dbContext.Bookings.AsNoTracking().Where(b => b.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<AdminBookingSummary>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await dbContext.Bookings
            .AsNoTracking()
            .OrderByDescending(b => b.Date)
            .ThenByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(
                dbContext.Users,
                booking => booking.UserId,
                user => user.Id,
                (booking, user) => new AdminBookingSummary(
                    booking.Id,
                    booking.SlotId,
                    booking.Slot.Resource.Name,
                    booking.Slot.StartTime,
                    booking.Slot.EndTime,
                    booking.UserId,
                    user.Email,
                    booking.Date,
                    booking.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<int?> GetSlotResourceIdAsync(int slotId, CancellationToken cancellationToken = default) =>
        await dbContext.Slots
            .Where(s => s.Id == slotId)
            .Select(s => (int?)s.ResourceId)
            .FirstOrDefaultAsync(cancellationToken);
}
