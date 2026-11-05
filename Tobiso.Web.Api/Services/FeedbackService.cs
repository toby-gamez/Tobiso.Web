using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IFeedbackService
{
    Task<List<FeedbackResponse>> GetAll();
    Task<FeedbackResponse?> GetById(int id);
    Task<FeedbackResponse?> Create(CreateFeedbackDto dto);
    Task<bool> MarkAsRead(int id);
    Task<bool> Delete(int id);
    Task<PagedFeedbackResponse> GetPaged(string? platform, string? type, string? status, int page, int pageSize);
    Task<FeedbackItemResponse?> GetItemById(int id);
    Task<FeedbackItemResponse?> Patch(int id, UpdateFeedbackRequest request);
}

public class FeedbackService : IFeedbackService
{
    private readonly TobisoDbContext _context;
    private readonly IPushNotificationService _push;

    public FeedbackService(TobisoDbContext context, IPushNotificationService push)
    {
        _context = context;
        _push = push;
    }

    public async Task<List<FeedbackResponse>> GetAll()
    {
        var feedbacks = await _context.Feedbacks.OrderByDescending(f => f.CreatedAt).ToListAsync();
        return feedbacks.Select(ToResponse).ToList();
    }

    public async Task<FeedbackResponse?> GetById(int id)
    {
        var f = await _context.Feedbacks.FindAsync(id);
        return f == null ? null : ToResponse(f);
    }

    public async Task<FeedbackResponse?> Create(CreateFeedbackDto dto)
    {
        // Same bounds as CreateFeedbackDto's attributes, enforced here too because the Blazor page
        // calls this service directly, without model validation.
        static string Cap(string? v, int max) => string.IsNullOrEmpty(v) ? string.Empty : v.Length <= max ? v : v[..max];
        var message = Cap(dto.Message, 5000);
        var title = string.IsNullOrWhiteSpace(dto.Title)
            ? message[..Math.Min(message.Length, 80)]
            : Cap(dto.Title, 200);

        var feedback = new Feedback
        {
            Name      = Cap(dto.Name, 100),
            Email     = Cap(dto.Email, 256),
            Title     = title,
            Message   = message,
            Platform  = Cap(dto.Platform, 50),
            Type      = dto.Type,
            Status    = FeedbackStatus.New,
            CreatedAt = DateTime.UtcNow,
            IsRead    = false,
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        _ = _push.SendFeedbackNotificationAsync(feedback);

        return ToResponse(feedback);
    }

    public async Task<bool> MarkAsRead(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null) return false;
        feedback.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null) return false;
        _context.Feedbacks.Remove(feedback);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PagedFeedbackResponse> GetPaged(string? platform, string? type, string? status, int page, int pageSize)
    {
        var query = _context.Feedbacks.AsQueryable();

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(f => f.Platform == platform);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<FeedbackType>(type, out var parsedType))
            query = query.Where(f => f.Type == parsedType);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<FeedbackStatus>(status, out var parsedStatus))
            query = query.Where(f => f.Status == parsedStatus);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedFeedbackResponse
        {
            Items      = items.Select(ToItemResponse).ToList(),
            Page       = page,
            PageSize   = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<FeedbackItemResponse?> GetItemById(int id)
    {
        var f = await _context.Feedbacks.FindAsync(id);
        return f == null ? null : ToItemResponse(f);
    }

    public async Task<FeedbackItemResponse?> Patch(int id, UpdateFeedbackRequest request)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null) return null;

        if (request.Status.HasValue)  feedback.Status    = request.Status.Value;
        if (request.AdminNote != null) feedback.AdminNote = request.AdminNote;

        await _context.SaveChangesAsync();
        return ToItemResponse(feedback);
    }

    private static FeedbackResponse ToResponse(Feedback f) => new()
    {
        Id        = f.Id,
        Name      = f.Name,
        Email     = f.Email,
        Message   = f.Message,
        Platform  = f.Platform,
        CreatedAt = f.CreatedAt,
        IsRead    = f.IsRead,
    };

    private static FeedbackItemResponse ToItemResponse(Feedback f) => new()
    {
        Id          = f.Id.ToString(),
        Type        = f.Type,
        Platform    = f.Platform,
        Title       = f.Title,
        Description = f.Message,
        Name        = f.Name,
        Email       = f.Email,
        SubmittedAt = f.CreatedAt,
        Status      = f.Status,
        AdminNote   = f.AdminNote,
    };
}
