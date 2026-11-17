using System.Text.RegularExpressions;
using Markdig;

namespace Tobiso.Web.App.Services;

/// <summary>
/// Helpers for rendering AI-generated (untrusted) markdown. Markdig emits raw HTML verbatim and does
/// not filter link schemes, so a prompt-injected <c>&lt;img onerror=...&gt;</c> or
/// <c>[x](javascript:...)</c> in a model response would become live markup once handed to
/// <c>MarkupString</c>. Use these for anything sourced from the AI service (also via
/// <c>&lt;MarkdownContent Untrusted="true"&gt;</c>); admin-authored post content legitimately relies on
/// raw HTML / KaTeX and is rendered without them.
/// </summary>
public static partial class AiMarkdown
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // pipe tables etc. used by AI comparison output
            .DisableHtml()
            .Build();

    public static string ToSafeHtml(string? markdown) =>
        string.IsNullOrEmpty(markdown) ? string.Empty : StripDangerousUrls(Markdown.ToHtml(markdown, Pipeline));

    /// <summary>
    /// Like <see cref="ToSafeHtml"/>, but LaTeX in the source (<c>\(..\)</c>, <c>\[..\]</c>, <c>$$..$$</c>,
    /// <c>$..$</c>) is lifted out before Markdig runs - it would otherwise eat the backslashes and
    /// treat <c>_</c>/<c>*</c> as emphasis - and re-inserted as <c>data-math</c> spans that
    /// katex-init.js renders. Code spans and fenced blocks are left alone.
    /// </summary>
    public static string ToSafeHtmlWithMath(string? markdown, MarkdownPipeline pipeline)
    {
        if (string.IsNullOrEmpty(markdown)) return string.Empty;

        var formulas = new List<(string Tex, bool Display)>();
        var parts = CodeRegionRegex().Split(markdown);
        for (var i = 0; i < parts.Length; i += 2) // odd entries are the captured code regions
            parts[i] = ExtractMath(parts[i], formulas);

        var html = StripDangerousUrls(Markdown.ToHtml(string.Concat(parts), pipeline));
        if (formulas.Count == 0) return html;

        return PlaceholderRegex().Replace(html, m =>
        {
            var (tex, display) = formulas[int.Parse(m.Groups[1].Value)];
            // A span even for display math: a block element would be split out of its <p> by the HTML parser.
            return $"<span class=\"math-inline\" data-math=\"{System.Net.WebUtility.HtmlEncode(tex)}\"{(display ? " data-display=\"true\"" : "")}></span>";
        });
    }

    private static string ExtractMath(string text, List<(string Tex, bool Display)> formulas) =>
        MathRegex().Replace(text, m =>
        {
            var display = m.Groups["dd"].Success || m.Groups["br"].Success;
            var tex = (m.Groups["dd"].Success ? m.Groups["dd"].Value
                : m.Groups["br"].Success ? m.Groups["br"].Value
                : m.Groups["pa"].Success ? m.Groups["pa"].Value
                : m.Groups["sd"].Value).Trim();
            if (tex.Length == 0) return m.Value;
            formulas.Add((tex, display));
            return $"{formulas.Count - 1}";
        });

    [GeneratedRegex(@"(```.*?```|`[^`\n]+`)", RegexOptions.Singleline)]
    private static partial Regex CodeRegionRegex();

    [GeneratedRegex(@"(\d+)")]
    private static partial Regex PlaceholderRegex();

    // Single $..$ is deliberately strict (no inner edge whitespace, not followed by a digit) so
    // prices like "5 $ a 10 $" are left alone.
    [GeneratedRegex(@"\$\$(?<dd>.+?)\$\$|\\\[(?<br>.+?)\\\]|\\\((?<pa>.+?)\\\)|(?<!\\)\$(?<sd>(?=\S)[^$\n]+?(?<=\S))\$(?!\d)",
        RegexOptions.Singleline)]
    private static partial Regex MathRegex();

    /// <summary>
    /// Makes untrusted markdown source unable to form HTML tags: every "&lt;" becomes an entity, so it
    /// still displays as "&lt;" but can never open a tag. Applied before any trusted preprocessing that
    /// inserts its own markup.
    /// </summary>
    public static string NeutralizeHtml(string markdown) => markdown.Replace("<", "&lt;");

    /// <summary>Replaces href/src values using a script-capable scheme (javascript:, vbscript:, data:) with "#".</summary>
    public static string StripDangerousUrls(string html) =>
        DangerousUrlRegex().Replace(html, m => $"{m.Groups["attr"].Value}=\"#\"");

    [GeneratedRegex(@"(?<attr>href|src)\s*=\s*(?<q>[""']?)\s*(?:javascript|vbscript|data)\s*:[^""'\s>]*\k<q>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DangerousUrlRegex();
}
