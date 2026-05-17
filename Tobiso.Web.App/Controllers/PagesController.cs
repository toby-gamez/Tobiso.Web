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
    public async Task<IActionResult> GetPosts()
    {
        // optional gradeId query parameter
        var gradeIdStr = HttpContext.Request.Query["gradeId"].FirstOrDefault();
        int? gradeId = null;
        if (int.TryParse(gradeIdStr, out var g)) gradeId = g;
        return Ok(await _postService.GetAll(gradeId));
    }

    [HttpGet("summaries")]
    public async Task<IActionResult> GetPostSummaries()
    {
        return Ok(await _postService.GetSummaries());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(int id)
    {
        var gradeIdStr = HttpContext.Request.Query["gradeId"].FirstOrDefault();
        int? gradeId = null;
        if (int.TryParse(gradeIdStr, out var g)) gradeId = g;
        var post = await _postService.GetById(id, gradeId);
        if (post == null)
            return NotFound();
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
        var ancestors = await _categoryService.GetAncestors(categoryId);
        return Ok(ancestors);
    }

}
