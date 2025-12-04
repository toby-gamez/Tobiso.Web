using System;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
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

            var doc = new HtmlDocument();
            doc.LoadHtml(request.Html);

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
                    page.Margin(20);
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
                            col.Item().Text("Obsah z Tobisa (tobiso.com)").FontSize(9);
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
                    if (!string.IsNullOrWhiteSpace(node.InnerText))
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
                else if (tagName == "table")
                {
                    var rows = node.SelectNodes(".//tr");
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            var cells = row.Elements("th").Any()
                                ? row.Elements("th").Select(c => HtmlEntity.DeEntitize(c.InnerText.Trim())).ToList()
                                : row.Elements("td").Select(c => HtmlEntity.DeEntitize(c.InnerText.Trim())).ToList();
                            column.Item().Text(string.Join(" | ", cells));
                        }
                    }
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
            container.Text(txt =>
            {
                ProcessNodeContent(txt, node);
            });
        }

        private void ProcessNodeContent(QuestPDF.Fluent.TextDescriptor text, HtmlNode node)
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
                    
                    var innerText = HtmlEntity.DeEntitize(child.InnerText);
                    
                    if (childTag == "strong" || childTag == "b")
                    {
                        text.Span(innerText).Bold();
                    }
                    else if (childTag == "em" || childTag == "i")
                    {
                        text.Span(innerText).Italic();
                    }
                    else
                    {
                        // Pro ostatní elementy zpracuj rekurzivně
                        ProcessNodeContent(text, child);
                    }
                }
            }
        }
    }
}