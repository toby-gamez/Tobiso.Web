using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[Route("api/admin/feedback")]
[ApiController]
[Authorize]
public class AdminFeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public AdminFeedbackController(IFeedbackService feedbackService) => _feedbackService = feedbackService;

    [HttpGet]
    public async Task<IActionResult> GetFeedback(
        [FromQuery] string? platform,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _feedbackService.GetPaged(platform, type, status, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFeedbackById(int id)
    {
        var item = await _feedbackService.GetItemById(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchFeedback(int id, [FromBody] UpdateFeedbackRequest request)
    {
        var result = await _feedbackService.Patch(id, request);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFeedback(int id)
    {
        var deleted = await _feedbackService.Delete(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
