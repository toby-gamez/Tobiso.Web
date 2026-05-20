using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    // Public endpoint - anyone can submit feedback
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var feedback = await _feedbackService.Create(dto);
        return Ok(feedback);
    }

    // Admin only - get all feedbacks
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllFeedbacks()
    {
        var feedbacks = await _feedbackService.GetAll();
        return Ok(feedbacks);
    }

    // Admin only - get specific feedback
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFeedback(int id)
    {
        var feedback = await _feedbackService.GetById(id);
        if (feedback == null)
            return NotFound();
        return Ok(feedback);
    }

    // Admin only - mark as read
    [Authorize]
    [HttpPut("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _feedbackService.MarkAsRead(id);
        if (!result)
            return NotFound();
        return NoContent();
    }

    // Admin only - delete feedback
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFeedback(int id)
    {
        var result = await _feedbackService.Delete(id);
        if (!result)
            return NotFound();
        return NoContent();
    }
}
