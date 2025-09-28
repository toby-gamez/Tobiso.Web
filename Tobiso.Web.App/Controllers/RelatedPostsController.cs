using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RelatedPostsController : ControllerBase
{
    private readonly IRelatedPostService _relatedPostService;

    public RelatedPostsController(IRelatedPostService relatedPostService)
    {
        _relatedPostService = relatedPostService;
    }

    [HttpGet("by-post/{postId}")]
    public async Task<ActionResult<IList<RelatedPostResponse>>> GetRelatedPostsByPostId(int postId)
    {
        var relatedPosts = await _relatedPostService.GetByPostId(postId);
        return Ok(relatedPosts);
    }
}