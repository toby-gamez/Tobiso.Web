using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Controllers;

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
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        return Ok(await _categoryService.GetAll());
    }

    [HttpGet("tree")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryTree()
    {
        return Ok(await _categoryService.GetTree());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryResponse category)
    {
        var result = await _categoryService.Create(category);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryResponse category)
    {
        var result = await _categoryService.Update(id, category);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _categoryService.Delete(id);
        return NoContent();
    }
}
