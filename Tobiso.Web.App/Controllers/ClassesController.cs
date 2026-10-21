using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

// Student side of classes: pick a class (admin approves it in Tobiso.Web.App.Admin) and its groups.
[ApiController]
[Route("api/classes")]
[Authorize(Policy = AuthPolicies.AnyUser)]
public class ClassesController(IClassService classService) : ControllerBase
{
    // Students only: an admin token's "sub" is a config value that could collide with a real user id.
    private int? GetStudentId()
    {
        if (!AuthPolicies.IsStudent(User)) return null;
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    // GET /api/classes
    [HttpGet]
    public async Task<IActionResult> GetClasses() =>
        Ok((await classService.GetAllAsync()).Select(c => c with { ApprovedCount = 0, PendingCount = 0 }));

    // GET /api/classes/mine
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        var mine = await classService.GetMyClassAsync(userId.Value);
        return mine == null ? NoContent() : Ok(mine);
    }

    // POST /api/classes/join  body: { classId, groupIds }
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinClassRequest request)
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        return await classService.RequestJoinAsync(userId.Value, request.ClassId, request.GroupIds)
            ? Ok(await classService.GetMyClassAsync(userId.Value))
            : NotFound();
    }

    // PUT /api/classes/mine/groups  body: { groupIds }
    [HttpPut("mine/groups")]
    public async Task<IActionResult> UpdateGroups([FromBody] UpdateMyGroupsRequest request)
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        return await classService.UpdateMyGroupsAsync(userId.Value, request.GroupIds)
            ? Ok(await classService.GetMyClassAsync(userId.Value))
            : NotFound();
    }

    // DELETE /api/classes/mine
    [HttpDelete("mine")]
    public async Task<IActionResult> Leave()
    {
        var userId = GetStudentId();
        if (userId == null) return Forbid();
        await classService.LeaveAsync(userId.Value);
        return NoContent();
    }
}
