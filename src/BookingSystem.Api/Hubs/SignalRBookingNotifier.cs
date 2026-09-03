using BookingSystem.Application.Bookings;
using Microsoft.AspNetCore.SignalR;

namespace BookingSystem.Api.Hubs;

public class SignalRBookingNotifier(IHubContext<BookingsHub> hubContext) : IBookingNotifier
{
    public Task NotifySlotBookedAsync(int resourceId, int slotId, DateOnly date, int bookingId, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(BookingGroupNames.ForResource(resourceId))
            .SendAsync("SlotBooked", new SlotBookedPayload(resourceId, slotId, date, bookingId), cancellationToken);
}
