using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Bookings;

public interface IBookingRepository
{
    Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<int?> GetSlotResourceIdAsync(int slotId, CancellationToken cancellationToken = default);
}
