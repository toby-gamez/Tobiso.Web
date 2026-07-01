namespace Tobiso.Web.Shared.DTOs
{
    public class RewriteGradeRequest
    {
        public int PostId { get; set; }
        public int TargetGrade { get; set; }
    }

    public class RewriteRegisterRequest
    {
        public int PostId { get; set; }
        public string Register { get; set; } = "student"; // "simple" | "student" | "expert"
    }

    public class RewriteGradeResponse
    {
        public string Content { get; set; } = string.Empty;
    }
}
