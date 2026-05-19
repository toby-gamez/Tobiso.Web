using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;

    public GradesController(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _gradeService.GetAll());
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id)
    {
        var g = await _gradeService.GetById(id);
        if (g == null) return NotFound();
        return Ok(g);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    public async Task<IActionResult> Create([FromBody] CreateGradeRequest req)
    {
        var created = await _gradeService.Create(req);
        if (created == null) return BadRequest("Grade level already exists or invalid request.");
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGradeRequest req)
    {
        var ok = await _gradeService.Update(id, req);
        if (!ok) return BadRequest("Update failed (not found or level conflict).");
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _gradeService.Delete(id);
            if (!ok) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("seed")]
    [AllowAnonymous]
    public async Task<IActionResult> Seed()
    {
        await _gradeService.SeedDefaultsAsync();
        return NoContent();
    }
}
