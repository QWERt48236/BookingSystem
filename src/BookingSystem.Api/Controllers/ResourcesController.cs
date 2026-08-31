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
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var resource = await resourceService.GetByIdAsync(id, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        var slots = resource.Slots.Select(s => new SlotResponse(s.Id, s.StartTime, s.EndTime));
        return Ok(new ResourceDetailResponse(resource.Id, resource.Name, slots));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(ResourceRequest request, CancellationToken cancellationToken)
    {
        var created = await resourceService.CreateAsync(new Resource { Name = request.Name }, cancellationToken);
        var response = new ResourceResponse(created.Id, created.Name);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, ResourceRequest request, CancellationToken cancellationToken)
    {
        var updated = await resourceService.UpdateAsync(new Resource { Id = id, Name = request.Name }, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await resourceService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/slots")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddSlots(int id, List<SlotRequest> request, CancellationToken cancellationToken)
    {
        var slots = request.Select(s => new Slot { StartTime = s.StartTime, EndTime = s.EndTime });
        var result = await resourceService.AddSlotsAsync(id, slots, cancellationToken);
        if (result.ResourceNotFound)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var response = result.Slots.Select(s => new SlotResponse(s.Id, s.StartTime, s.EndTime));
        return CreatedAtAction(nameof(GetById), new { id }, response);
    }
}
