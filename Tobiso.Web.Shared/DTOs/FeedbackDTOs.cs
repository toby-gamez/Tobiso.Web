using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class FeedbackResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class CreateFeedbackDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public FeedbackType Type { get; set; } = FeedbackType.Bug;
}

public class FeedbackItemResponse
{
    public string Id { get; set; } = string.Empty;
    public FeedbackType Type { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SubmittedByUserId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public FeedbackStatus Status { get; set; }
    public string? AdminNote { get; set; }
}

public class UpdateFeedbackRequest
{
    public FeedbackStatus? Status { get; set; }
    public string? AdminNote { get; set; }
}

public class PagedFeedbackResponse
{
    public List<FeedbackItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
