using BookingSystem.Application.Common;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Bookings;

public interface IBookingService
{
    Task<Result<Booking>> CreateAsync(int slotId, DateOnly date, string userId, CancellationToken cancellationToken = default);
    Task<Result<Booking>> GetByIdAsync(int id, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetMyBookingsAsync(string userId, CancellationToken cancellationToken = default);
}
