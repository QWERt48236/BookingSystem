using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookingSystem.Api.Hubs;

[Authorize]
public class BookingsHub : Hub
{
    public Task JoinResourceGroup(int resourceId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, BookingGroupNames.ForResource(resourceId));

    public Task LeaveResourceGroup(int resourceId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, BookingGroupNames.ForResource(resourceId));
}
