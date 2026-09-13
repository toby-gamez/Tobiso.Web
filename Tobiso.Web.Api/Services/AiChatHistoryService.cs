using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IAiChatHistoryService
{
    /// <summary>Continues the most recently active session for (userId, postId), or creates one if none exists yet.</summary>
    Task<AiChatSession> GetOrCreateSessionAsync(int userId, int? postId);
    /// <summary>Always starts a brand-new session, even if one already exists for (userId, postId) - backs the "new chat" action.</summary>
    Task<AiChatSession> CreateSessionAsync(int userId, int? postId);
    /// <summary>Reads the most recently active existing session without creating one, so opening a chat box doesn't spawn empty history rows.</summary>
    Task<AiChatSession?> FindSessionAsync(int userId, int? postId);
    /// <summary>Reads one specific session by id, scoped to its owner.</summary>
    Task<AiChatSession?> GetSessionByIdAsync(int sessionId, int userId);
    Task SaveMessageAsync(int sessionId, string role, string content, int? creditsUsed = null);
    /// <summary>Replaces the set of attached posts for a session with the given IDs (the session's own PostId is never stored here).</summary>
    Task SaveAttachedPostsAsync(int sessionId, IEnumerable<int> postIds);
    /// <summary>Returns the IDs of posts attached to a session, scoped to its owner.</summary>
    Task<List<int>> GetAttachedPostIdsAsync(int sessionId, int userId);
    Task<List<AiChatSession>> GetUserSessionsAsync(int userId);
    Task<List<AiChatMessage>> GetSessionMessagesAsync(int sessionId, int userId);
}

public class AiChatHistoryService : IAiChatHistoryService
{
    // A pooled factory (rather than the circuit-scoped TobisoDbContext) so a Blazor component using
    // this service can't collide with concurrent DB calls made elsewhere on the same circuit - e.g.
    // AiChatBox's history lookups running alongside its host page's own OnInitializedAsync queries,
    // which previously threw "A second operation was started on this context instance...".
    private readonly IDbContextFactory<TobisoDbContext> _dbFactory;

    public AiChatHistoryService(IDbContextFactory<TobisoDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<AiChatSession> GetOrCreateSessionAsync(int userId, int? postId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Multiple sessions can now exist per (userId, postId) - "new chat" starts an extra one -
        // so pick the most recently active one rather than an arbitrary match.
        var session = await db.AiChatSessions
            .Where(s => s.UserId == userId && s.PostId == postId)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();

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

    public async Task<AiChatSession> CreateSessionAsync(int userId, int? postId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var session = new AiChatSession { UserId = userId, PostId = postId };
        db.AiChatSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task<AiChatSession?> FindSessionAsync(int userId, int? postId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiChatSessions
            .Where(s => s.UserId == userId && s.PostId == postId)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<AiChatSession?> GetSessionByIdAsync(int sessionId, int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
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

        // Keep the session's UpdatedAt fresh so "most recent" lookups and the history sidebar's
        // ordering reflect actual chat activity, not just when the session row was first created.
        var session = await db.AiChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session != null)
        {
            session.UpdatedAt = DateTime.UtcNow;

            // Name the conversation after its opening question, once, the first time one is saved -
            // "Obecná konverzace" for every general chat regardless of content made the history list
            // useless for telling conversations apart.
            if (role == "user" && string.IsNullOrWhiteSpace(session.Title))
                session.Title = BuildTitle(content);
        }

        await db.SaveChangesAsync();
    }

    private static string BuildTitle(string question)
    {
        const int maxLength = 60;
        var trimmed = question.Trim();
        if (trimmed.Length == 0) return "Nová konverzace";
        if (trimmed.Length <= maxLength) return trimmed;

        var cut = trimmed[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > 20) cut = cut[..lastSpace];
        return cut.TrimEnd() + "…";
    }

    // Replaces the whole set of attached posts for a session with the given IDs. Called on every
    // sent message so add/remove changes made in the chat UI are persisted alongside the Q&A text.
    public async Task SaveAttachedPostsAsync(int sessionId, IEnumerable<int> postIds)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var distinct = postIds.Distinct().ToList();
        var existing = await db.AiChatSessionPosts
            .Where(x => x.AiChatSessionId == sessionId)
            .Select(x => x.PostId)
            .ToListAsync();

        var added = distinct.Except(existing).ToList();
        var removed = existing.Except(distinct).ToList();

        if (removed.Count > 0)
            db.AiChatSessionPosts.RemoveRange(
                db.AiChatSessionPosts.Where(x => x.AiChatSessionId == sessionId && removed.Contains(x.PostId)));

        if (added.Count > 0)
            db.AiChatSessionPosts.AddRange(
                added.Select(pid => new AiChatSessionPost { AiChatSessionId = sessionId, PostId = pid }));

        if (added.Count > 0 || removed.Count > 0)
            await db.SaveChangesAsync();
    }

    public async Task<List<int>> GetAttachedPostIdsAsync(int sessionId, int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var owned = await db.AiChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId);
        if (!owned) return [];

        return await db.AiChatSessionPosts
            .Where(x => x.AiChatSessionId == sessionId)
            .Select(x => x.PostId)
            .ToListAsync();
    }

    public async Task<List<AiChatSession>> GetUserSessionsAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiChatSessions
            .Where(s => s.UserId == userId)
            .Include(s => s.Post)
            .Include(s => s.AttachedPosts)
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
