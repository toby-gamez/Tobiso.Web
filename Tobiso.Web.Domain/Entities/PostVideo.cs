namespace Tobiso.Web.Domain.Entities;

public class PostVideo
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public string YoutubeUrl { get; set; } = "";
    public int Timestamp { get; set; } = 0;  // seconds
    public string Label { get; set; } = "";
}
