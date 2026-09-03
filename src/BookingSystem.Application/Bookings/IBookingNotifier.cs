namespace BookingSystem.Application.Bookings;

public interface IBookingNotifier
{
    Task NotifySlotBookedAsync(int resourceId, int slotId, DateOnly date, int bookingId, CancellationToken cancellationToken = default);
}
