using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostVersionsController : ControllerBase
{
    private readonly Tobiso.Api.Infrastructure.Data.TobisoDbContext _context;

    public PostVersionsController(Tobiso.Api.Infrastructure.Data.TobisoDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-post/{postId}")]
    public async Task<IActionResult> GetByPost(int postId)
    {
        var versions = await _context.PostVersions
            .Where(v => v.PostId == postId)
            .Select(v => new PostVersionResponse
            {
                Id = v.Id,
                PostId = v.PostId,
                GradeId = v.GradeId,
                Content = v.Content,
                LastFix = v.LastFix,
                LastEdit = v.LastEdit
            })
            .ToListAsync();
        return Ok(versions);
    }

    [Authorize(AuthenticationSchemes = Tobiso.Api.Authentication.BasicAuthConstants.Scheme)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PostVersionResponse req)
    {
        var entity = new Tobiso.Web.Domain.Entities.PostVersion
        {
            PostId = req.PostId,
            GradeId = req.GradeId,
            Content = req.Content,
            LastFix = req.LastFix,
            LastEdit = req.LastEdit
        };
        _context.PostVersions.Add(entity);
        await _context.SaveChangesAsync();
        req.Id = entity.Id;
        return CreatedAtAction(nameof(GetByPost), new { postId = req.PostId }, req);
    }

    [Authorize(AuthenticationSchemes = Tobiso.Api.Authentication.BasicAuthConstants.Scheme)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PostVersionResponse req)
    {
        var entity = await _context.PostVersions.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Content = req.Content;
        entity.GradeId = req.GradeId;
        entity.LastEdit = req.LastEdit;
        entity.LastFix = req.LastFix;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = Tobiso.Api.Authentication.BasicAuthConstants.Scheme)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.PostVersions.FindAsync(id);
        if (entity == null) return NotFound();
        _context.PostVersions.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
