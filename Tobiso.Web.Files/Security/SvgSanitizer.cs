using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Tobiso.Web.Files.Security;

/// <summary>
/// Strips active content from uploaded SVG files before they are stored and served from
/// files.tobiso.com. Works on an allowlist of SVG elements; everything else (script,
/// foreignObject, animation elements, foreign namespaces, comments, processing instructions)
/// is removed. Event handler attributes and any reference that is not a same-document
/// fragment (or an embedded raster image) are dropped as well.
/// </summary>
public static class SvgSanitizer
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";
    private static readonly XNamespace XlinkNs = "http://www.w3.org/1999/xlink";

    // Animation elements (animate, set, ...) are deliberately missing: they can rewrite
    // href to a javascript: URL at runtime. foreignObject can embed arbitrary HTML.
    private static readonly HashSet<string> AllowedElements = new(StringComparer.Ordinal)
    {
        "svg", "g", "defs", "symbol", "use", "title", "desc", "metadata", "style", "a", "image",
        "path", "rect", "circle", "ellipse", "line", "polyline", "polygon",
        "text", "tspan", "textPath",
        "linearGradient", "radialGradient", "stop", "pattern", "clipPath", "mask", "marker",
        "filter", "feBlend", "feColorMatrix", "feComponentTransfer", "feComposite",
        "feConvolveMatrix", "feDiffuseLighting", "feDisplacementMap", "feDistantLight",
        "feDropShadow", "feFlood", "feFuncA", "feFuncB", "feFuncG", "feFuncR",
        "feGaussianBlur", "feImage", "feMerge", "feMergeNode", "feMorphology", "feOffset",
        "fePointLight", "feSpecularLighting", "feSpotLight", "feTile", "feTurbulence",
    };

    private static readonly Regex UrlReference = new(
        @"url\s*\(\s*['""]?\s*([^)'""]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EmbeddedRasterImage = new(
        @"^data:image/(png|jpe?g|gif|webp);base64,", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TrySanitize(Stream input, out byte[]? sanitized, out string? error)
    {
        sanitized = null;
        error = null;

        XDocument doc;
        try
        {
            var settings = new XmlReaderSettings
            {
                // No DTDs at all: blocks XXE and entity-expansion ("billion laughs") attacks.
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
            };
            using var reader = XmlReader.Create(input, settings);
            doc = XDocument.Load(reader);
        }
        catch (XmlException)
        {
            error = "Soubor není platné SVG (chybné XML)";
            return false;
        }

        var root = doc.Root;
        if (root == null || root.Name != SvgNs + "svg")
        {
            error = "Soubor není platné SVG";
            return false;
        }

        SanitizeElement(root);

        var writerSettings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = false,
        };
        using var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output, writerSettings))
        {
            // Saving only the root drops any leftover top-level nodes besides it.
            new XDocument(root).Save(writer);
        }

        sanitized = output.ToArray();
        return true;
    }

    private static void SanitizeElement(XElement element)
    {
        foreach (var attr in element.Attributes().ToList())
        {
            if (!IsAttributeSafe(attr))
            {
                attr.Remove();
            }
        }

        foreach (var node in element.Nodes().ToList())
        {
            switch (node)
            {
                case XElement child when IsElementAllowed(child):
                    SanitizeElement(child);
                    break;
                case XElement child:
                    child.Remove();
                    break;
                case XText text when element.Name.LocalName == "style":
                    // Covers CDATA too (XCData derives from XText).
                    if (!IsCssSafe(text.Value)) text.Remove();
                    break;
                case XText:
                    break;
                default:
                    node.Remove();
                    break;
            }
        }
    }

    private static bool IsElementAllowed(XElement element) =>
        element.Name.Namespace == SvgNs && AllowedElements.Contains(element.Name.LocalName);

    private static bool IsAttributeSafe(XAttribute attr)
    {
        if (attr.IsNamespaceDeclaration)
        {
            return true;
        }

        var name = attr.Name;
        var localName = name.LocalName;

        if (name == XlinkNs + "href" || (name.Namespace == XNamespace.None && localName == "href"))
        {
            return IsHrefSafe(attr.Value, attr.Parent?.Name.LocalName);
        }

        // Only plain attributes and xml:* (xml:space, xml:lang) are kept; editor namespaces
        // (inkscape:, sodipodi:, ...) carry nothing needed for rendering.
        if (name.Namespace != XNamespace.None && name.Namespace != XNamespace.Xml)
        {
            return false;
        }

        if (localName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = Normalize(attr.Value);
        if (value.Contains("javascript:") || value.Contains("vbscript:"))
        {
            return false;
        }

        return localName == "style" ? IsCssSafe(attr.Value) : HasOnlyLocalUrlReferences(attr.Value);
    }

    private static bool IsHrefSafe(string value, string? elementName)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('#'))
        {
            return true;
        }

        // Inline raster data is fine for <image>/<feImage>; it cannot run script.
        return elementName is "image" or "feImage" && EmbeddedRasterImage.IsMatch(trimmed);
    }

    private static bool IsCssSafe(string css)
    {
        var normalized = Normalize(css);
        // Backslash escapes ("u\72l(", "\6a avascript:") could hide the keywords below.
        if (normalized.Contains('\\') || normalized.Contains("@import") || normalized.Contains("expression(")
            || normalized.Contains("javascript:") || normalized.Contains("vbscript:")
            || normalized.Contains("-moz-binding") || normalized.Contains("behavior:"))
        {
            return false;
        }

        return HasOnlyLocalUrlReferences(css);
    }

    // url(...) may only point inside the document (e.g. fill="url(#gradient)"); external
    // references would let the image load remote content or track viewers.
    private static bool HasOnlyLocalUrlReferences(string value)
    {
        foreach (Match match in UrlReference.Matches(value))
        {
            if (!match.Groups[1].Value.TrimStart().StartsWith('#'))
            {
                return false;
            }
        }

        return true;
    }

    // Lowercase and strip whitespace/control characters so "java\tscript:" or CSS escapes
    // split by whitespace can't slip past the substring checks.
    private static string Normalize(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (!char.IsWhiteSpace(c) && !char.IsControl(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }
}
