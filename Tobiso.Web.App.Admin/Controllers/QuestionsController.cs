using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Admin.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;

    public QuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetQuestionsByPostId(int postId)
    {
        var questions = await _questionService.GetByPostId(postId);
        return Ok(questions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuestion(int id)
    {
        var question = await _questionService.GetById(id);
        if (question == null)
            return NotFound();
        return Ok(question);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        var created = await _questionService.Create(request);
        if (created == null)
            return BadRequest("Otázku se nepodařilo vytvořit.");
        return CreatedAtAction(nameof(GetQuestion), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] UpdateQuestionRequest request)
    {
        if (id != request.Id)
            return BadRequest("Id v URL neodpovídá Id v těle požadavku.");
        
        var updated = await _questionService.Update(request);
        if (!updated)
            return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var deleted = await _questionService.Delete(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}