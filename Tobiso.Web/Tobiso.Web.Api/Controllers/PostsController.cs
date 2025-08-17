using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Helpers;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.Api.Controllers;


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

    [HttpGet]
    public async Task<IActionResult> GetPosts()
    {
        return Ok(await _postService.GetAll());
    }

    [AllowAnonymous]
    [HttpGet("links")]
    public async Task<IActionResult> GetPostLinks()
    {
        var posts = await _postService.GetLinks();
        return Ok(posts);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(int id)
    {
        var post = await _postService.GetById(id);
        if (post == null)
            return NotFound();
        return Ok(post);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] Tobiso.Web.Shared.DTOs.PostResponse post)
    {
        if (id != post.Id)
            return BadRequest("Id v URL neodpovídá Id v těle požadavku.");
        var updated = await _postService.Update(post);
        if (!updated)
            return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var deleted = await _postService.Delete(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] Tobiso.Web.Shared.DTOs.PostResponse post)
    {
        var created = await _postService.Create(post);
        if (created == null)
            return BadRequest("Post se nepodařilo vytvořit.");
        return CreatedAtAction(nameof(GetPost), new { id = created.Id }, created);
    }

    [HttpPost("upload-md")]
    public async Task<IActionResult> UploadMdFiles([FromQuery] string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return BadRequest("Neplatná cesta ke složce.");
        var uploader = new MdUploader(_postService);
        var posts = await uploader.UploadFromDirectory(directory);
        return Ok(new {
            count = posts.Count,
            titles = posts.Select(p => p.Title).ToList()
        });
    }
}
