using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Admin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostVersionsController : ControllerBase
{
    private readonly IPostVersionService _versionService;

    public PostVersionsController(IPostVersionService versionService)
    {
        _versionService = versionService;
    }

    [HttpGet("by-post/{postId}")]
    public async Task<IActionResult> GetByPost(int postId)
    {
        return Ok(await _versionService.GetByPost(postId));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVersionRequest req)
    {
        var created = await _versionService.Create(req);
        if (created == null)
            return BadRequest("Version could not be created. Check that the GradeId exists and that this (PostId, GradeId) combination is not already taken.");
        return CreatedAtAction(nameof(GetByPost), new { postId = req.PostId }, created);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVersionRequest req)
    {
        var updated = await _versionService.Update(id, req);
        if (!updated) return NotFound();
        return NoContent();
    }

    [Authorize]
    [HttpPatch("{id}/grade")]
    public async Task<IActionResult> UpdateGrade(int id, [FromBody] UpdateVersionGradeRequest req)
    {
        var updated = await _versionService.UpdateGrade(id, req.GradeId);
        if (!updated) return BadRequest("Grade not found or already used by another version of this post.");
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _versionService.Delete(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
