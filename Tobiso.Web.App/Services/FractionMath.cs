using System.Net;
using System.Text.RegularExpressions;

namespace Tobiso.Web.App.Services;

/// <summary>
/// Turns the authoring shorthand for fractions (<c>1/2</c>, <c>a/b</c>, <c>{n+1}/{n-1}</c>) into
/// inline KaTeX placeholder spans (<c>&lt;span class='math-inline' data-math="\frac{..}{..}"&gt;</c>),
/// which wwwroot/js/katex-init.js renders client-side. Shared by <c>MarkdownContent</c> (post bodies)
/// and <c>MathText</c> (plain DB strings) so both accept the same syntax.
/// <para>
/// A braced side may itself contain braces, and may contain further fractions as long as the inner
/// fraction has braces on both sides: <c>{{a}/{b} + 1}/{2}</c>. Inside a side, <c>&lt;sub&gt;x&lt;/sub&gt;</c> and
/// <c>&lt;sup&gt;x&lt;/sup&gt;</c> become <c>_{x}</c> / <c>^{x}</c> (raw or HTML-encoded, since plain text reaches
/// this step already escaped).
/// </para>
/// </summary>
public static class FractionMath
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    // "{...}" whose body may contain balanced nested braces; the body is captured as <name>.
    // Empty "{}" is rejected, matching the old "[^}]+" behaviour. The whole loop is atomic so
    // unbalanced input fails in linear time per start position instead of backtracking.
    private static string Braced(string name) =>
        @"\{(?!\})(?<" + name + @">(?>(?:[^{}]+|\{(?<o" + name + @">)|\}(?<-o" + name + @">))*))(?(o" + name + @")(?!))\}";

    // Plain-text fraction: each side is a {braced expression} or a single number/letter.
    private static readonly Regex TopLevel = new(
        @"(?<![\w/\-])(?:" + Braced("num") + @"|(?<num>-?(?:\d+(?:,\d+)?|\p{L})))\s*/\s*" +
        @"(?:" + Braced("den") + @"|(?<den>-?(?:\d+(?:,\d+)?|\p{L})))(?![\w/])",
        RegexOptions.CultureInvariant, RegexTimeout);

    // Fraction nested inside a braced side: both sides must be braced, so "{m/s}" inside a side
    // stays a unit and is not split.
    private static readonly Regex Nested = new(
        @"(?<![\w/])" + Braced("n") + @"\s*/\s*" + Braced("d") + @"(?![\w/])",
        RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Regex Strike = new(@"#s#(.*?)#s#", RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Regex Sub = new(
        @"(?:<|&lt;)sub(?:>|&gt;)(.*?)(?:<|&lt;)/sub(?:>|&gt;)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, RegexTimeout);

    private static readonly Regex Sup = new(
        @"(?:<|&lt;)sup(?:>|&gt;)(.*?)(?:<|&lt;)/sup(?:>|&gt;)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, RegexTimeout);

    /// <summary>Replaces every fraction in <paramref name="input"/> with a KaTeX placeholder span.
    /// Callers protect code regions first; returns the input unchanged if matching times out.</summary>
    public static string Replace(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        try
        {
            var replaced = TopLevel.Replace(input, match =>
            {
                try
                {
                    var num = match.Groups["num"].Value;
                    var den = match.Groups["den"].Value;

                    if (IsPlainWord(num) || IsPlainWord(den)) return match.Value;

                    var math = $"\\frac{{{ToLatex(num)}}}{{{ToLatex(den)}}}";
                    return $"<span class='math-inline' data-math=\"{WebUtility.HtmlEncode(math)}\"></span>";
                }
                catch (RegexMatchTimeoutException)
                {
                    return match.Value;
                }
            });

            // Absorb a leading hyphen separated by space into the math span so that patterns like
            // "- 1/2" become negative fractions: "-\frac{1}{2}".
            return Regex.Replace(replaced, "-\\s*(<span class='math-inline' data-math=\\\")",
                "<span class='math-inline' data-math=\"-", RegexOptions.CultureInvariant, RegexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return input;
        }
    }

    // "ano/ne" (yes/no) and similar word pairs stay plain text.
    private static bool IsPlainWord(string side) =>
        string.Equals(side, "ano", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(side, "ne", StringComparison.OrdinalIgnoreCase);

    // One fraction side -> LaTeX: nested braced fractions, #s#..#s# strikethrough, sub/sup tags.
    private static string ToLatex(string side)
    {
        side = Nested.Replace(side, m =>
            $"\\frac{{{ToLatex(m.Groups["n"].Value)}}}{{{ToLatex(m.Groups["d"].Value)}}}");
        side = Strike.Replace(side, m => $@"\cancel{{{m.Groups[1].Value}}}");
        side = Sub.Replace(side, m => $"_{{{m.Groups[1].Value}}}");
        side = Sup.Replace(side, m => $"^{{{m.Groups[1].Value}}}");
        return side.Trim().Replace(',', '.');
    }
}
