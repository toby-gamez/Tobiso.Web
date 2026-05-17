using System;

namespace Tobiso.Web.Shared.DTOs;

public class PostVersionResponse
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int? GradeId { get; set; }
    public string Content { get; set; } = string.Empty;
    // Grade level (e.g. 6,7,8,9) included for client-side resolution convenience
    public int? GradeLevel { get; set; }
    public DateTime? LastFix { get; set; }
    public DateTime? LastEdit { get; set; }
}
