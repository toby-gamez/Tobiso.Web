using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Shared.Interfaces;

namespace Tobiso.Web.Api.Controllers;

/// <summary>
/// Controller pro interaktivní cvičení - používá se v Tobiso.Web.App (uživatelská verze)
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class InteractiveExercisesController : ControllerBase
{
    private readonly IInteractiveExerciseService _exerciseService;
    private readonly ILogger<InteractiveExercisesController> _logger;

    public InteractiveExercisesController(
        IInteractiveExerciseService exerciseService,
        ILogger<InteractiveExercisesController> logger)
    {
        _exerciseService = exerciseService ?? throw new ArgumentNullException(nameof(exerciseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Získá všechna aktivní cvičení pro daný článek (veřejné)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetByPostId(int postId)
    {
        try
        {
            var exercises = await _exerciseService.GetByPostIdAsync(postId, includeInactive: false);
            return Ok(exercises);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání cvičení pro článek {PostId}", postId);
            return StatusCode(500, "Chyba při načítání cvičení.");
        }
    }

    /// <summary>
    /// Získá konkrétní cvičení podle ID (veřejné)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var exercise = await _exerciseService.GetByIdAsync(id);
            if (exercise == null)
            {
                return NotFound();
            }
            return Ok(exercise);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání cvičení {Id}", id);
            return StatusCode(500, "Chyba při načítání cvičení.");
        }
    }

    /// <summary>
    /// Validuje řešení od uživatele (veřejné)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{id}/validate")]
    public async Task<IActionResult> ValidateSolution(int id, [FromBody] ValidateSolutionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserSolutionJson))
            {
                return BadRequest("Řešení nesmí být prázdné.");
            }

            var result = await _exerciseService.ValidateSolutionAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při validaci řešení cvičení {Id}", id);
            return StatusCode(500, "Chyba při validaci řešení.");
        }
    }

    /// <summary>
    /// Vytvoří nové cvičení (pouze Admin - vyžaduje autentizaci)
    /// </summary>
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInteractiveExerciseRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Název cvičení je povinný.");
            }

            if (string.IsNullOrWhiteSpace(request.Type))
            {
                return BadRequest("Typ cvičení je povinný.");
            }

            var created = await _exerciseService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření cvičení");
            return StatusCode(500, "Chyba při vytváření cvičení.");
        }
    }

    /// <summary>
    /// Aktualizuje existující cvičení (pouze Admin - vyžaduje autentizaci)
    /// </summary>
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInteractiveExerciseRequest request)
    {
        try
        {
            if (id != request.Id)
            {
                return BadRequest("ID v URL a v těle požadavku se neshodují.");
            }

            var updated = await _exerciseService.UpdateAsync(request);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při aktualizaci cvičení {Id}", id);
            return StatusCode(500, "Chyba při aktualizaci cvičení.");
        }
    }

    /// <summary>
    /// Smaže cvičení (pouze Admin - vyžaduje autentizaci)
    /// </summary>
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _exerciseService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při mazání cvičení {Id}", id);
            return StatusCode(500, "Chyba při mazání cvičení.");
        }
    }

    /// <summary>
    /// Získá správné řešení cvičení (pouze Admin - vyžaduje autentizaci)
    /// </summary>
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    [HttpGet("{id}/solution")]
    public async Task<IActionResult> GetSolution(int id)
    {
        try
        {
            var solution = await _exerciseService.GetSolutionAsync(id);
            if (solution == null)
            {
                return NotFound();
            }

            return Ok(solution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání řešení cvičení {Id}", id);
            return StatusCode(500, "Chyba při načítání řešení.");
        }
    }

    /// <summary>
    /// Získá všechna cvičení pro daný článek včetně neaktivních (pouze Admin)
    /// </summary>
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    [HttpGet("post/{postId}/all")]
    public async Task<IActionResult> GetAllByPostId(int postId)
    {
        try
        {
            var exercises = await _exerciseService.GetByPostIdAsync(postId, includeInactive: true);
            return Ok(exercises);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání všech cvičení pro článek {PostId}", postId);
            return StatusCode(500, "Chyba při načítání cvičení.");
        }
    }
}
