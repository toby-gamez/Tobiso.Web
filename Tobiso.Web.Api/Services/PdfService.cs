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

            // Enable QuestPDF debugging output to get detailed layout diagnostics
            try
            {
                // Prefer direct call; if property name changes across versions, ignore failures
                QuestPDF.Settings.EnableDebugging = true;
                Console.WriteLine("[PdfService] QuestPDF debugging enabled");
            }
            catch
            {
                // Ignore if not available
            }

        }

        public byte[] GeneratePdf(PdfRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Html)) return Array.Empty<byte>();

            // Normalize whitespace: replace multiple spaces with single space
            var html = System.Text.RegularExpressions.Regex.Replace(request.Html, @"\s{2,}", " ");
            
            // Remove (--DOD-x--) patterns where x is any integer
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\(--DOD-\d+--\)", "");

            // Using QuestPDF pipeline only (Plan B): remove Puppeteer/Node path and render
            // textual math fallbacks directly. This keeps PDF generation deterministic
            // and free of external runtime dependencies.

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var titleNode = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim();
            
            // Filter to get actual content nodes (skip the wrapper div and get its children)
            var contentNodes = doc.DocumentNode.SelectNodes("//div/*") ??
                              doc.DocumentNode.SelectNodes("//*") ??
                              new HtmlNodeCollection(doc.DocumentNode);

            Console.WriteLine($"[PdfService] HTML length={html.Length}, title='{titleNode ?? "(none)"}'");
            if (contentNodes != null)
            {
                Console.WriteLine($"[PdfService] contentNodes count={contentNodes.Count}");
                var idx = 0;
                foreach (var n in contentNodes.Take(20))
                {
                    Console.WriteLine($"[PdfService] contentNodes[{idx}] = <{n.Name}> innerLen={n.InnerText?.Trim().Length ?? 0}");
                    idx++;
                }
            }

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
                Console.WriteLine($"[PdfService] RenderContent node: <{node.Name}> innerLen={node.InnerText?.Trim().Length ?? 0} hasMath={ContainsMathFraction(node)}");
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
                    else if (ContainsMathFraction(node) || !string.IsNullOrWhiteSpace(node.InnerText))
                    {
                        // Render even when InnerText is empty if there are math spans
                        // (KaTeX inline placeholders are empty but carry data-math).
                        column.Item().Element(container => RenderTextWithFormatting(container, node));
                    }
                }
                else if (tagName == "div")
                {
                if (ContainsMathFraction(node) || !string.IsNullOrWhiteSpace(node.InnerText))
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
                            row.RelativeItem().PaddingLeft(5).Element(c =>
                            {
                                if (ContainsMathFraction(item))
                                {
                                    var segments = ExtractSegments(item);
                                    RenderMixedContent(c, segments, 11f);
                                }
                                else
                                {
                                    c.Text(txt => ProcessNodeContent(txt, item));
                                }
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
                            if (ContainsMathFraction(node))
                            {
                                var segments = ExtractSegments(node);
                                col.Item().Element(c => RenderMixedContent(c, segments, 11f));
                            }
                            else
                            {
                                col.Item().Text(txt => ProcessNodeContent(txt, node, skipImages: true));
                            }
                        }
                    }
                });
            }
            else
            {
                if (ContainsMathFraction(node))
                {
                    var segments = ExtractSegments(node);
                    container.Element(c => RenderMixedContent(c, segments, 11f));
                }
                else
                {
                    container.Text(txt => ProcessNodeContent(txt, node, skipImages: false));
                }
            }
        }

        // Segment types used to split a node into text and fraction pieces
        private abstract record ContentSegment;
        private record TextNodes(System.Collections.Generic.List<HtmlNode> Nodes) : ContentSegment;
        private record FractionSeg(string Sign, string Num, string Den) : ContentSegment;

        private bool ContainsMathFraction(HtmlNode node)
        {
            var spans = node.SelectNodes(".//span[@data-math]");
            if (spans == null) return false;
            foreach (var s in spans)
            {
                var data = s.GetAttributeValue("data-math", "");
                if (!string.IsNullOrEmpty(data) && data.Contains("frac")) return true;
            }
            return false;
        }

        private System.Collections.Generic.List<ContentSegment> ExtractSegments(HtmlNode node)
        {
            var list = new System.Collections.Generic.List<ContentSegment>();
            var buffer = new System.Collections.Generic.List<HtmlNode>();

            void flushBuffer()
            {
                if (buffer.Any())
                {
                    list.Add(new TextNodes(new System.Collections.Generic.List<HtmlNode>(buffer)));
                    buffer.Clear();
                }
            }

            void processNode(HtmlNode current)
            {
                foreach (var child in current.ChildNodes)
                {
                    if (child.NodeType == HtmlNodeType.Text)
                    {
                        buffer.Add(child);
                        continue;
                    }

                    if (child.NodeType == HtmlNodeType.Element && child.Name.ToLowerInvariant() == "span")
                    {
                        var dataMath = child.GetAttributeValue("data-math", "");
                        if (!string.IsNullOrEmpty(dataMath))
                        {
                            var math = HtmlEntity.DeEntitize(dataMath);
                            var fracRx = new System.Text.RegularExpressions.Regex(@"^\s*([+\-\u2212\u2013\u2014]?)\\?frac\{(.+?)\}\{(.+?)\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                            var m = fracRx.Match(math);
                            if (m.Success)
                            {
                                flushBuffer();
                                var sign = m.Groups[1].Value ?? string.Empty;
                                var num = m.Groups[2].Value.Trim();
                                var den = m.Groups[3].Value.Trim();
                                list.Add(new FractionSeg(sign, num, den));
                                continue;
                            }
                        }
                    }

                    // If element contains descendant data-math spans, recurse into it to split properly
                    if (child.NodeType == HtmlNodeType.Element && child.SelectSingleNode(".//span[@data-math]") != null)
                    {
                        processNode(child);
                    }
                    else
                    {
                        // Element without fractions: keep as a single node so formatting (strong/em) is preserved
                        buffer.Add(child);
                    }
                }
            }

            processNode(node);
            flushBuffer();
            Console.WriteLine($"[PdfService] ExtractSegments -> {list.Count} segments for node <{node.Name}> innerLen={node.InnerText?.Trim().Length ?? 0}");
            var si = 0;
            foreach (var s in list)
            {
                if (s is TextNodes tn) Console.WriteLine($"[PdfService]  seg[{si}] TextNodes count={tn.Nodes.Count}");
                if (s is FractionSeg fs) Console.WriteLine($"[PdfService]  seg[{si}] Fraction sign='{fs.Sign}' num='{fs.Num}' den='{fs.Den}'");
                si++;
            }
            return list;
        }

        // Render a stacked fraction visually inside an IContainer
        private void RenderVisualFraction(QuestPDF.Infrastructure.IContainer container, FractionSeg frac, float fontSize)
        {
            // Use a compact column: numerator, rule, denominator. The container
            // will size to its content when placed inside an AutoItem. We add
            // a small horizontal padding to the rule so it matches text width
            // and doesn't extend beyond the numerator/denominator.
            container.Column(col =>
            {
                col.Spacing(0);
                col.Item().AlignCenter().Text(t =>
                {
                    var numText = (string.IsNullOrEmpty(frac.Sign) || frac.Sign.Trim() == "+") ? frac.Num : frac.Sign + frac.Num;
                    t.Span(numText).FontSize(fontSize * 0.85f);
                });

                // Render the rule as a small box with bottom border. To make
                // the rule the same width as the widest of numerator/denominator,
                // render the numerator and denominator in separate centered
                // items and let the column width be determined by the widest
                // item. The rule is an Item with fixed height and no extra width
                // so it naturally matches the container width.
                col.Item().Height(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingHorizontal(1);

                col.Item().AlignCenter().Text(t => t.Span(frac.Den).FontSize(fontSize * 0.85f));
            });
        }

        // Render a mixed sequence of TextNodes and FractionSegs into a TextDescriptor (or container represented as TextDescriptor)
        private void RenderMixedContent(QuestPDF.Fluent.TextDescriptor text, System.Collections.Generic.List<ContentSegment> segments, float fontSize)
        {
            // TextDescriptor can't directly contain container elements, so instead
            // create an outer element and re-render segments using Row/Element.
            // We assume caller is rendering into a container that supports Element().
            // Find a way to access parent container: use a temporary inline hack by
            // writing segments into the TextDescriptor as spans where possible and
            // delegating stacked fractions to a simple {num}/{den} fallback if we
            // cannot render complex layout here. To fully support stacked layout
            // we require the caller to render via IContainer.Element. We will
            // therefore fall back to rendering as inline text within braces for
            // safety when called with a TextDescriptor.

            // Simple fallback: write as {num}/{den} to avoid breaking PDF generation.
            foreach (var seg in segments)
            {
                if (seg is TextNodes tn)
                {
                    // Render nodes as normal
                    foreach (var n in tn.Nodes)
                    {
                        if (n.NodeType == HtmlNodeType.Text)
                        {
                            var content = HtmlEntity.DeEntitize(n.InnerText);
                            if (!string.IsNullOrWhiteSpace(content)) text.Span(content);
                        }
                        else
                        {
                            // For element nodes, delegate to existing processor
                            ProcessNodeContent(text, n, skipImages: true);
                        }
                    }
                }
                else if (seg is FractionSeg f)
                {
                    // Fallback inline textual representation; this keeps the PDF
                    // layout stable when ProcessNodeContent expects a TextDescriptor.
                    var sign = (!string.IsNullOrEmpty(f.Sign) && f.Sign.Trim() == "-") ? "-" : string.Empty;
                    text.Span(sign + "{" + f.Num + "}/{" + f.Den + "}");
                }
            }
        }

        // Overload for rendering mixed content into an outer container (preferred)
        private void RenderMixedContent(QuestPDF.Infrastructure.IContainer container, System.Collections.Generic.List<ContentSegment> segments, float fontSize)
        {
            // If there's only a single TextNodes, shortcut
            if (segments.Count == 1 && segments[0] is TextNodes tn)
            {
                container.Text(txt => ProcessNodeContent(txt, tn.Nodes));
                return;
            }

            container.Row(row =>
            {
                // Group consecutive TextNodes into a single RelativeItem to avoid
                // creating multiple flexible items which can lead to conflicting
                // size constraints in complex layouts.
                var textBuffer = new System.Collections.Generic.List<HtmlNode>();

                void flushTextBuffer(bool preferAuto = false)
                {
                    if (!textBuffer.Any()) return;
                    var nodesToRender = new System.Collections.Generic.List<HtmlNode>(textBuffer);
                    textBuffer.Clear();
                    if (preferAuto)
                    {
                        // Render short text before a fraction as AutoItem so the
                        // fraction sits immediately after it. AutoItems do not wrap,
                        // so we only prefer this when the buffered text is short.
                        var totalTextLength = string.Join("", nodesToRender.Where(n => n.NodeType == HtmlNodeType.Text).Select(n => n.InnerText)).Length;
                        if (totalTextLength < 80)
                        {
                            // Center text vertically so it aligns with stacked fraction
                            row.AutoItem().AlignMiddle().Element(c =>
                            {
                                c.Text(txt => ProcessNodeContent(txt, nodesToRender));
                            });
                            return;
                        }
                        // Fallback to RelativeItem if text is long (to allow wrapping)
                    }

                    // Allow wrapping but center vertically to match fraction height
                    row.RelativeItem().AlignMiddle().Element(c =>
                    {
                        c.Text(txt => ProcessNodeContent(txt, nodesToRender));
                    });
                }

                Console.WriteLine($"[PdfService] RenderMixedContent(container) called with {segments.Count} segments");
                foreach (var seg in segments)
                {
                    if (seg is TextNodes tn2)
                    {
                        // Accumulate text nodes
                        textBuffer.AddRange(tn2.Nodes);
                    }
                    else if (seg is FractionSeg f)
                    {
                        // First flush any accumulated text
                        flushTextBuffer(preferAuto: true);
                        // Let the AutoItem size to the content so the fraction's
                        // rule will be as long as the text. Add a small horizontal
                        // padding so the fraction doesn't butt directly against
                        // neighbouring characters.
                        // Keep the fraction vertically centered in the row
                        row.AutoItem().AlignMiddle().AlignLeft().PaddingLeft(2).PaddingRight(2).Element(c => RenderVisualFraction(c, f, fontSize));
                    }
                }

                // Flush tailing text (allow wrapping)
                flushTextBuffer(preferAuto: false);
            });
        }

        // Overload that processes a sequence of nodes (used by mixed-content renderers)
        private void ProcessNodeContent(QuestPDF.Fluent.TextDescriptor text, System.Collections.Generic.IEnumerable<HtmlNode> nodes, bool skipImages = false)
        {
            foreach (var child in nodes)
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

                    if (childTag == "ul" || childTag == "ol")
                    {
                        continue;
                    }

                    if (skipImages && childTag == "img")
                    {
                        continue;
                    }

                    var innerText = HtmlEntity.DeEntitize(child.InnerText);

                    if (childTag == "span")
                    {
                        var dataMath = child.GetAttributeValue("data-math", "");
                        if (!string.IsNullOrEmpty(dataMath))
                        {
                            try
                            {
                                var math = HtmlEntity.DeEntitize(dataMath);
                                var fracRx = new System.Text.RegularExpressions.Regex(@"^\s*([+\-\u2212\u2013\u2014]?)\\?frac\{(.+?)\}\{(.+?)\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                                var m = fracRx.Match(math);
                                if (m.Success)
                                {
                                    var sign = m.Groups[1].Value ?? string.Empty;
                                    var num = m.Groups[2].Value.Trim();
                                    var den = m.Groups[3].Value.Trim();
                                    if (!string.IsNullOrEmpty(sign) && sign.Trim() == "-")
                                        text.Span($"-{{{num}}}/{{{den}}}");
                                    else
                                        text.Span($"{{{num}}}/{{{den}}}");
                                    continue;
                                }

                                text.Span($"{{{math}}}");
                                continue;
                            }
                            catch
                            {
                            }
                        }
                    }

                    if (childTag == "strong" || childTag == "b")
                    {
                        text.Span(innerText).Bold();
                    }
                    else if (childTag == "em" || childTag == "i")
                    {
                        if (innerText.Contains("zde", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        text.Span(innerText).Italic();
                    }
                    else if (childTag == "img")
                    {
                        continue;
                    }
                    else
                    {
                        ProcessNodeContent(text, child, skipImages);
                    }
                }
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
                                    // If rendering to image is not available, always use textual fallback
                                    var sign = m.Groups[1].Value ?? string.Empty;
                                    var num = m.Groups[2].Value.Trim();
                                    var den = m.Groups[3].Value.Trim();
                                    // Use curly-brace format as canonical textual representation
                                    if (!string.IsNullOrEmpty(sign) && sign.Trim() == "-")
                                        text.Span($"-{{{num}}}/{{{den}}}");
                                    else
                                        text.Span($"{{{num}}}/{{{den}}}");
                                    continue;
                                }
                                // If not a simple \frac, render raw math inside curly braces
                                text.Span($"{{{math}}}");
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

        // Note: Node-based KaTeX rendering was removed (Plan B). Math rendering
        // now uses textual curly-brace fallbacks produced in ProcessNodeContent.
    }
}
