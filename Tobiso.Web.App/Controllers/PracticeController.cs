using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

/// <summary>REST face of the /practice pages (which call the services directly): flashcard decks, attempts, stats.</summary>
[ApiController]
[Route("api/practice")]
public class PracticeController(IQuestionAttemptService attempts, ICategoryService categories) : ControllerBase
{
    private const int DefaultFlashcardCount = 20;
    private const int DefaultSessionCount = 25;

    // Students only: an admin token's "sub" is a config value that could collide with a real user id.
    private int? GetUserId()
    {
        if (!AuthPolicies.IsStudent(User)) return null;
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }

    // GET /api/practice/flashcards?categoryId=&count= - flashcard-eligible questions (the category's whole subtree when
    // given); a signed-in student gets unmastered ones first. Anonymous use is fine.
    [HttpGet("flashcards")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFlashcards([FromQuery] int? categoryId, [FromQuery] int? count)
    {
        List<int>? categoryIds = categoryId.HasValue ? await categories.GetDescendantIds(categoryId.Value) : null;
        var session = await attempts.GetSessionAsync(GetUserId(), categoryIds, count ?? DefaultFlashcardCount,
            preferUnmastered: true, requireFlashcardEligible: true);
        return Ok(session);
    }

    // GET /api/practice/recommended?count= - a session from the student's weakest categories (all categories when
    // there is no history yet), unmastered questions first.
    [HttpGet("recommended")]
    [Authorize(Policy = AuthPolicies.AnyUser)]
    public async Task<IActionResult> GetRecommended([FromQuery] int? count)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var weak = await attempts.GetWeakCategoryIds(userId.Value);
        return Ok(await attempts.GetSessionAsync(userId, weak, count ?? DefaultSessionCount, preferUnmastered: true));
    }

    // GET /api/practice/stats - answered / mastered counts and accuracy
    [HttpGet("stats")]
    [Authorize(Policy = AuthPolicies.AnyUser)]
    public async Task<IActionResult> GetStats()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        return Ok(await attempts.GetStatsAsync(userId.Value));
    }

    // POST /api/practice/attempt  body: { questionId, correct } - one flashcard / self-checked answer
    // (graded quiz answers already reach the server through POST /api/user/points/earn).
    [HttpPost("attempt")]
    [Authorize(Policy = AuthPolicies.AnyUser)]
    public async Task<IActionResult> RecordAttempt([FromBody] PracticeAttemptRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        if (req.QuestionId <= 0) return BadRequest(new { message = "Neplatná otázka." });
        await attempts.RecordAttemptAsync(userId.Value, req.QuestionId, req.Correct);
        return NoContent();
    }
}

public record PracticeAttemptRequest(int QuestionId, bool Correct);
