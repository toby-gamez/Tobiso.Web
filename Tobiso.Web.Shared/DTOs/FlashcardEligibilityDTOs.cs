namespace Tobiso.Web.Shared.DTOs;

public class FlashcardEligibilityStats
{
    public int Total { get; set; }
    public int Eligible { get; set; }
    public int Ineligible { get; set; }
    public int Unclassified { get; set; }
}

public class ClassifyFlashcardEligibilityRequest
{
    public int BatchSize { get; set; } = 30;
}

public class FlashcardEligibilityBatchResult
{
    public int Processed { get; set; }
    public int EligibleCount { get; set; }
    public int IneligibleCount { get; set; }
    public int RemainingUnclassified { get; set; }
    public string? Error { get; set; }
}
