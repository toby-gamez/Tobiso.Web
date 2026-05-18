using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Helpers;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Admin.Controllers;

[Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
[Route("api/[controller]")]
[ApiController]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] int? gradeId = null)
    {
        return Ok(await _postService.GetAll(gradeId));
    }

    [AllowAnonymous]
    [HttpGet("summaries")]
    public async Task<IActionResult> GetPostSummaries()
    {
        return Ok(await _postService.GetSummaries());
    }

    [AllowAnonymous]
    [HttpGet("links")]
    public async Task<IActionResult> GetPostLinks()
    {
        return Ok(await _postService.GetLinks());
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(int id, [FromQuery] int? gradeId = null)
    {
        var post = await _postService.GetById(id, gradeId);
        if (post == null) return NotFound();
        return Ok(post);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest req)
    {
        var created = await _postService.Create(req);
        if (created == null) return BadRequest("Post se nepodařilo vytvořit.");
        return CreatedAtAction(nameof(GetPost), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostRequest req)
    {
        var updated = await _postService.UpdateMetadata(id, req);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var deleted = await _postService.Delete(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("upload-md")]
    public async Task<IActionResult> UploadMdFiles([FromQuery] string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return BadRequest("Neplatná cesta ke složce.");
        var uploader = new MdUploader(_postService);
        var posts = await uploader.UploadFromDirectory(directory);
        return Ok(new { count = posts.Count, titles = posts.Select(p => p.Title).ToList() });
    }
}
