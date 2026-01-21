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
}

public class FeedbackService : IFeedbackService
{
    private readonly TobisoDbContext _context;

    public FeedbackService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<FeedbackResponse>> GetAll()
    {
        var feedbacks = await _context.Feedbacks
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
            
        return feedbacks.Select(f => new FeedbackResponse
        {
            Id = f.Id,
            Name = f.Name,
            Email = f.Email,
            Message = f.Message,
            CreatedAt = f.CreatedAt,
            IsRead = f.IsRead
        }).ToList();
    }

    public async Task<FeedbackResponse?> GetById(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null)
            return null;

        return new FeedbackResponse
        {
            Id = feedback.Id,
            Name = feedback.Name,
            Email = feedback.Email,
            Message = feedback.Message,
            CreatedAt = feedback.CreatedAt,
            IsRead = feedback.IsRead
        };
    }

    public async Task<FeedbackResponse?> Create(CreateFeedbackDto dto)
    {
        var feedback = new Feedback
        {
            Name = dto.Name,
            Email = dto.Email,
            Message = dto.Message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        return new FeedbackResponse
        {
            Id = feedback.Id,
            Name = feedback.Name,
            Email = feedback.Email,
            Message = feedback.Message,
            CreatedAt = feedback.CreatedAt,
            IsRead = feedback.IsRead
        };
    }

    public async Task<bool> MarkAsRead(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null)
            return false;

        feedback.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null)
            return false;

        _context.Feedbacks.Remove(feedback);
        await _context.SaveChangesAsync();
        return true;
    }
}
