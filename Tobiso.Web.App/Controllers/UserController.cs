using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[ApiController]
[Route("api/user")]
[Authorize(Policy = AuthPolicies.AnyUser)]
public class UserController(IUserProgressService progressService, IUserDataExportService exportService,
    IGameStateService gameStateService, IPointsWalletService walletService, IStreakService streakService) : ControllerBase
{
    // Students only: an admin token's "sub" is a config value that could collide with a real user id.
    private int? GetUserId()
    {
        if (!AuthPolicies.IsStudent(User)) return null;
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

    // POST /api/user/activity - "active today" for the streak. The server picks the date (Europe/Prague);
    // the client sends nothing. Idempotent per day. Call once when the app (or web) is opened.
    [HttpPost("activity")]
    public async Task<IActionResult> RecordActivity()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await streakService.RecordActivityAsync(userId.Value);
        return NoContent();
    }

    // GET /api/user/continue-reading - most recently read unfinished post, 204 if none
    [HttpGet("continue-reading")]
    public async Task<IActionResult> GetContinueReading()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await progressService.GetContinueReadingAsync(userId.Value);
        return result == null ? NoContent() : Ok(result);
    }

    // GET /api/user/game-state - 204 if the user has never stored any
    [HttpGet("game-state")]
    public async Task<IActionResult> GetGameState()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var state = await gameStateService.GetAsync(userId.Value);
        return state == null ? NoContent() : Ok(state);
    }

    // PUT /api/user/game-state  body: { baseRevision, state } - 409 with the current state on a stale baseRevision
    [HttpPut("game-state")]
    [RequestSizeLimit(GameStateService.MaxStateBytes)]
    public async Task<IActionResult> PutGameState([FromBody] PutGameStateRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        if (req.BaseRevision < 0)
            return BadRequest(new { message = "baseRevision nesmí být záporná." });
        if (req.State is not System.Text.Json.Nodes.JsonObject state)
            return BadRequest(new { message = "state musí být JSON objekt." });

        (Tobiso.Web.Shared.DTOs.GameStateSavedDto? saved, Tobiso.Web.Shared.DTOs.GameStateDto? current) result;
        try { result = await gameStateService.PutAsync(userId.Value, req.BaseRevision, state); }
        catch (InvalidGameStateException ex) { return BadRequest(new { message = ex.Message }); }
        var (saved, current) = result;
        if (saved != null) return Ok(saved);
        return Conflict(new GameStateConflictDto(current?.Revision ?? 0,
            current?.State ?? new System.Text.Json.Nodes.JsonObject()));
    }

    // GET /api/user/points - server-owned exchange points (not the app's local points)
    [HttpGet("points")]
    public async Task<IActionResult> GetPoints()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        return Ok(await walletService.GetAsync(userId.Value));
    }

    // POST /api/user/points/earn  body: { answers: [{ questionId, answerIds?, text? }] }
    // The server grades the answers itself and decides the amount; the client never sends points.
    [HttpPost("points/earn")]
    public async Task<IActionResult> EarnPoints([FromBody] PointsEarnRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        if (req.Answers is not { Count: > 0 } answers || answers.Count > PointsWalletService.MaxAnswersPerRequest)
            return BadRequest(new { message = $"Pošli 1 až {PointsWalletService.MaxAnswersPerRequest} odpovědí." });
        return Ok(await walletService.GradeAndAwardAsync(userId.Value, answers));
    }

    // POST /api/user/points/claim  body: { reason: "ai_credits", ref: "<shop item id>" } - spends exchange points
    [HttpPost("points/claim")]
    public async Task<IActionResult> ClaimPoints([FromBody] PointsClaimRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var (status, result) = await walletService.ClaimAsync(userId.Value, req.Reason, req.Ref);
        return status switch
        {
            PointsClaimStatus.Ok => Ok(result),
            PointsClaimStatus.UnknownItem => BadRequest(new { message = "Neznámá položka." }),
            PointsClaimStatus.InsufficientPoints => Conflict(new { code = "insufficient_points", message = "Nedostatek bodů." }),
            _ => StatusCode(429, new { code = "daily_limit", message = "Denní limit výměny bodů za kredity byl vyčerpán." }),
        };
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

    // GET /api/user/export - downloads all data stored about the signed-in user as JSON
    [HttpGet("export")]
    public async Task<IActionResult> ExportData()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var bytes = await exportService.ExportAsync(userId.Value);
        if (bytes == null) return NotFound();
        return File(bytes, "application/json", $"tobiso-moje-data-{DateTime.UtcNow:yyyyMMdd}.json");
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
