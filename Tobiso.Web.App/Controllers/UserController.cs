using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.App.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController(IUserProgressService progressService) : ControllerBase
{
    private int? GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    // POST /api/user/read-progress  body: { postId, scrollPercent }
    [HttpPost("read-progress")]
    public async Task<IActionResult> UpdateReadProgress([FromBody] ReadProgressRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await progressService.UpsertReadProgressAsync(userId.Value, req.PostId, req.ScrollPercent);
        return Ok();
    }

    // GET /api/user/bookmarks
    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetBookmarks()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        return Ok(await progressService.GetBookmarkIdsAsync(userId.Value));
    }

    // POST /api/user/bookmarks/{postId}
    [HttpPost("bookmarks/{postId:int}")]
    public async Task<IActionResult> AddBookmark(int postId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await progressService.AddBookmarkAsync(userId.Value, postId);
        return Ok();
    }

    // DELETE /api/user/bookmarks/{postId}
    [HttpDelete("bookmarks/{postId:int}")]
    public async Task<IActionResult> RemoveBookmark(int postId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await progressService.RemoveBookmarkAsync(userId.Value, postId);
        return Ok();
    }

    // GET /api/user/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        return Ok(await progressService.GetStatsAsync(userId.Value));
    }
}

public record ReadProgressRequest(int PostId, int ScrollPercent);
