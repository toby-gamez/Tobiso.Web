using System;

namespace Tobiso.Web.Domain.Entities;

public class PostVersion
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post? Post { get; set; }

    public int GradeId { get; set; }
    public Grade? Grade { get; set; }

    public string Content { get; set; } = string.Empty;

    // Like before: LastFix (minor) and LastEdit (major)
    public DateTime? LastFix { get; set; }
    public DateTime? LastEdit { get; set; }

    // Nullable so it survives independently of LastFix/LastEdit: a version can be
    // marked checked even on a LastEdit (e.g. new features), not just on a LastFix.
    public bool? IsChecked { get; set; }
}
