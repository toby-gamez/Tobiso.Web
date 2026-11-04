using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

// Student side of the class duty (the admin edits it in Tobiso.Web.App.Admin): the schedule of the
// student's own class and which roster name they are.
[ApiController]
[Route("api/duty")]
[Authorize(Policy = AuthPolicies.AnyUser)]
public class DutyController(IDutyService dutyService, IClassService classService) : ControllerBase
{
    // Students only: an admin token's "sub" is a config value that could collide with a real user id.
    private int? GetStudentId()
    {
        if (!AuthPolicies.IsStudent(User)) return null;
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    private async Task<int?> GetApprovedClassIdAsync(int userId) =>
        (await classService.GetViewerAsync(userId)).ClassId;

    // GET /api/duty/overview - this and next week of the student's class; 204 without an approved class
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        if (await GetApprovedClassIdAsync(userId.Value) is not int classId) return NoContent();
        return Ok(await dutyService.GetOverviewAsync(classId, DateOnly.FromDateTime(DateTime.Today)));
    }

    // GET /api/duty/weeks - the whole schedule from this week on, up to a year ahead
    [HttpGet("weeks")]
    public async Task<IActionResult> GetWeeks()
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        if (await GetApprovedClassIdAsync(userId.Value) is not int classId) return NoContent();
        var monday = DutyService.MondayOf(DateOnly.FromDateTime(DateTime.Today));
        return Ok(await dutyService.GetWeeksAsync(classId, monday, monday.AddYears(1)));
    }

    // GET /api/duty/me - the picked roster name plus the whole roster; 204 without an approved class
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        var mine = await dutyService.GetMyPersonAsync(userId.Value);
        return mine == null ? NoContent() : Ok(mine);
    }

    public record SetMyDutyPersonRequest(int? DutyPersonId);

    // PUT /api/duty/me  body: { dutyPersonId } (null clears it)
    [HttpPut("me")]
    public async Task<IActionResult> SetMe([FromBody] SetMyDutyPersonRequest request)
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        if (!await dutyService.SetMyPersonAsync(userId.Value, request.DutyPersonId)) return NotFound();
        return Ok(await dutyService.GetMyPersonAsync(userId.Value));
    }
}
