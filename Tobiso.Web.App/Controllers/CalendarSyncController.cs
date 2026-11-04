using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.App.Controllers;

// Settings of the student's personal .ics feed (the feed itself is CalendarFeedController).
[ApiController]
[Route("api/calendar/sync")]
[Authorize(Policy = AuthPolicies.AnyUser)]
public class CalendarSyncController(ICalendarFeedService feedService) : ControllerBase
{
    // Students only: an admin token's "sub" is a config value that could collide with a real user id.
    private int? GetStudentId()
    {
        if (!AuthPolicies.IsStudent(User)) return null;
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    public record CalendarSyncDto(string Url, List<string> Types, bool Duty);
    public record UpdateCalendarSyncRequest(List<string> Types, bool Duty);

    private async Task<CalendarSyncDto> BuildAsync(int userId, string token) =>
        new($"{Request.Scheme}://{Request.Host}/api/calendar/{token}.ics",
            (await feedService.GetTypesAsync(userId)).Select(t => t.ToString()).ToList(),
            await feedService.GetDutyEnabledAsync(userId));

    // GET /api/calendar/sync - the feed URL (created on first use) and what it contains
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        return Ok(await BuildAsync(userId.Value, await feedService.GetOrCreateTokenAsync(userId.Value)));
    }

    // PUT /api/calendar/sync  body: { types: ["Test", "Homework"], duty }
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCalendarSyncRequest request)
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        var types = new HashSet<EventType>();
        foreach (var raw in request.Types ?? [])
        {
            if (!Enum.TryParse<EventType>(raw, ignoreCase: true, out var t)) return BadRequest();
            types.Add(t);
        }
        await feedService.SetTypesAsync(userId.Value, types);
        await feedService.SetDutyEnabledAsync(userId.Value, request.Duty);
        return Ok(await BuildAsync(userId.Value, await feedService.GetOrCreateTokenAsync(userId.Value)));
    }

    // POST /api/calendar/sync/regenerate - new secret URL; the old one stops working
    [HttpPost("regenerate")]
    public async Task<IActionResult> Regenerate()
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        return Ok(await BuildAsync(userId.Value, await feedService.RegenerateTokenAsync(userId.Value)));
    }
}
