using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ExplanationsController : ControllerBase
{
    private readonly IExplanationService _explanationService;

    public ExplanationsController(IExplanationService explanationService)
    {
        _explanationService = explanationService;
    }

    [AllowAnonymous]
    [HttpGet("question/{questionId}")]
    public async Task<IActionResult> GetExplanationsByQuestionId(int questionId)
    {
        var explanations = await _explanationService.GetByQuestionId(questionId);
        return Ok(explanations);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetExplanation(int id)
    {
        var explanation = await _explanationService.GetById(id);
        if (explanation == null)
            return NotFound();
        return Ok(explanation);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExplanation([FromBody] CreateExplanationRequest request)
    {
        var created = await _explanationService.Create(request);
        if (created == null)
            return BadRequest("Vysvětlení se nepodařilo vytvořit.");
        return CreatedAtAction(nameof(GetExplanation), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExplanation(int id, [FromBody] UpdateExplanationRequest request)
    {
        if (id != request.Id)
            return BadRequest("Id v URL neodpovídá Id v těle požadavku.");
        
        var updated = await _explanationService.Update(request);
        if (!updated)
            return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExplanation(int id)
    {
        var deleted = await _explanationService.Delete(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}