using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Controllers;

[Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
[Route("api/[controller]")]
[ApiController]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var allEvents = await _eventService.GetAll();
        return Ok(allEvents);
    }

    [AllowAnonymous]
    [HttpGet("range")]
    public async Task<IActionResult> GetEventsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var eventsInRange = await _eventService.GetByDateRange(startDate, endDate);
        return Ok(eventsInRange);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var eventItem = await _eventService.GetById(id);
        if (eventItem == null)
            return NotFound();
        return Ok(eventItem);
    }

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> SearchEvents([FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return BadRequest("Hledaný výraz nesmí být prázdný.");
            
        var events = await _eventService.Search(searchTerm);
        return Ok(events);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _eventService.Create(request);
        if (created == null)
            return BadRequest("Událost se nepodařilo vytvořit.");
            
        return CreatedAtAction(nameof(GetEvent), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _eventService.Update(id, request);
        if (!updated)
            return NotFound();
            
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var deleted = await _eventService.Delete(id);
        if (!deleted)
            return NotFound();
            
        return NoContent();
    }
}