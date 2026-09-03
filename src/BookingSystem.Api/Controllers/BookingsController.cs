using System.Security.Claims;
using BookingSystem.Api.Common;
using BookingSystem.Api.Contracts.Bookings;
using BookingSystem.Application.Bookings;
using BookingSystem.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(BookingRequest request, CancellationToken cancellationToken)
    {
        var result = await bookingService.CreateAsync(request.SlotId, request.Date, CurrentUserId, cancellationToken);

        return this.ToActionResult(result, created =>
            CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await bookingService.GetByIdAsync(id, CurrentUserId, User.IsInRole(Roles.Admin), cancellationToken);

        return this.ToActionResult(result, booking => Ok(ToResponse(booking)));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var bookings = await bookingService.GetMyBookingsAsync(CurrentUserId, cancellationToken);
        return Ok(bookings.Select(ToResponse));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var bookings = await bookingService.GetAllBookingsAsync(page, pageSize, cancellationToken);
        return Ok(bookings.Select(ToAdminResponse));
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static BookingResponse ToResponse(Domain.Entities.Booking booking) =>
        new(booking.Id, booking.SlotId, booking.UserId, booking.Date, booking.CreatedAt);

    private static AdminBookingResponse ToAdminResponse(AdminBookingSummary summary) =>
        new(summary.Id, summary.SlotId, summary.ResourceName, summary.SlotStartTime, summary.SlotEndTime, summary.UserId, summary.UserEmail, summary.Date, summary.CreatedAt);
}
