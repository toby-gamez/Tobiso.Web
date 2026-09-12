namespace Tobiso.Web.Shared.DTOs;

public class DifficultyRatingRequest
{
    public int Rating { get; set; }      // 1=easy, 2=ok, 3=hard
    public string? DeviceId { get; set; }
}

public class DifficultyRatingResult
{
    public string Message { get; set; } = "";
}

public class PostDifficultyResponse
{
    public int Easy { get; set; }
    public int Ok { get; set; }
    public int Hard { get; set; }
    public int Total { get; set; }
}

public class PostVideoRequest
{
    public string? YoutubeUrl { get; set; }
    public int Timestamp { get; set; }
    public string? Label { get; set; }
}

public class PostVideoResponse
{
    public string YoutubeUrl { get; set; } = "";
    public int Timestamp { get; set; }
    public string Label { get; set; } = "";
}
