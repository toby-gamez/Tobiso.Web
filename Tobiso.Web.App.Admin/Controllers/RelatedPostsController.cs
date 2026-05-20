using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Api.Authentication;

namespace Tobiso.Web.App.Admin.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class RelatedPostsController : ControllerBase
{
    private readonly IRelatedPostService _relatedPostService;

    public RelatedPostsController(IRelatedPostService relatedPostService)
    {
        _relatedPostService = relatedPostService;
    }

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

    [HttpGet("diagnostics")]
    public async Task<ActionResult> GetDiagnostics()
    {
        try
        {
            using var context = HttpContext.RequestServices.GetRequiredService<TobisoDbContext>();
            var diagnostics = new
            {
                DatabaseExists = await context.Database.CanConnectAsync(),
                PostsCount = await context.Posts.CountAsync(),
                RelatedPostsTableExists = false,
                RelatedPostsCount = 0,
                Error = (string?)null
            };
            try
            {
                var count = await context.RelatedPosts.CountAsync();
                return Ok(new
                {
                    diagnostics.DatabaseExists,
                    diagnostics.PostsCount,
                    RelatedPostsTableExists = true,
                    RelatedPostsCount = count,
                    diagnostics.Error
                });
            }
            catch (Exception relatedPostEx)
            {
                return Ok(new
                {
                    diagnostics.DatabaseExists,
                    diagnostics.PostsCount,
                    RelatedPostsTableExists = false,
                    RelatedPostsCount = 0,
                    Error = GetFullExceptionDetail(relatedPostEx)
                });
            }
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

    [HttpGet("{id}")]
    public async Task<ActionResult<RelatedPostResponse>> GetRelatedPost(int id)
    {
        try
        {
            var relatedPost = await _relatedPostService.GetById(id);
            if (relatedPost == null)
                return NotFound($"Souvisejíci post s ID {id} nebyl nalezen.");
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
                return BadRequest("Souvisejíci post se nepodařilo vytvořit. Ověřte, že oba posty existují.");

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
                return NotFound($"Souvisejíci post s ID {id} nebyl nalezen nebo se nepodařilo aktualizovat.");

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
                return NotFound($"Souvisejíci post s ID {id} nebyl nalezen.");

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