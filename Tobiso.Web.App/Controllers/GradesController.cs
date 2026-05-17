using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;

    public GradesController(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGrades()
    {
        return Ok(await _gradeService.GetAll());
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedGrades()
    {
        await _gradeService.SeedDefaultsAsync();
        return NoContent();
    }
}
