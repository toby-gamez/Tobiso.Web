using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
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
