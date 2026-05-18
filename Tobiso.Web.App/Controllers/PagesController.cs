using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PagesController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ICategoryService _categoryService;

    public PagesController(IPostService postService, ICategoryService categoryService)
    {
        _postService = postService;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] int? gradeId = null)
    {
        return Ok(await _postService.GetAll(gradeId));
    }

    [HttpGet("summaries")]
    public async Task<IActionResult> GetPostSummaries()
    {
        return Ok(await _postService.GetSummaries());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(int id, [FromQuery] int? gradeId = null)
    {
        var post = await _postService.GetById(id, gradeId);
        if (post == null) return NotFound();
        return Ok(post);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        return Ok(await _categoryService.GetAll());
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetCategoryTree()
    {
        return Ok(await _categoryService.GetTree());
    }

    [HttpGet("categories/ancestors/{categoryId}")]
    public async Task<IActionResult> GetCategoryAncestors(int categoryId)
    {
        return Ok(await _categoryService.GetAncestors(categoryId));
    }
}
