using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
[Route("api/[controller]")]
[ApiController]
public class RelatedPostsController : ControllerBase
{
    private readonly IRelatedPostService _relatedPostService;

    public RelatedPostsController(IRelatedPostService relatedPostService)
    {
        _relatedPostService = relatedPostService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IList<RelatedPostResponse>>> GetAllRelatedPosts()
    {
        var relatedPosts = await _relatedPostService.GetAll();
        return Ok(relatedPosts);
    }

    [AllowAnonymous]
    [HttpGet("by-post/{postId}")]
    public async Task<ActionResult<IList<RelatedPostResponse>>> GetRelatedPostsByPostId(int postId)
    {
        var relatedPosts = await _relatedPostService.GetByPostId(postId);
        return Ok(relatedPosts);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<RelatedPostResponse>> GetRelatedPost(int id)
    {
        var relatedPost = await _relatedPostService.GetById(id);
        if (relatedPost == null)
            return NotFound($"Souvisejíci post s ID {id} nebyl nalezen.");

        return Ok(relatedPost);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRelatedPost([FromBody] CreateRelatedPostRequest request)
    {
        if (request.PostId == request.RelatedPostId)
            return BadRequest("Post nemůže odkazovat sám na sebe.");

        var created = await _relatedPostService.Create(request);
        if (created == null)
            return BadRequest("Souvisejíci post se nepodařilo vytvořit. Ověřte, že oba posty existují.");

        return CreatedAtAction(nameof(GetRelatedPost), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRelatedPost(int id, [FromBody] UpdateRelatedPostRequest request)
    {
        if (request.PostId == request.RelatedPostId)
            return BadRequest("Post nemůže odkazovat sám na sebe.");

        var updated = await _relatedPostService.Update(id, request);
        if (!updated)
            return NotFound($"Souvisejíci post s ID {id} nebyl nalezen nebo se nepodařilo aktualizovat.");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRelatedPost(int id)
    {
        var deleted = await _relatedPostService.Delete(id);
        if (!deleted)
            return NotFound($"Souvisejíci post s ID {id} nebyl nalezen.");

        return NoContent();
    }
}