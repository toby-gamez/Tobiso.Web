namespace Tobiso.Web.Shared.DTOs
{
    public class RewriteGradeRequest
    {
        public int PostId { get; set; }
        public int TargetGrade { get; set; }
    }

    public class RewriteGradeResponse
    {
        public string Content { get; set; } = string.Empty;
    }
}
