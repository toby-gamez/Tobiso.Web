namespace Tobiso.Web.Shared.Helpers;

/// <summary>
/// Posts have no dedicated slug column - their URL slug is derived from the markdown
/// file's name (without directory/extension), e.g. FilePath "matematika/nasobeni-zlomku.md"
/// -> slug "nasobeni-zlomku". Keeps link generation consistent everywhere a post URL is built.
/// </summary>
public static class PostSlug
{
    public static string? FromFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return null;
        var slug = System.IO.Path.GetFileNameWithoutExtension(filePath);
        return string.IsNullOrWhiteSpace(slug) ? null : slug;
    }

    /// <summary>Builds "/post/{slug}", falling back to "/post/{id}" when no slug can be derived.</summary>
    public static string Url(int id, string? filePath)
    {
        var slug = FromFilePath(filePath);
        return slug != null ? $"/post/{slug}" : $"/post/{id}";
    }
}
