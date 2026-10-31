using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.App.Controllers;

// Calendar apps can't send a JWT or cookie, so the secret token in the URL is the credential.
[ApiController]
[AllowAnonymous]
public class CalendarFeedController(ICalendarFeedService feedService) : ControllerBase
{
    // GET /api/calendar/{token}.ics - the student's personal feed (global + class + group + own events)
    [HttpGet("api/calendar/{token}.ics")]
    public async Task<IActionResult> GetFeed(string token)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var ics = await feedService.BuildFeedAsync(token, baseUrl);
        if (ics == null) return NotFound();

        // The URL is a secret: keep shared caches and referrers away from it.
        Response.Headers.CacheControl = "private, max-age=300";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return Content(ics, "text/calendar; charset=utf-8");
    }
}
