using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Tobiso.Web.Shared.Graphs;

/// <summary>Small helpers shared by the block renderers (flowchart, Venn).</summary>
internal static class DiagramSupport
{
    // key(args): value   -   same line shape the [grafN] blocks use.
    public static readonly Regex LineRegex = new(
        @"^(?<key>[\p{L}_][\p{L}\p{N}_]*)\s*(?:\((?<args>[^)]*)\))?\s*:\s*(?<value>.*)$",
        RegexOptions.CultureInvariant);

    public static string Enc(string s) => WebUtility.HtmlEncode(s);

    public static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    // Lower-case + strip diacritics, so "Začátek"/"zacatek" and "ČERVENÁ"/"cervena" match.
    public static string Normalize(string s)
    {
        var decomposed = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // Deterministic across processes (string.GetHashCode is randomised), keeps svg ids stable.
    public static string StableHash(string s)
    {
        uint h = 2166136261;
        foreach (var c in s) h = (h ^ c) * 16777619;
        return h.ToString("x8", CultureInfo.InvariantCulture);
    }

    /// <summary>"id: a, color: RED" becomes {id: a, color: RED}. A bare token is stored under "".</summary>
    public static Dictionary<string, string> ParseArgs(string? args)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(args)) return result;
        foreach (var part in args.Split(','))
        {
            var colon = part.IndexOf(':');
            if (colon < 0)
            {
                if (part.Trim().Length > 0) result[""] = part.Trim();
                continue;
            }
            result[Normalize(part[..colon])] = part[(colon + 1)..].Trim();
        }
        return result;
    }

    /// <summary>Greedy word wrap on spaces; a word longer than the limit stays on its own line.</summary>
    public static List<string> Wrap(string text, int maxChars)
    {
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length > 0 && current.Length + 1 + word.Length > maxChars)
            {
                lines.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }
        if (current.Length > 0) lines.Add(current.ToString());
        if (lines.Count == 0) lines.Add("");
        return lines;
    }
}
