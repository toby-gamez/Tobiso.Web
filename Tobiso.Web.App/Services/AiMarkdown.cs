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
