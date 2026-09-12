using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IAiChatHistoryService
{
    Task<AiChatSession> GetOrCreateSessionAsync(int userId, int? postId);
    /// <summary>Reads an existing session without creating one, so opening a chat box doesn't spawn empty history rows.</summary>
    Task<AiChatSession?> FindSessionAsync(int userId, int? postId);
    Task SaveMessageAsync(int sessionId, string role, string content, int? creditsUsed = null);
    Task<List<AiChatSession>> GetUserSessionsAsync(int userId);
    Task<List<AiChatMessage>> GetSessionMessagesAsync(int sessionId, int userId);
}

public class AiChatHistoryService : IAiChatHistoryService
{
    // A pooled factory (rather than the circuit-scoped TobisoDbContext) so a Blazor component using
    // this service can't collide with concurrent DB calls made elsewhere on the same circuit — e.g.
    // AiChatBox's history lookups running alongside its host page's own OnInitializedAsync queries,
    // which previously threw "A second operation was started on this context instance...".
    private readonly IDbContextFactory<TobisoDbContext> _dbFactory;

    public AiChatHistoryService(IDbContextFactory<TobisoDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<AiChatSession> GetOrCreateSessionAsync(int userId, int? postId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var session = await db.AiChatSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.PostId == postId);

        if (session != null)
        {
            session.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return session;
        }

        session = new AiChatSession { UserId = userId, PostId = postId };
        db.AiChatSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task<AiChatSession?> FindSessionAsync(int userId, int? postId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiChatSessions.FirstOrDefaultAsync(s => s.UserId == userId && s.PostId == postId);
    }

    public async Task SaveMessageAsync(int sessionId, string role, string content, int? creditsUsed = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.AiChatMessages.Add(new AiChatMessage
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreditsUsed = creditsUsed
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<AiChatSession>> GetUserSessionsAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiChatSessions
            .Where(s => s.UserId == userId)
            .Include(s => s.Post)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<AiChatMessage>> GetSessionMessagesAsync(int sessionId, int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var session = await db.AiChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session == null) return [];

        return await db.AiChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
