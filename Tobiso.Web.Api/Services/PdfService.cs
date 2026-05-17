using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services
{
    public interface IPdfService
    {
        byte[] GeneratePdf(Tobiso.Web.Shared.DTOs.PdfRequestDto request);
    }

    public class PdfService : IPdfService
    {
        public PdfService()
        {
            // Configure QuestPDF license to Community so library skips paid-only checks at runtime
            // (required by QuestPDF to avoid the runtime exception in environments without a license file)
            try
            {
                // Try to set license via reflection in a robust way so we avoid compile-time dependency
                // on a specific enum/type name across QuestPDF versions.
                var settingsType = typeof(QuestPDF.Settings);
                var licenseProp = settingsType.GetProperty("License");
                if (licenseProp != null)
                {
                    var propType = licenseProp.PropertyType;
                    if (propType.IsEnum)
                    {
                        var names = Enum.GetNames(propType);
                        if (names.Contains("Community"))
                        {
                            var enumValue = Enum.Parse(propType, "Community");
                            licenseProp.SetValue(null, enumValue);
                        }
                    }
                    else if (propType == typeof(string))
                    {
                        licenseProp.SetValue(null, "Community");
                    }
                }
                else
                {
                    // If there's no License property, try to find an enum type in QuestPDF assembly
                    // that contains a 'Community' value and assign it to a static field/property named similarly.
                    var asm = settingsType.Assembly;
                    var enumType = asm.GetTypes().FirstOrDefault(t => t.IsEnum && t.GetEnumNames().Contains("Community"));
                    if (enumType != null)
                    {
                        var communityValue = Enum.Parse(enumType, "Community");
                        // Try to find any static property or field named 'License' or 'LicenseType'
                        var targetProp = settingsType.GetProperty("License") ?? settingsType.GetProperty("LicenseType");
                        if (targetProp != null && targetProp.PropertyType == enumType)
                        {
                            targetProp.SetValue(null, communityValue);
                        }
                        else
                        {
                            var field = settingsType.GetField("License") ?? settingsType.GetField("LicenseType");
                            if (field != null && field.FieldType == enumType)
                            {
                                field.SetValue(null, communityValue);
                            }
                        }
                    }
                }
            }
            catch
            {
                // If reflection fails, let QuestPDF throw its own informative exception at generation time.
            }

        }

        public byte[] GeneratePdf(PdfRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Html)) return Array.Empty<byte>();

            // Normalize whitespace: replace multiple spaces with single space
            var html = System.Text.RegularExpressions.Regex.Replace(request.Html, @"\s{2,}", " ");
            
            // Remove (--DOD-x--) patterns where x is any integer
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\(--DOD-\d+--\)", "");

            // Try headless Chrome (Puppeteer) renderer if available for exact KaTeX output
            try
            {
                var script = Path.Combine(AppContext.BaseDirectory, "../../../../tools/html-to-pdf/render_pdf.js");
                if (File.Exists(script))
                {
                    var tmpHtml = Path.Combine(Path.GetTempPath(), $"tobiso_pdf_{Guid.NewGuid()}.html");
                    var tmpPdf = Path.Combine(Path.GetTempPath(), $"tobiso_pdf_{Guid.NewGuid()}.pdf");
                    File.WriteAllText(tmpHtml, html);
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "node",
                        Arguments = $"\"{script}\" \"{tmpHtml}\" \"{tmpPdf}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = System.Diagnostics.Process.Start(psi);
                    if (proc != null)
                    {
                        var stderr = proc.StandardError.ReadToEndAsync();
                        proc.WaitForExit(30000);
                        if (proc.ExitCode == 0 && File.Exists(tmpPdf))
                        {
                            var bytes = File.ReadAllBytes(tmpPdf);
                            try { File.Delete(tmpPdf); } catch { }
                            try { File.Delete(tmpHtml); } catch { }
                            return bytes;
                        }
                        else
                        {
                            Console.WriteLine("[PdfService] Puppeteer renderer failed: " + stderr.Result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[PdfService] Puppeteer render error: " + ex.Message);
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var titleNode = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim();
            
            // Filter to get actual content nodes (skip the wrapper div and get its children)
            var contentNodes = doc.DocumentNode.SelectNodes("//div/*") ?? 
                              doc.DocumentNode.SelectNodes("//*") ?? 
                              new HtmlNodeCollection(doc.DocumentNode);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).LineHeight(1.5f));

                    page.Header().ShowOnce().Element(h => h.Text(titleNode ?? string.Empty).FontSize(18).Bold());
                    
                    page.Content().Column(col =>
                    {
                        col.Spacing(5);
                        RenderContent(col, contentNodes);
                    });
                    
                    page.Footer().Element(f => 
                    {
                        f.AlignCenter().Column(col =>
                        {
                            col.Item().Text("Tento obsah pochází z Tobisa (tobiso.com)").FontSize(9);
                        });
                    });
                });
            });

            using var ms = new MemoryStream();
            document.GeneratePdf(ms);
            return ms.ToArray();
        }

        private void RenderContent(QuestPDF.Fluent.ColumnDescriptor column, HtmlNodeCollection nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Name.ToLowerInvariant() == "h1") continue; // Skip h1
                
                var tagName = node.Name.ToLowerInvariant();
                
                if (tagName == "h2")
                {
                    column.Item().Text(HtmlEntity.DeEntitize(node.InnerText.Trim())).FontSize(15).Bold();
                }
                else if (tagName == "h3")
                {
                    column.Item().Text(HtmlEntity.DeEntitize(node.InnerText.Trim())).FontSize(13).Bold();
                }
                else if (tagName == "h4")
                {
                    column.Item().Text(HtmlEntity.DeEntitize(node.InnerText.Trim())).FontSize(12).Bold();
                }
                else if (tagName == "h5" || tagName == "h6")
                {
                    column.Item().Text(HtmlEntity.DeEntitize(node.InnerText.Trim())).FontSize(11).Bold();
                }
                else if (tagName == "p")
                {
                    // Check if p contains only img tag(s)
                    var imgNodes = node.SelectNodes(".//img");
                    var textContent = node.InnerText?.Trim() ?? "";
                    var hasOnlyImages = imgNodes != null && imgNodes.Any() && string.IsNullOrWhiteSpace(textContent);
                    
                    Console.WriteLine($"[PdfService] Processing <p>: imgNodes={imgNodes?.Count ?? 0}, hasOnlyImages={hasOnlyImages}, textContent='{textContent}'");
                    
                    if (hasOnlyImages && imgNodes != null)
                    {
                        // Render images directly
                        foreach (var imgNode in imgNodes)
                        {
                            var src = imgNode.GetAttributeValue("src", "");
                            Console.WriteLine($"[PdfService] Found img in p: src={src}");
                            if (!string.IsNullOrEmpty(src))
                            {
                                try
                                {
                                    // Replace https://tobiso.com with https://www.tobiso.com
                                    if (src.StartsWith("https://tobiso.com/", StringComparison.OrdinalIgnoreCase))
                                    {
                                        src = src.Replace("https://tobiso.com/", "https://www.tobiso.com/");
                                        Console.WriteLine($"[PdfService] Transformed https src to: {src}");
                                    }
                                    else if (!src.Contains("http"))
                                    {
                                        if (src.Contains("images"))
                                        {
                                            src = src.StartsWith("/") ? $"https://www.tobiso.com{src}" : $"https://www.tobiso.com/{src}";
                                            Console.WriteLine($"[PdfService] Transformed src to: {src}");
                                        }
                                    }
                                    
                                    var imageBytes = DownloadImage(src);
                                    if (imageBytes != null && imageBytes.Length > 0)
                                    {
                                        Console.WriteLine($"[PdfService] Rendering image with {imageBytes.Length} bytes");
                                        column.Item().MaxWidth(200).MaxHeight(200).Image(imageBytes).FitArea();
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[PdfService] Image download failed or returned empty");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[PdfService] Error rendering image: {ex.Message}");
                                }
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(node.InnerText))
                    {
                        column.Item().Element(container => RenderTextWithFormatting(container, node));
                    }
                }
                else if (tagName == "div")
                {
                    if (!string.IsNullOrWhiteSpace(node.InnerText))
                    {
                        column.Item().Element(container => RenderTextWithFormatting(container, node));
                    }
                }
                else if (tagName == "ul" || tagName == "ol")
                {
                    RenderList(column, node, 0);
                }
                else if (tagName == "img")
                {
                    var src = node.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(src))
                    {
                        try
                        {
                            // Replace https://tobiso.com with https://www.tobiso.com
                            if (src.StartsWith("https://tobiso.com/", StringComparison.OrdinalIgnoreCase))
                            {
                                src = src.Replace("https://tobiso.com/", "https://www.tobiso.com/");
                            }
                            else if (!src.Contains("http"))
                            {
                                if (src.Contains("images"))
                                {
                                    src = src.StartsWith("/") ? $"https://www.tobiso.com{src}" : $"https://www.tobiso.com/{src}";
                                }
                            }
                            
                            var imageBytes = DownloadImage(src);
                            if (imageBytes != null && imageBytes.Length > 0)
                            {
                                column.Item().AlignRight().MaxWidth(400).Image(imageBytes).FitArea();
                            }
                        }
                        catch { }
                    }
                }
                else if (tagName == "table")
                {
                    RenderTable(column, node);
                }
            }
        }

        private void RenderList(QuestPDF.Fluent.ColumnDescriptor column, HtmlNode listNode, int level)
        {
            var tagName = listNode.Name.ToLowerInvariant();
            var items = listNode.Elements("li").ToList();
            
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                
                // Určení prefixu podle úrovně a typu seznamu
                string prefix;
                if (tagName == "ol")
                {
                    prefix = $"{i + 1}.";
                }
                else // ul
                {
                    prefix = level == 0 ? "•" : "◦"; // Plná tečka pro první úroveň, prázdná pro vnořené
                }
                
                // Odsazení podle úrovně (0 = žádné, 1+ = vnořené)
                var indent = level * 15;
                
                // Extrahuj text bez vnořených seznamů
                var textOnly = GetDirectText(item);
                
                // Check if there are nested lists
                var nestedLists = item.ChildNodes.Where(n => 
                    n.NodeType == HtmlNodeType.Element && 
                    (n.Name.ToLowerInvariant() == "ul" || n.Name.ToLowerInvariant() == "ol")).ToList();
                
                var hasNestedLists = nestedLists.Any();
                
                if (!string.IsNullOrWhiteSpace(textOnly))
                {
                    column.Item()
                        .PaddingBottom(hasNestedLists ? 0 : 2)
                        .Row(row =>
                        {
                            if (indent > 0)
                            {
                                row.AutoItem().Width(indent).Text("");
                            }
                            row.AutoItem().Text(prefix).FontSize(11);
                            row.RelativeItem().PaddingLeft(5).Text(txt =>
                            {
                                ProcessNodeContent(txt, item);
                            });
                        });
                }
                
                // Zpracuj vnořené seznamy - renderuj je přímo do stejné column bez dalšího Item()
                foreach (var nestedList in nestedLists)
                {
                    RenderList(column, nestedList, level + 1);
                }
            }
        }
        
        private string GetDirectText(HtmlNode node)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var child in node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Text)
                {
                    sb.Append(child.InnerText);
                }
                else if (child.NodeType == HtmlNodeType.Element)
                {
                    var childTag = child.Name.ToLowerInvariant();
                    // Ignoruj vnořené seznamy, ale zahrň ostatní elementy (např. <strong>, <em>)
                    if (childTag != "ul" && childTag != "ol")
                    {
                        sb.Append(child.InnerText);
                    }
                }
            }
            return HtmlEntity.DeEntitize(sb.ToString().Trim());
        }

        private void RenderTextWithFormatting(QuestPDF.Infrastructure.IContainer container, HtmlNode node)
        {
            // Zkontroluj, jestli node obsahuje img tagy
            var imgNodes = node.SelectNodes(".//img");
            var mathNodes = node.SelectNodes(".//span[@data-math]");
            if (imgNodes != null && imgNodes.Any())
            {
                // Pokud obsahuje obrázky, renderuj je postupně
                container.Column(col =>
                {
                    col.Spacing(5);
                    foreach (var child in node.ChildNodes)
                    {
                        if (child.NodeType == HtmlNodeType.Element && child.Name.ToLowerInvariant() == "img")
                        {
                            var src = child.GetAttributeValue("src", "");
                            if (!string.IsNullOrEmpty(src))
                            {
                                try
                                {
                                    if (!src.Contains("http"))
                                    {
                                        if (src.Contains("images"))
                                        {
                                            src = src.StartsWith("/") ? $"https://tobiso.com{src}" : $"https://tobiso.com/{src}";
                                        }
                                    }
                                    
                                    var imageBytes = DownloadImage(src);
                                    if (imageBytes != null && imageBytes.Length > 0)
                                    {
                                        col.Item().MaxWidth(400).Image(imageBytes).FitWidth();
                                    }
                                }
                                catch { }
                            }
                        }
                        else if (child.NodeType == HtmlNodeType.Text || (child.NodeType == HtmlNodeType.Element && child.Name.ToLowerInvariant() != "img"))
                        {
                            col.Item().Text(txt => ProcessNodeContent(txt, node, skipImages: true));
                        }
                    }
                });
            }
            else if (mathNodes != null && mathNodes.Any())
            {
                // Render math spans and surrounding text as column items; embed rendered PNGs for KaTeX visuals
                container.Column(col =>
                {
                    col.Spacing(2);
                    foreach (var child in node.ChildNodes)
                    {
                        if (child.NodeType == HtmlNodeType.Element && child.Name.ToLowerInvariant() == "span" && child.GetAttributeValue("data-math", null) != null)
                        {
                            var dataMath = HtmlEntity.DeEntitize(child.GetAttributeValue("data-math", ""));
                            try
                            {
                                var png = RenderMathToPng(dataMath);
                                if (png != null && png.Length > 0)
                                {
                                    col.Item().MaxHeight(24).Image(png).FitArea();
                                    continue;
                                }
                            }
                            catch { }

                            // fallback textual
                            col.Item().Text(text => text.Span("[" + dataMath + "]"));
                        }
                        else if (child.NodeType == HtmlNodeType.Text || (child.NodeType == HtmlNodeType.Element && child.Name.ToLowerInvariant() != "img"))
                        {
                            col.Item().Text(txt => ProcessNodeContent(txt, node, skipImages: true));
                        }
                    }
                });
            }
            else
            {
                container.Text(txt =>
                {
                    ProcessNodeContent(txt, node, skipImages: false);
                });
            }
        }

        private void ProcessNodeContent(QuestPDF.Fluent.TextDescriptor text, HtmlNode node, bool skipImages = false)
        {
            foreach (var child in node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Text)
                {
                    var content = HtmlEntity.DeEntitize(child.InnerText);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        text.Span(content);
                    }
                }
                else if (child.NodeType == HtmlNodeType.Element)
                {
                    var childTag = child.Name.ToLowerInvariant();
                    
                    // Ignoruj vnořené seznamy v list items
                    if (childTag == "ul" || childTag == "ol")
                    {
                        continue;
                    }
                    
                    // Ignoruj img tagy pokud skipImages = true (budou zpracovány zvlášť)
                    if (skipImages && childTag == "img")
                    {
                        continue;
                    }
                    
                    var innerText = HtmlEntity.DeEntitize(child.InnerText);
                    
                    // Special-case: KaTeX math placeholders rendered client-side are
                    // represented as empty <span class='math-inline' data-math="..."></span>.
                    // These spans carry the LaTeX in the data-math attribute — extract
                    // and render a textual fallback into the PDF (e.g. (a)/(b)).
                    if (childTag == "span")
                    {
                        var dataMath = child.GetAttributeValue("data-math", "");
                        if (!string.IsNullOrEmpty(dataMath))
                        {
                            try
                            {
                                // HtmlAgilityPack may return encoded attribute values; de-entitize
                                var math = HtmlEntity.DeEntitize(dataMath);
                                // Look for an optional leading sign followed by \frac{num}{den}
                                var fracRx = new System.Text.RegularExpressions.Regex(@"^\s*([+\-\u2212\u2013\u2014]?)\\?frac\{(.+?)\}\{(.+?)\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                                var m = fracRx.Match(math);
                                if (m.Success)
                                {
                                    var mathFull = m.Value;
                                    // Try to render full LaTeX to PNG via Node renderer for KaTeX-equivalent look
                                    var png = RenderMathToPng(mathFull);
                                    if (png != null && png.Length > 0)
                                    {
                                        try
                                        {
                                            // Embed image into PDF (fit reasonably)
                                            text.Span(" ");
                                            // QuestPDF doesn't allow direct image inside TextDescriptor; instead
                                            // we'll fallback to rendering inline image by placing it into current container
                                            // This is a pragmatic approach: append a small image via the parent container
                                        }
                                        catch { }
                                    }
                                    // If rendering failed, fallback to textual representation
                                    var sign = m.Groups[1].Value ?? string.Empty;
                                    var num = m.Groups[2].Value.Trim();
                                    var den = m.Groups[3].Value.Trim();
                                    if (!string.IsNullOrEmpty(sign) && sign.Trim() == "-")
                                        text.Span($"-({num})/({den})");
                                    else
                                        text.Span($"({num})/({den})");
                                    continue;
                                }
                                // If not a simple \frac, just render the raw math inside brackets
                                text.Span($"[{math}]");
                                continue;
                            }
                            catch
                            {
                                // fall through to generic handling
                            }
                        }
                    }

                    if (childTag == "strong" || childTag == "b")
                    {
                        text.Span(innerText).Bold();
                    }
                    else if (childTag == "em" || childTag == "i")
                    {
                        // Skip entire italic block if it contains "zde"
                        if (innerText.Contains("zde", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        text.Span(innerText).Italic();
                    }
                    else if (childTag == "img")
                    {
                        // Obrázky v textu - přeskočíme je a zpracujeme v RenderTextWithFormatting
                        continue;
                    }
                    else
                    {
                        // Pro ostatní elementy zpracuj rekurzivně
                        ProcessNodeContent(text, child, skipImages);
                    }
                }
            }
        }

        private void RenderTable(QuestPDF.Fluent.ColumnDescriptor column, HtmlNode tableNode)
        {
            var rows = tableNode.SelectNodes(".//tr");
            if (rows == null || !rows.Any()) return;

            // Zjistit počet sloupců (maximální počet buněk v jakémkoliv řádku)
            var columnCount = rows.Max(row => 
                row.Elements("th").Count() + row.Elements("td").Count());

            column.Item().Table(table =>
            {
                // Definovat sloupce s rovnoměrnou šířkou
                table.ColumnsDefinition(columns =>
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        columns.RelativeColumn();
                    }
                });

                var isFirstRow = true;
                
                foreach (var row in rows)
                {
                    var cells = row.Elements("th").Any()
                        ? row.Elements("th").ToList()
                        : row.Elements("td").ToList();
                    
                    var isHeader = row.Elements("th").Any() || 
                                   (isFirstRow && row.ParentNode?.Name.ToLowerInvariant() == "thead");

                    for (int i = 0; i < cells.Count; i++)
                    {
                        var cell = cells[i];
                        var cellText = HtmlEntity.DeEntitize(cell.InnerText.Trim());

                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5).Element(container =>
                            {
                                if (isHeader)
                                {
                                    container.Text(cellText).Bold().FontSize(10);
                                }
                                else
                                {
                                    container.Text(cellText).FontSize(10);
                                }
                            });
                    }

                    isFirstRow = false;
                }
            });
        }

        private byte[]? DownloadImage(string url)
        {
            try
            {
                Console.WriteLine($"[PdfService] Downloading image: {url}");
                
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                };
                
                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                client.DefaultRequestHeaders.Add("Accept", "image/webp,image/apng,image/*,*/*;q=0.8");
                
                var response = client.GetAsync(url).GetAwaiter().GetResult();
                Console.WriteLine($"[PdfService] Response status: {response.StatusCode}");
                
                // Handle 307 redirect manually
                if (response.StatusCode == System.Net.HttpStatusCode.RedirectKeepVerb || 
                    response.StatusCode == System.Net.HttpStatusCode.Redirect)
                {
                    if (response.Headers.Location != null)
                    {
                        var redirectUrl = response.Headers.Location.IsAbsoluteUri 
                            ? response.Headers.Location.ToString() 
                            : new Uri(new Uri(url), response.Headers.Location).ToString();
                        Console.WriteLine($"[PdfService] Following redirect to: {redirectUrl}");
                        response = client.GetAsync(redirectUrl).GetAwaiter().GetResult();
                        Console.WriteLine($"[PdfService] Redirect response status: {response.StatusCode}");
                    }
                }
                
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"[PdfService] Downloaded {result.Length} bytes from {url}");
                    return result;
                }
                else
                {
                    Console.WriteLine($"[PdfService] Failed with status {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PdfService] Failed to download image {url}: {ex.Message}");
                return null;
            }
        }

        private byte[]? RenderMathToPng(string latex)
        {
            try
            {
                // Use a cache directory to avoid repeated renders
                var cacheDir = "/tmp/tobiso_katex_cache";
                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
                var key = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(latex))).Replace("=","_");
                var outPath = Path.Combine(cacheDir, key + ".png");
                if (File.Exists(outPath)) return File.ReadAllBytes(outPath);

                // Locate node renderer script
                var script = Path.Combine(AppContext.BaseDirectory, "../../../../tools/math-renderer/render_katex.js");
                if (!File.Exists(script))
                {
                    Console.WriteLine("[PdfService] KaTeX renderer script not found: " + script);
                    return null;
                }

                var psi = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "node",
                    Arguments = $"\"{script}\" \"{latex.Replace("\"","\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null) return null;
                using var ms = new MemoryStream();
                proc.StandardOutput.BaseStream.CopyTo(ms);
                var err = proc.StandardError.ReadToEnd();
                proc.WaitForExit(15000);
                if (proc.ExitCode != 0)
                {
                    Console.WriteLine("[PdfService] KaTeX renderer failed: " + err);
                    return null;
                }

                var bytes = ms.ToArray();
                if (bytes.Length > 0)
                {
                    try { File.WriteAllBytes(outPath, bytes); } catch { }
                    return bytes;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[PdfService] RenderMathToPng error: " + ex.Message);
                return null;
            }
        }
    }
}
