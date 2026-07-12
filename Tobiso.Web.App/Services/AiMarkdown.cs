using Markdig;

namespace Tobiso.Web.App.Services;

/// <summary>
/// Renders AI-generated (untrusted) markdown to HTML with raw HTML passthrough disabled.
/// Markdig normally emits inline/block HTML verbatim, so a prompt-injected
/// <c>&lt;script&gt;</c> or <c>&lt;img onerror=...&gt;</c> in a model response would become
/// live markup when handed to <c>MarkupString</c>. <see cref="MarkdownPipelineBuilder.DisableHtml"/>
/// makes any such raw HTML render as escaped text instead, closing that XSS vector.
/// Use this for anything sourced from the AI service; do NOT use it for admin-authored
/// post content, which legitimately relies on raw HTML / KaTeX.
/// </summary>
public static class AiMarkdown
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // pipe tables etc. used by AI comparison output
            .DisableHtml()
            .Build();

    public static string ToSafeHtml(string? markdown) =>
        string.IsNullOrEmpty(markdown) ? string.Empty : Markdown.ToHtml(markdown, Pipeline);
}
