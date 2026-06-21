namespace Tobiso.Web.Shared.DTOs
{
    public class FlashcardRequest
    {
        public int PostId { get; set; }
    }

    public class FlashcardCard
    {
        public string Term { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
    }

    public class FlashcardResponse
    {
        public List<FlashcardCard> Cards { get; set; } = new();
    }
}
