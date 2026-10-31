using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Tobiso.Web.Files;

// Strips active content from uploaded SVGs before they're written to disk. SVG is XML, so a
// malicious upload can embed <script>, event-handler attributes (onload, onerror, ...), SMIL
// elements that rewrite attributes at runtime (a known sanitizer-bypass vector), or references
// to external resources. This is an allowlist-adjacent removal pass, not a full DOMPurify-style
// sanitizer (none exists as a maintained .NET package) -- it targets the concrete vectors that
// matter for a file that will be served back out and can be opened directly in a browser tab.
public static partial class SvgSanitizer
{
    private static readonly HashSet<string> DisallowedElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "foreignObject", "iframe", "embed", "object", "audio", "video",
        "link", "meta", "base",
        // SMIL animation elements can rewrite an attribute (e.g. href) at runtime, bypassing
        // static attribute-value checks below -- so they're dropped outright rather than sanitized.
        "animate", "animateMotion", "animateTransform", "animateColor", "set",
        // Legacy declarative event-handler elements from the SVG event spec.
        "handler", "listener",
    };

    [GeneratedRegex(@"url\(\s*(['""]?)([^'"")]*)\1\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlFunctionRegex();

    public static bool TrySanitize(Stream input, out byte[]? sanitized, out string? error)
    {
        sanitized = null;
        error = null;

        XDocument doc;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };
            using var reader = XmlReader.Create(input, settings);
            doc = XDocument.Load(reader, LoadOptions.None);
        }
        catch (Exception ex)
        {
            error = $"Neplatný SVG soubor: {ex.Message}";
            return false;
        }

        if (doc.Root is null || !string.Equals(doc.Root.Name.LocalName, "svg", StringComparison.Ordinal))
        {
            error = "Soubor není platné SVG (chybí kořenový element <svg>).";
            return false;
        }

        doc.DescendantNodes().OfType<XComment>().Remove();
        doc.DescendantNodes().OfType<XProcessingInstruction>().Remove();
        SanitizeElement(doc.Root);

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings { Encoding = Encoding.UTF8 }))
        {
            doc.Save(writer);
        }
        sanitized = ms.ToArray();
        return true;
    }

    private static void SanitizeElement(XElement element)
    {
        foreach (var child in element.Elements().ToList())
        {
            if (DisallowedElements.Contains(child.Name.LocalName))
            {
                child.Remove();
                continue;
            }
            SanitizeElement(child);
        }

        foreach (var attr in element.Attributes().ToList())
        {
            var localName = attr.Name.LocalName;

            // Covers both href and xlink:href -- XName.LocalName drops the namespace prefix.
            if (string.Equals(localName, "href", StringComparison.OrdinalIgnoreCase))
            {
                var value = attr.Value.Trim();
                if (!value.StartsWith('#') && !value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    attr.Remove();
                }
                continue;
            }

            if (localName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                attr.Remove();
                continue;
            }

            if (string.Equals(localName, "style", StringComparison.OrdinalIgnoreCase) && ContainsDangerousCss(attr.Value))
            {
                attr.Remove();
            }
        }

        if (string.Equals(element.Name.LocalName, "style", StringComparison.OrdinalIgnoreCase) && ContainsDangerousCss(element.Value))
        {
            element.RemoveNodes();
        }
    }

    private static bool ContainsDangerousCss(string css)
    {
        if (css.Contains("javascript:", StringComparison.OrdinalIgnoreCase)) return true;
        if (css.Contains("expression(", StringComparison.OrdinalIgnoreCase)) return true;
        if (css.Contains("@import", StringComparison.OrdinalIgnoreCase)) return true;

        foreach (Match m in UrlFunctionRegex().Matches(css))
        {
            // Internal references like url(#gradient) are fine; anything else (http:, data:, //) is dropped.
            if (!m.Groups[2].Value.Trim().StartsWith('#'))
            {
                return true;
            }
        }

        return false;
    }
}
