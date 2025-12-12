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
        try
        {
            var relatedPosts = await _relatedPostService.GetAll();
            return Ok(relatedPosts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, GetFullExceptionDetail(ex));
        }
    }

    [AllowAnonymous]
    [HttpGet("by-post/{postId}")]
    public async Task<ActionResult<IList<RelatedPostResponse>>> GetRelatedPostsByPostId(int postId)
    {
        try
        {
            var relatedPosts = await _relatedPostService.GetByPostId(postId);
            return Ok(relatedPosts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, GetFullExceptionDetail(ex));
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<RelatedPostResponse>> GetRelatedPost(int id)
    {
        try
        {
            var relatedPost = await _relatedPostService.GetById(id);
            if (relatedPost == null)
                return NotFound($"Související post s ID {id} nebyl nalezen.");
            return Ok(relatedPost);
        }
        catch (Exception ex)
        {
            return StatusCode(500, GetFullExceptionDetail(ex));
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRelatedPost([FromBody] CreateRelatedPostRequest request)
    {
        try
        {
            if (request.PostId == request.RelatedPostId)
                return BadRequest("Post nemůže odkazovat sám na sebe.");

            var created = await _relatedPostService.Create(request);
            if (created == null)
                return BadRequest("Související post se nepodařilo vytvořit. Ověřte, že oba posty existují.");

            return CreatedAtAction(nameof(GetRelatedPost), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return StatusCode(500, GetFullExceptionDetail(ex));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRelatedPost(int id, [FromBody] UpdateRelatedPostRequest request)
    {
        try
        {
            if (request.PostId == request.RelatedPostId)
                return BadRequest("Post nemůže odkazovat sám na sebe.");

            var updated = await _relatedPostService.Update(id, request);
            if (!updated)
                return NotFound($"Související post s ID {id} nebyl nalezen nebo se nepodařilo aktualizovat.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, GetFullExceptionDetail(ex));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRelatedPost(int id)
    {
        try
        {
            var deleted = await _relatedPostService.Delete(id);
            if (!deleted)
                return NotFound($"Související post s ID {id} nebyl nalezen.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, GetFullExceptionDetail(ex));
        }
    }
    private object GetFullExceptionDetail(Exception ex)
    {
        return new
        {
            Message = ex.Message,
            Type = ex.GetType().FullName,
            Source = ex.Source,
            StackTrace = ex.StackTrace,
            Data = ex.Data,
            InnerException = ex.InnerException != null ? GetFullExceptionDetail(ex.InnerException) : null
        };
    }
}