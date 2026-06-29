using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IAiChatHistoryService
{
    Task<AiChatSession> GetOrCreateSessionAsync(int userId, int postId);
    Task SaveMessageAsync(int sessionId, string role, string content, int? creditsUsed = null);
    Task<List<AiChatSession>> GetUserSessionsAsync(int userId);
    Task<List<AiChatMessage>> GetSessionMessagesAsync(int sessionId, int userId);
}

public class AiChatHistoryService : IAiChatHistoryService
{
    private readonly TobisoDbContext _db;

    public AiChatHistoryService(TobisoDbContext db) => _db = db;

    public async Task<AiChatSession> GetOrCreateSessionAsync(int userId, int postId)
    {
        var session = await _db.AiChatSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.PostId == postId);

        if (session != null)
        {
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return session;
        }

        session = new AiChatSession { UserId = userId, PostId = postId };
        _db.AiChatSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task SaveMessageAsync(int sessionId, string role, string content, int? creditsUsed = null)
    {
        _db.AiChatMessages.Add(new AiChatMessage
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreditsUsed = creditsUsed
        });
        await _db.SaveChangesAsync();
    }

    public Task<List<AiChatSession>> GetUserSessionsAsync(int userId) =>
        _db.AiChatSessions
            .Where(s => s.UserId == userId)
            .Include(s => s.Post)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

    public async Task<List<AiChatMessage>> GetSessionMessagesAsync(int sessionId, int userId)
    {
        var session = await _db.AiChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session == null) return [];

        return await _db.AiChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
