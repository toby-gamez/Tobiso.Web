using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
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
}
