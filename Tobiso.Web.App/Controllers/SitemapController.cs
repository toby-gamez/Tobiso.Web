using Microsoft.AspNetCore.Mvc;
using System.Text;
using Tobiso.Web.Shared.Interfaces;

namespace Tobiso.Web.App.Controllers
{
    [ApiController]
    public class SitemapController : ControllerBase
    {
        private readonly ITobisoAnonymApi _api;

        public SitemapController(ITobisoAnonymApi api)
        {
            _api = api;
        }

        [HttpGet("sitemap.xml")]
        public async Task<IActionResult> GetSitemap()
        {
            try
            {
                var xml = new StringBuilder();
                xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
                
                // Domovská stránka
                xml.AppendLine("  <url>");
                xml.AppendLine("    <loc>https://www.tobiso.com/</loc>");
                xml.AppendLine("    <priority>1.0</priority>");
                xml.AppendLine("  </url>");
                
                // Získání všech postů
                var posts = await _api.GetAllPosts();
                
                // Přidání hlavní stránky pro posty
                xml.AppendLine("  <url>");
                xml.AppendLine("    <loc>https://www.tobiso.com/post</loc>");
                xml.AppendLine("    <priority>0.80</priority>");
                xml.AppendLine("  </url>");
                
                // Přidání jednotlivých postů
                foreach (var post in posts)
                {
                    xml.AppendLine("  <url>");
                    xml.AppendLine($"    <loc>https://www.tobiso.com/post/{post.Id}</loc>");
                    xml.AppendLine("    <priority>0.80</priority>");
                    xml.AppendLine("  </url>");
                }
                
                // Získání všech kategorií
                try
                {
                    var categories = await _api.GetAllCategories();
                    
                    // Přidání hlavní stránky pro kategorie
                    xml.AppendLine("  <url>");
                    xml.AppendLine("    <loc>https://www.tobiso.com/categories</loc>");
                    xml.AppendLine("    <priority>0.90</priority>");
                    xml.AppendLine("  </url>");
                    
                    // Přidání jednotlivých kategorií
                    foreach (var category in categories)
                    {
                        xml.AppendLine("  <url>");
                        xml.AppendLine($"    <loc>https://www.tobiso.com/categories/{category.Id}</loc>");
                        xml.AppendLine("    <priority>0.90</priority>");
                        xml.AppendLine("  </url>");
                    }
                }
                catch
                {
                    // ALTERNATIVA: Pokud GetAllCategories() neexistuje nebo selže
                    var categoryIds = posts.Where(p => p.CategoryId.HasValue)
                                          .Select(p => p.CategoryId.Value)
                                          .Distinct()
                                          .ToList();
                    
                    // Přidání hlavní stránky pro kategorie
                    xml.AppendLine("  <url>");
                    xml.AppendLine("    <loc>https://www.tobiso.com/categories</loc>");
                    xml.AppendLine("    <priority>0.90</priority>");
                    xml.AppendLine("  </url>");
                    
                    // Přidání jednotlivých kategorií
                    foreach (var categoryId in categoryIds)
                    {
                        xml.AppendLine("  <url>");
                        xml.AppendLine($"    <loc>https://www.tobiso.com/categories/{categoryId}</loc>");
                        xml.AppendLine("    <priority>0.90</priority>");
                        xml.AppendLine("  </url>");
                    }
                }
                
                xml.AppendLine("</urlset>");
                
                return Content(xml.ToString(), "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return Content($"Chyba při generování sitemap: {ex.Message}", "text/plain");
            }
        }
    }
}