using BookingSystem.Application.Bookings;
using BookingSystem.Application.Common;
using BookingSystem.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Bookings;

public class BookingService(IBookingRepository bookingRepository) : IBookingService
{
    public async Task<Result<Booking>> CreateAsync(int slotId, DateOnly date, string userId, CancellationToken cancellationToken = default)
    {
        if (date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Result<Booking>.Validation(["Booking date cannot be in the past."]);
        }

        if (!await bookingRepository.SlotExistsAsync(slotId, cancellationToken))
        {
            return Result<Booking>.Validation(["Slot not found."]);
        }

        try
        {
            var created = await bookingRepository.CreateAsync(
                new Booking { SlotId = slotId, Date = date, UserId = userId },
                cancellationToken);
            return Result<Booking>.Success(created);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Result<Booking>.Conflict("This slot is already booked for the given date.");
        }
    }

    public async Task<Result<Booking>> GetByIdAsync(int id, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.GetByIdAsync(id, cancellationToken);
        if (booking is null || (!isAdmin && booking.UserId != userId))
        {
            return Result<Booking>.NotFound();
        }

        return Result<Booking>.Success(booking);
    }

    public Task<IEnumerable<Booking>> GetMyBookingsAsync(string userId, CancellationToken cancellationToken = default) =>
        bookingRepository.GetByUserIdAsync(userId, cancellationToken);
}
