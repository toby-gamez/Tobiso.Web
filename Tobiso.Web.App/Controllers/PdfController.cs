using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IPdfService _pdfService;
    private readonly IPostService _postService;

    public PdfController(IPdfService pdfService, IPostService postService)
    {
        _pdfService = pdfService;
        _postService = postService;
    }

    [HttpPost("generate")]
    public IActionResult Generate([FromBody] PdfRequestDto req)
    {
        if (req == null || string.IsNullOrEmpty(req.Html))
            return BadRequest("Missing HTML content");

        var fileName = string.IsNullOrEmpty(req.FileName) ? "document.pdf" : req.FileName;
        var bytes = _pdfService.GeneratePdf(req);
        if (bytes == null || bytes.Length == 0) return BadRequest("Failed to generate PDF");

        return new FileContentResult(bytes, "application/pdf") { FileDownloadName = fileName };
    }

    // Generate PDF for a post and return as a downloadable file (no JS required)
    [HttpGet("generate/post/{id}")]
    public async Task<IActionResult> GenerateFromPost(int id, [FromQuery] string? fileName)
    {
        var post = await _postService.GetById(id);
        if (post == null) return NotFound();

        var safeTitle = string.IsNullOrWhiteSpace(post.Title) ? "post" : string.Join('_', post.Title.Split(Path.GetInvalidFileNameChars()).Select(s => s.Trim()).Where(s => s.Length > 0));
        var outputName = string.IsNullOrEmpty(fileName) ? $"{safeTitle}_{DateTime.UtcNow:yyyyMMdd}.pdf" : fileName;

        // Transform content using the same logic as PostDetail.razor
        string contentHtml;
        // Use the content already resolved by PostService.GetById (it returns
        // the appropriate version's content in PostResponse.Content).
        var versionContent = post.Content ?? string.Empty;
        if (!string.IsNullOrEmpty(post.FilePath) && post.FilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            contentHtml = await TransformMarkdownContent(versionContent);
        }
        else
        {
            contentHtml = versionContent;
        }

        // Preserve the original characters in the title (do not HtmlEncode to entities).
        var titleHtml = string.IsNullOrWhiteSpace(post.Title)
            ? string.Empty
            : Markdig.Markdown.ToHtml(post.Title ?? string.Empty);

        // Markdig wraps plain text in <p>..</p>; if so, extract inner text to use inside <h1>.
        if (titleHtml.StartsWith("<p>", StringComparison.OrdinalIgnoreCase) && titleHtml.EndsWith("</p>", StringComparison.OrdinalIgnoreCase))
        {
            titleHtml = titleHtml.Substring(3, titleHtml.Length - 7);
        }

        var wrapper = $"<div><h1>{titleHtml}</h1>{contentHtml}</div>";
        Console.WriteLine($"[PdfController] Generating PDF for post {post.Id} titleLen={post.Title?.Length ?? 0} contentHtmlLen={contentHtml?.Length ?? 0} wrapperLen={wrapper?.Length ?? 0}");
        if (!string.IsNullOrEmpty(contentHtml) && contentHtml.Length > 1000)
        {
            Console.WriteLine($"[PdfController] contentHtml head: {contentHtml.Substring(0, 500).Replace('\n',' ')}");
        }
        var req = new PdfRequestDto { Html = wrapper, FileName = outputName };
        var bytes = _pdfService.GeneratePdf(req);
        if (bytes == null || bytes.Length == 0) return BadRequest("Failed to generate PDF");

        return new FileContentResult(bytes, "application/pdf") { FileDownloadName = outputName };
    }

    private async Task<string> TransformMarkdownContent(string? content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        // Replace markdown mailto links before processing
        content = ReplaceMarkdownMailtoInText(content);
        
        // Convert fraction notation to inline math spans so PDF rendering can
        // pick them up server-side (mirrors client-side MarkdownContent behavior).
        content = ReplaceFractionsInText(content);

        var html = Markdig.Markdown.ToHtml(content);

        // Get all posts for link transformation
        var allPosts = await _postService.GetAll();

        // Fix images: add prefix to src in <img>
        var imgRegex = new Regex("<img\\s+[^>]*src=\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
        html = imgRegex.Replace(html, match =>
        {
            var origSrc = match.Groups[1].Value;
            if (origSrc.Contains("http")) return match.Value;
            if (origSrc.Contains("images"))
            {
                var fullSrc = origSrc.StartsWith("/") ? $"https://www.tobiso.com{origSrc}" : $"https://www.tobiso.com/{origSrc}";
                return match.Value.Replace(origSrc, fullSrc);
            }
            return match.Value;
        });
        
        // Clean up alt attributes: remove newlines and extra whitespace
        html = Regex.Replace(html, @"<img\s+([^>]*)\s+alt=\""([^\""]*)\""", match =>
        {
            var beforeAlt = match.Groups[1].Value;
            var altText = match.Groups[2].Value;
            // Remove newlines and normalize whitespace in alt text
            altText = Regex.Replace(altText, @"\s+", " ").Trim();
            return $"<img {beforeAlt} alt=\"{altText}\"";
        }, RegexOptions.Singleline);
        
        // Convert broken markdown image syntax that Markdig didn't convert: ](images/...) -> <img>
        html = Regex.Replace(html, @"<p>\s*\]\(([^)]+)\)\s*</p>", match =>
        {
            var src = match.Groups[1].Value;
            // Apply same image path transformation
            if (src.Contains("images") && !src.Contains("http"))
            {
                src = src.StartsWith("/") ? $"https://www.tobiso.com{src}" : $"https://www.tobiso.com/{src}";
            }
            return $"<p><img src=\"{src}\" alt=\"\" /></p>";
        }, RegexOptions.IgnoreCase);
        
        html = Regex.Replace(html, @"\]\(([^)]+)\)", match =>
        {
            var src = match.Groups[1].Value;
            if (src.Contains("images") && !src.Contains("http"))
            {
                src = src.StartsWith("/") ? $"https://www.tobiso.com{src}" : $"https://www.tobiso.com/{src}";
                return $"<img src=\"{src}\" alt=\"\" />";
            }
            return match.Value;
        }, RegexOptions.IgnoreCase);

        // Process links
        var regex = new Regex("<a\\s+href=\\\"([^\\\"]+)\\\"(.*?)>(.*?)<\\/a>", RegexOptions.IgnoreCase);
        html = regex.Replace(html, match =>
        {
            var origHref = match.Groups[1].Value;
            var attrs = match.Groups[2].Value;
            var linkText = match.Groups[3].Value;

            if (origHref.Contains("http"))
            {
                return $"<a href=\"{origHref}\" target=\"_blank\" rel=\"noopener noreferrer\"{attrs}>{linkText}</a>";
            }
            if (origHref.Contains("files"))
            {
                var fullUrl = origHref.StartsWith("/") ? $"https://tobiso.com{origHref}" : $"https://tobiso.com/{origHref}";
                return $"<a href=\"{fullUrl}\" target=\"_blank\" rel=\"noopener noreferrer\"{attrs}>{linkText}</a>";
            }
            if (origHref.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var mailto = origHref.Substring("mailto:".Length);
                    var parts = mailto.Split('?', 2);
                    var address = parts[0] ?? "";
                    var encodedQuery = "";
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        encodedQuery = string.Join("&",
                            parts[1].Split('&', StringSplitOptions.RemoveEmptyEntries)
                                .Select(p =>
                                {
                                    var kv = p.Split('=', 2);
                                    var k = kv[0];
                                    var v = kv.Length > 1 ? kv[1] : "";
                                    return $"{Uri.EscapeDataString(k)}={Uri.EscapeDataString(Uri.UnescapeDataString(v))}";
                                })
                        );
                    }
                    var href = "mailto:" + address + (string.IsNullOrEmpty(encodedQuery) ? "" : "?" + encodedQuery);
                    return $"<a href=\"{href}\"{attrs}>{linkText}</a>";
                }
                catch
                {
                    return $"<a href=\"{origHref}\"{attrs}>{linkText}</a>";
                }
            }

            var file = Regex.Replace(origHref, "^(ml-|l-|sl-|hv-|m-|ch-|f-|pr-|z-|li-|geo-)", "");
            file = file.Replace(".html", ".md");
            if (!file.StartsWith("/")) file = "/" + file;
            var postMatch = allPosts.FirstOrDefault(p => p.FilePath.EndsWith(file, StringComparison.OrdinalIgnoreCase));
            if (postMatch != null)
            {
                // In PDF, convert internal links to absolute URLs
                return $"<a href=\"https://tobiso.com/post/{postMatch.Id}\"{attrs}>{linkText}</a>";
            }
            return $"<span style=\"color:gray; text-decoration:line-through;\">{linkText}</span>";
        });

        // Wrap text in dots into div with class intro
        var dotsRegex = new Regex(@"(\.\.\.\s*)(.*?)(\s*\.\.\.)", RegexOptions.Singleline);
        html = dotsRegex.Replace(html, m => $"<div class=\"intro\">{m.Groups[2].Value}</div>");
        var singleDotsRegex = new Regex(@"<p>\s*\.\.\.\s*<\/p>", RegexOptions.Singleline);
        html = singleDotsRegex.Replace(html, "");
        html = html.Replace("...", "");
        html = ConvertMarkdownTablesToHtml(html);
        return html;
    }

    private string ReplaceMarkdownMailtoInText(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains("mailto:", StringComparison.OrdinalIgnoreCase))
            return input ?? string.Empty;

        var mdMailtoRegex = new Regex(@"\[([^\]]+)\]\((mailto:([^\)\s]+)(\?[^\)]*)?)\)", RegexOptions.IgnoreCase);
        return mdMailtoRegex.Replace(input, match =>
        {
            try
            {
                var linkText = match.Groups[1].Value;
                var full = match.Groups[2].Value;
                var mailto = full.Substring("mailto:".Length);
                var parts = mailto.Split('?', 2);
                var address = parts[0] ?? "";
                var encodedQuery = "";
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    encodedQuery = string.Join("&",
                        parts[1].Split('&', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p =>
                            {
                                var kv = p.Split('=', 2);
                                var k = kv[0];
                                var v = kv.Length > 1 ? kv[1] : "";
                                return $"{Uri.EscapeDataString(k)}={Uri.EscapeDataString(Uri.UnescapeDataString(v))}";
                            })
                    );
                }
                var href = "mailto:" + address + (string.IsNullOrEmpty(encodedQuery) ? "" : "?" + encodedQuery);
                return $"<a href=\"{href}\">{linkText}</a>";
            }
            catch
            {
                return match.Value;
            }
        });
    }

    private string ConvertMarkdownTablesToHtml(string html)
    {
        var tableRegex = new Regex(@"<p>(\s*\|.*(?:<br\s*/?>\s*\|.*)+)</p>", RegexOptions.Singleline);

        html = tableRegex.Replace(html, match =>
        {
            var tableContent = match.Groups[1].Value.Trim();
            var lines = Regex.Split(tableContent, @"<br\s*/?>")
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l) && l.Contains("|"))
                .ToList();

            if (lines.Count < 2) return match.Value;

            int separatorIndex = lines.FindIndex(line => Regex.IsMatch(line, @"^\|?(\s*:?-+:?\s*\|)+\s*:?-+:?\s*\|?$"));
            if (separatorIndex < 1) return match.Value;

            var headerLine = lines[separatorIndex - 1];
            var bodyLines = lines.Skip(separatorIndex + 1).ToList();
            var headerCells = ParseConcatenatedRow(headerLine);

            var sb = new System.Text.StringBuilder();
            sb.Append("<table class=\"md-table\">");
            sb.Append("<thead><tr>");
            foreach (var cellContent in headerCells)
            {
                sb.Append($"<th>{cellContent}</th>");
            }
            sb.Append("</tr></thead>");

            sb.Append("<tbody>");
            foreach (var bodyLine in bodyLines)
            {
                var bodyCells = ParseConcatenatedRow(bodyLine);
                sb.Append("<tr>");
                for (int i = 0; i < headerCells.Count; i++)
                {
                    var cellContent = i < bodyCells.Count ? bodyCells[i] : "";
                    sb.Append($"<td>{cellContent}</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");

            return sb.ToString();
        });

        var universalTableRegex = new Regex(@"<p>((?:\|[^\n]+\n)+\|[^\n]+)</p>", RegexOptions.Singleline);
        html = universalTableRegex.Replace(html, match =>
        {
            var tableContent = match.Groups[1].Value.Trim();
            var lines = Regex.Split(tableContent, @"\r?\n|<br\s*/?>")
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l) && l.Contains("|"))
                .ToList();
            if (lines.Count < 2) return match.Value;
            int separatorIndex = lines.FindIndex(line => Regex.IsMatch(line, @"^\|?([\-: ]+\|)+[\-: ]*\|?$"));
            if (separatorIndex < 1) return match.Value;
            var headerLine = lines[separatorIndex - 1];
            var bodyLines = lines.Skip(separatorIndex + 1).ToList();
            var headerCells = ParseConcatenatedRow(headerLine);
            var sb = new System.Text.StringBuilder();
            sb.Append("<table class=\"md-table\">");
            sb.Append("<thead><tr>");
            foreach (var cellContent in headerCells)
                sb.Append($"<th>{cellContent}</th>");
            sb.Append("</tr></thead>");
            sb.Append("<tbody>");
            foreach (var bodyLine in bodyLines)
            {
                var bodyCells = ParseConcatenatedRow(bodyLine);
                sb.Append("<tr>");
                for (int i = 0; i < headerCells.Count; i++)
                {
                    var cellContent = i < bodyCells.Count ? bodyCells[i] : "";
                    sb.Append($"<td>{cellContent}</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        });
        return html;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Fraction replacement (port from MarkdownContent.razor)
    // ──────────────────────────────────────────────────────────────────────────

    private string ReplaceFractionsInText(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var (protectedText, regions) = ProtectCodeRegions(input);

        var fracRegex = new Regex(
            @"(?<![\w/\-])(?:(\{([^}]+)\})|(-?(?:\d+(?:,\d+)?|\p{L})))\s*/\s*(?:(\{([^}]+)\})|(-?(?:\d+(?:,\d+)?|\p{L})))(?![\w/])",
            RegexOptions.CultureInvariant);

        var replaced = fracRegex.Replace(protectedText, match =>
        {
            try
            {
                var num = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                var den = match.Groups[5].Success ? match.Groups[5].Value : match.Groups[6].Value;

                var fracStrikeRx = new Regex(@"#s#(.*?)#s#");
                num = fracStrikeRx.Replace(num, sm => $"\\cancel{{{sm.Groups[1].Value}}}");
                den = fracStrikeRx.Replace(den, sm => $"\\cancel{{{sm.Groups[1].Value}}}");

                if (string.Equals(num, "ano", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(den, "ano", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(num, "ne", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(den, "ne", StringComparison.OrdinalIgnoreCase))
                    return match.Value;

                num = num.Trim().Replace(',', '.');
                den = den.Trim().Replace(',', '.');

                var math = $"\\frac{{{num}}}{{{den}}}";
                return $"<span class='math-inline' data-math=\"{System.Net.WebUtility.HtmlEncode(math)}\"></span>";
            }
            catch
            {
                return match.Value;
            }
        });

        try
        {
            replaced = Regex.Replace(replaced, "-\\s*(<span class='math-inline' data-math=\\\")", "<span class='math-inline' data-math=\"-", RegexOptions.CultureInvariant);
        }
        catch { }

        return RestoreCodeRegions(replaced, regions);
    }

    private (string, Dictionary<string, string>) ProtectCodeRegions(string input)
    {
        var regions = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(input)) return (string.Empty, regions);

        int counter = 0;
        var rx = new Regex("(?ms)(```.*?```)", RegexOptions.Singleline);
        var result = rx.Replace(input, m =>
        {
            var key = $"__PROTECTED_CODE_{counter}__";
            regions[key] = m.Value;
            counter++;
            return key;
        });

        return (result, regions);
    }

    private string RestoreCodeRegions(string input, Dictionary<string, string> regions)
    {
        if (regions == null || regions.Count == 0) return input;
        var result = input;
        foreach (var kv in regions)
            result = result.Replace(kv.Key, kv.Value);
        return result;
    }

    private List<string> ParseConcatenatedRow(string rowText)
    {
        var cleanRowText = rowText;
        if (cleanRowText.StartsWith("|")) cleanRowText = cleanRowText.Substring(1);
        if (cleanRowText.EndsWith("|")) cleanRowText = cleanRowText.Substring(0, cleanRowText.Length - 1);

        return cleanRowText.Split('|', StringSplitOptions.None)
            .Select(s => s.Trim())
            .ToList();
    }
}
