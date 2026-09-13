using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Helpers;

/// <summary>
/// Picks which grade-level version of a post's content to use, given an optional
/// preferred grade level (e.g. the visitor's default grade from the nav menu).
/// </summary>
public static class PostVersionSelector
{
    /// <summary>
    /// No preference: the easiest (lowest grade level) version, so a young student
    /// isn't handed the most advanced content by default. With a preference: the exact
    /// grade if the post has it, else the nearest lower grade available, else the
    /// nearest grade available overall.
    /// </summary>
    public static PostVersionResponse? SelectForGrade(List<PostVersionResponse>? versions, int? preferredLevel)
    {
        if (versions == null || versions.Count == 0) return null;

        if (preferredLevel is null)
            return versions.OrderBy(v => v.GradeLevel ?? int.MaxValue).First();

        var exact = versions.FirstOrDefault(v => v.GradeLevel == preferredLevel);
        if (exact != null) return exact;

        var lower = versions.Where(v => (v.GradeLevel ?? int.MinValue) < preferredLevel.Value).ToList();
        if (lower.Count > 0)
            return lower.OrderByDescending(v => v.GradeLevel).First();

        return versions.OrderBy(v => Math.Abs((v.GradeLevel ?? int.MaxValue) - preferredLevel.Value)).First();
    }
}
