using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security;
using System.Text;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.Helpers;
using Tobiso.Web.Shared.Interfaces;

namespace Tobiso.Web.App.Controllers
{
    [ApiController]
    public class SitemapController : ControllerBase
    {
        private const string BaseUrl = "https://www.tobiso.com";

        private readonly ITobisoAnonymApi _api;
        private readonly IChronicleAxisService _axisService;
        private readonly ILogger<SitemapController> _logger;

        public SitemapController(ITobisoAnonymApi api, IChronicleAxisService axisService, ILogger<SitemapController> logger)
        {
            _api = api;
            _axisService = axisService;
            _logger = logger;
        }

        [HttpGet("sitemap.xml")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetSitemap()
        {
            try
            {
                var posts = await _api.GetPostSummaries();
                var categories = await _api.GetAllCategories();

                var xml = new StringBuilder();
                xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

                AddUrl(xml, "/", "1.0");
                AddUrl(xml, "/categories", "0.9");
                AddUrl(xml, "/latest", "0.9");
                AddUrl(xml, "/calendar", "0.9");
                AddUrl(xml, "/feedback", "0.7");
                AddUrl(xml, "/practice", "0.8");
                AddUrl(xml, "/practice/flashcards", "0.7");
                AddUrl(xml, "/legal", "0.3");

                foreach (var category in categories)
                    AddUrl(xml, $"/categories/{category.Id}", "0.9");

                foreach (var post in posts)
                {
                    var lastMod = new[] { post.LastEdit, post.LastFix, post.CreatedAt }.Max();
                    AddUrl(xml, PostSlug.Url(post.Id, post.FilePath), "0.8", lastMod);
                }

                try
                {
                    foreach (var axis in await _axisService.GetAll())
                        AddUrl(xml, $"/kronika/{axis.Slug}", "0.7");
                }
                catch (Exception ex)
                {
                    // Chronicle is optional; don't fail the whole sitemap.
                    _logger.LogWarning(ex, "Sitemap: failed to load chronicle axes");
                }

                xml.AppendLine("</urlset>");

                return Content(xml.ToString(), "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sitemap generation failed");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static void AddUrl(StringBuilder xml, string path, string priority, DateTime? lastMod = null)
        {
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{SecurityElement.Escape(BaseUrl + path)}</loc>");
            if (lastMod.HasValue)
                xml.AppendLine($"    <lastmod>{lastMod.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}</lastmod>");
            xml.AppendLine($"    <priority>{priority}</priority>");
            xml.AppendLine("  </url>");
        }
    }
}
