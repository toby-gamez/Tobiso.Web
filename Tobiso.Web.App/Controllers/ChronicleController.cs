using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChronicleController : ControllerBase
{
    private readonly IChronicleAxisService _axisService;
    private readonly IChronicleItemService _itemService;
    private readonly IChronicleCategoryService _categoryService;
    private readonly IChronicleLinkService _linkService;
    private readonly IChroniclePeriodizationService _periodizationService;
    private readonly IChronicleRegionService _regionService;
    private readonly IChroniclePolityService _polityService;
    private readonly IChronicleImportService _importService;

    public ChronicleController(
        IChronicleAxisService axisService,
        IChronicleItemService itemService,
        IChronicleCategoryService categoryService,
        IChronicleLinkService linkService,
        IChroniclePeriodizationService periodizationService,
        IChronicleRegionService regionService,
        IChroniclePolityService polityService,
        IChronicleImportService importService)
    {
        _axisService = axisService;
        _itemService = itemService;
        _categoryService = categoryService;
        _linkService = linkService;
        _periodizationService = periodizationService;
        _regionService = regionService;
        _polityService = polityService;
        _importService = importService;
    }

    // --- Axes ---

    [HttpGet("axes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAxes() => Ok(await _axisService.GetAll());

    [HttpGet("axes/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAxis(string slug)
    {
        var axis = await _axisService.GetBySlug(slug);
        return axis == null ? NotFound() : Ok(axis);
    }

    [HttpGet("axes/{slug}/layout")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLayout(string slug, [FromQuery] ChronicleFilter filter) =>
        Ok(await _axisService.GetLayout(slug, filter));

    [HttpGet("axes/{axisId:int}/entries")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAxisEntries(int axisId) => Ok(await _axisService.GetEntries(axisId));

    [HttpPost("axes")]
    [Authorize]
    public async Task<IActionResult> CreateAxis([FromBody] CreateChronicleAxisRequest request) =>
        Ok(await _axisService.Create(request));

    [HttpPost("axes/entries")]
    [Authorize]
    public async Task<IActionResult> AddAxisEntry([FromBody] CreateChronicleAxisEntryRequest request) =>
        Ok(await _axisService.AddEntry(request));

    [HttpDelete("axes/entries/{id:int}")]
    [Authorize]
    public async Task<IActionResult> RemoveAxisEntry(int id)
    {
        await _axisService.RemoveEntry(id);
        return NoContent();
    }

    // --- Items (events & people) ---

    [HttpGet("items/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItem(string slug)
    {
        var item = await _itemService.GetBySlug(slug);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("items/search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchItems([FromQuery] string? q, [FromQuery] string? type, [FromQuery] int take = 25) =>
        Ok(await _itemService.Search(q, type, take));

    [HttpPost("items")]
    [Authorize]
    public async Task<IActionResult> CreateItem([FromBody] CreateChronicleItemRequest request) =>
        Ok(await _itemService.Create(request));

    [HttpPut("items/{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateChronicleItemRequest request) =>
        Ok(await _itemService.Update(id, request));

    [HttpDelete("items/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteItem(int id)
    {
        await _itemService.Delete(id);
        return NoContent();
    }

    [HttpGet("items/{eventId:int}/people")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventPeople(int eventId) => Ok(await _itemService.GetEventPeople(eventId));

    [HttpGet("people/{personId:int}/events")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPersonEvents(int personId) => Ok(await _itemService.GetPersonEvents(personId));

    [HttpPost("items/event-person")]
    [Authorize]
    public async Task<IActionResult> SetEventPerson([FromBody] SetChronicleEventPersonRequest request) =>
        Ok(await _itemService.SetEventPerson(request));

    [HttpDelete("items/event-person/{eventId:int}/{personId:int}")]
    [Authorize]
    public async Task<IActionResult> RemoveEventPerson(int eventId, int personId)
    {
        await _itemService.RemoveEventPerson(eventId, personId);
        return NoContent();
    }

    [HttpPost("items/sources")]
    [Authorize]
    public async Task<IActionResult> AddSource([FromBody] CreateChronicleSourceRequest request) =>
        Ok(await _itemService.AddSource(request));

    [HttpDelete("items/sources/{id:int}")]
    [Authorize]
    public async Task<IActionResult> RemoveSource(int id)
    {
        await _itemService.RemoveSource(id);
        return NoContent();
    }

    // --- Causality ---

    [HttpGet("items/{itemId:int}/causes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCauses(int itemId) => Ok(await _linkService.GetCauses(itemId));

    [HttpGet("items/{itemId:int}/consequences")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConsequences(int itemId) => Ok(await _linkService.GetConsequences(itemId));

    [HttpPost("links")]
    [Authorize]
    public async Task<IActionResult> CreateLink([FromBody] CreateChronicleLinkRequest request) =>
        Ok(await _linkService.Create(request));

    [HttpDelete("links/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteLink(int id)
    {
        await _linkService.Delete(id);
        return NoContent();
    }

    // --- Categories ---

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories() => Ok(await _categoryService.GetAll());

    [HttpGet("categories/tree")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryTree() => Ok(await _categoryService.GetTree());

    [HttpPost("categories")]
    [Authorize]
    public async Task<IActionResult> CreateCategory([FromBody] CreateChronicleCategoryRequest request) =>
        Ok(await _categoryService.Create(request));

    [HttpPut("categories/{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateChronicleCategoryRequest request) =>
        Ok(await _categoryService.Update(id, request));

    [HttpDelete("categories/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _categoryService.Delete(id);
        return NoContent();
    }

    // --- Periodizations ---

    [HttpGet("periodizations")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPeriodizations() => Ok(await _periodizationService.GetAll());

    [HttpGet("periodizations/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPeriodization(int id)
    {
        var p = await _periodizationService.GetById(id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost("periodizations")]
    [Authorize]
    public async Task<IActionResult> CreatePeriodization([FromBody] CreateChroniclePeriodizationRequest request) =>
        Ok(await _periodizationService.Create(request));

    [HttpPost("periodizations/periods")]
    [Authorize]
    public async Task<IActionResult> AddPeriod([FromBody] CreateChroniclePeriodRequest request) =>
        Ok(await _periodizationService.AddPeriod(request));

    [HttpDelete("periodizations/periods/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeletePeriod(int id)
    {
        await _periodizationService.DeletePeriod(id);
        return NoContent();
    }

    [HttpDelete("periodizations/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeletePeriodization(int id)
    {
        await _periodizationService.Delete(id);
        return NoContent();
    }

    // --- Regions ---

    [HttpGet("regions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRegions() => Ok(await _regionService.GetAll());

    [HttpPost("regions")]
    [Authorize]
    public async Task<IActionResult> CreateRegion([FromBody] CreateChronicleRegionRequest request) =>
        Ok(await _regionService.Create(request));

    [HttpDelete("regions/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        await _regionService.Delete(id);
        return NoContent();
    }

    // --- Polities ---

    [HttpGet("polities")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPolities() => Ok(await _polityService.GetAll());

    [HttpPost("polities")]
    [Authorize]
    public async Task<IActionResult> CreatePolity([FromBody] CreateChroniclePolityRequest request) =>
        Ok(await _polityService.Create(request));

    [HttpPost("polities/territories")]
    [Authorize]
    public async Task<IActionResult> AddPolityTerritory([FromBody] CreateChroniclePolityTerritoryRequest request) =>
        Ok(await _polityService.AddTerritory(request));

    [HttpGet("polities/labels")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPolityLabels([FromQuery] int regionId, [FromQuery] long year) =>
        Ok(await _polityService.GetLabels(regionId, year));

    // --- Import ---

    [HttpPost("import")]
    [Authorize]
    public async Task<IActionResult> Import([FromBody] ChronicleImportRequest request) =>
        Ok(await _importService.Import(request));
}
