using System.Globalization;
using System.Text;

namespace Tobiso.Web.Shared.Helpers;

/// <summary>
/// Diacritics-insensitive substring search helper, e.g. so "reseni" matches "řešení".
/// </summary>
public static class TextSearch
{
    public static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool ContainsIgnoreDiacritics(string haystack, string needle) =>
        RemoveDiacritics(haystack).Contains(RemoveDiacritics(needle), StringComparison.OrdinalIgnoreCase);
}
