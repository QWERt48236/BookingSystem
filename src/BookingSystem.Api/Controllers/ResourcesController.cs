using BookingSystem.Api.Common;
using BookingSystem.Api.Contracts.Resources;
using BookingSystem.Application.Resources;
using BookingSystem.Domain.Constants;
using BookingSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Api.Controllers;

[ApiController]
[Route("api/resources")]
public class ResourcesController(IResourceService resourceService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var resources = await resourceService.GetAllAsync(cancellationToken);
        return Ok(resources.Select(r => new ResourceResponse(r.Id, r.Name)));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var result = await resourceService.GetByIdAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return this.ToActionResult(result);
        }

        var effectiveDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var bookedSlotIds = await resourceService.GetBookedSlotIdsAsync(id, effectiveDate, cancellationToken);

        return this.ToActionResult(result, resource =>
        {
            var slots = resource.Slots.Select(s => new SlotResponse(s.Id, s.StartTime, s.EndTime, bookedSlotIds.Contains(s.Id)));
            return Ok(new ResourceDetailResponse(resource.Id, resource.Name, slots));
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(ResourceRequest request, CancellationToken cancellationToken)
    {
        var result = await resourceService.CreateAsync(new Resource { Name = request.Name }, cancellationToken);

        return this.ToActionResult(result, created =>
            CreatedAtAction(nameof(GetById), new { id = created.Id }, new ResourceResponse(created.Id, created.Name)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, ResourceRequest request, CancellationToken cancellationToken)
    {
        var result = await resourceService.UpdateAsync(new Resource { Id = id, Name = request.Name }, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await resourceService.DeleteAsync(id, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("{id:int}/slots")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddSlots(int id, List<SlotRequest> request, CancellationToken cancellationToken)
    {
        var slots = request.Select(s => new Slot { StartTime = s.StartTime, EndTime = s.EndTime });
        var result = await resourceService.AddSlotsAsync(id, slots, cancellationToken);

        return this.ToActionResult(result, created =>
            CreatedAtAction(nameof(GetById), new { id }, created.Select(s => new SlotResponse(s.Id, s.StartTime, s.EndTime, false))));
    }
}
