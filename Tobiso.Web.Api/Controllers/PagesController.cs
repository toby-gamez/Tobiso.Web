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
        return Ok(await _postService.GetAll());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(int id)
    {
        var post = await _postService.GetById(id);
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

}
