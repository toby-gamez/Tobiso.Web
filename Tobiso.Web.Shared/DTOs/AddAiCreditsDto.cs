namespace Tobiso.Web.Shared.DTOs
{
    public class AddAiCreditsRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public int Count { get; set; }
        public long ValidUntilUtc { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public class AddAiCreditsResponse
    {
        public bool Success { get; set; }
        public int TotalRemainingToday { get; set; }
    }
}
