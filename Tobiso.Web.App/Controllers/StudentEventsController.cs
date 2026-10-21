using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

// Separate from EventsController on purpose: that one's class-level [Authorize] is admin-only,
// and ASP.NET Core requires every [Authorize] on the class and the action to pass.
[ApiController]
[Route("api/events")]
[Authorize(Policy = AuthPolicies.AnyUser)]
public class StudentEventsController(IEventService eventService, IClassService classService) : ControllerBase
{
    // Students only: an admin token's "sub" is a config value that could collide with a real user id.
    private async Task<EventViewer?> GetStudentViewer()
    {
        if (!AuthPolicies.IsStudent(User)) return null;
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return null;
        return await classService.GetViewerAsync(userId);
    }

    private IActionResult ToActionResult(StudentEventResult result) => result switch
    {
        StudentEventResult.Ok => NoContent(),
        StudentEventResult.NotFound => NotFound(),
        StudentEventResult.Forbidden => Forbid(),
        StudentEventResult.TooMany => StatusCode(429, "Za poslední den jsi přidal/a příliš mnoho událostí."),
        _ => BadRequest("Neplatná událost.")
    };

    // GET /api/events/mine?startDate=&endDate= - global events plus the student's class, group and personal ones
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyEvents([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var viewer = await GetStudentViewer();
        if (viewer == null) return Forbid();
        return Ok(await eventService.GetByDateRange(startDate, endDate, viewer));
    }

    // POST /api/events/student
    [HttpPost("student")]
    public async Task<IActionResult> Create([FromBody] StudentEventRequest request)
    {
        var viewer = await GetStudentViewer();
        if (viewer == null) return Forbid();
        var (result, created) = await eventService.CreateStudentEvent(viewer, request);
        return result == StudentEventResult.Ok ? Ok(created) : ToActionResult(result);
    }

    // PUT /api/events/student/{id} - only the student's own events
    [HttpPut("student/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] StudentEventRequest request)
    {
        var viewer = await GetStudentViewer();
        if (viewer == null) return Forbid();
        return ToActionResult(await eventService.UpdateStudentEvent(viewer, id, request));
    }

    // DELETE /api/events/student/{id} - only the student's own events
    [HttpDelete("student/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var viewer = await GetStudentViewer();
        if (viewer == null) return Forbid();
        return ToActionResult(await eventService.DeleteStudentEvent(viewer, id));
    }
}
