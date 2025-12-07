using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Controllers;

[Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
[Route("api/[controller]")]
[ApiController]
public class AddendumsController : ControllerBase
{
    private readonly IAddendumService _addendumService;

    public AddendumsController(IAddendumService addendumService)
    {
        _addendumService = addendumService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAddendums()
    {
        return Ok(await _addendumService.GetAll());
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAddendum(int id)
    {
        var addendum = await _addendumService.GetById(id);
        if (addendum == null)
            return NotFound();
        return Ok(addendum);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddendum([FromBody] AddendumResponse addendum)
    {
        var created = await _addendumService.Create(addendum);
        if (created == null)
            return BadRequest();
        return CreatedAtAction(nameof(GetAddendum), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAddendum(int id, [FromBody] AddendumResponse addendum)
    {
        if (id != addendum.Id)
            return BadRequest();

        var result = await _addendumService.Update(addendum);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAddendum(int id)
    {
        var result = await _addendumService.Delete(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
