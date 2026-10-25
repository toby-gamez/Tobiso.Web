using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public enum PointsClaimStatus { Ok, UnknownItem, InsufficientPoints, DailyLimit, Conflict }

public interface IGameStateService
{
    Task<GameStateDto?> GetAsync(int userId);
    /// <summary>Saved on success; otherwise the current server state (409).</summary>
    Task<(GameStateSavedDto? Saved, GameStateDto? Current)> PutAsync(int userId, int baseRevision, JsonNode state);
    Task<(PointsClaimStatus Status, PointsClaimResultDto? Result)> ClaimAsync(int userId, string reason, string reference);
}

public class GameStateService(IDbContextFactory<TobisoDbContext> dbFactory, ILogger<GameStateService> logger) : IGameStateService
{
    // The blob is never embedded in HTML, so keep non-ASCII (Czech) text unescaped to stay within MaxStateBytes.
    private static readonly JsonSerializerOptions StoreOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public const int MaxStateBytes = 256 * 1024;
    private const string ExchangeReason = "points_exchange";

    // Server-side price list for the points -> AI credits exchange, keyed by the app's shop item id.
    // The client never sends a price or an amount, so it can't pick its own rate.
    private static readonly Dictionary<string, (int Cost, int Credits)> AiCreditPacks = new()
    {
        ["80"] = (40, 5),
        ["81"] = (70, 10),
    };
    private const int MaxExchangedCreditsPerDay = 20;

    public async Task<GameStateDto?> GetAsync(int userId)
    {
        await using var db = dbFactory.CreateDbContext();
        var row = await db.UserGameStates.AsNoTracking().FirstOrDefaultAsync(g => g.UserId == userId);
        return row == null ? null : ToDto(row);
    }

    public async Task<(GameStateSavedDto?, GameStateDto?)> PutAsync(int userId, int baseRevision, JsonNode state)
    {
        await using var db = dbFactory.CreateDbContext();
        var json = state.ToJsonString(StoreOptions);
        // MVC binds with case-insensitive names; re-parse so lookups here and in ClaimAsync are case-sensitive alike.
        state = JsonNode.Parse(json)!;
        var points = ReadPoints(state);
        var now = DateTime.UtcNow;

        if (baseRevision == 0)
        {
            db.UserGameStates.Add(new UserGameState
                { UserId = userId, Revision = 1, Json = json, Points = points, UpdatedAt = now });
            try
            {
                await db.SaveChangesAsync();
                return (new GameStateSavedDto(1, now), null);
            }
            catch (DbUpdateException ex)
            {
                db.ChangeTracker.Clear();
                var current = await db.UserGameStates.AsNoTracking().FirstOrDefaultAsync(g => g.UserId == userId);
                // Only a lost race (the row now exists) is a conflict; anything else is a real failure.
                if (current == null) throw;
                logger.LogInformation(ex, "Game state insert for user {UserId} lost a race", userId);
                return (null, ToDto(current));
            }
        }

        var rows = await db.UserGameStates
            .Where(g => g.UserId == userId && g.Revision == baseRevision)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.Revision, baseRevision + 1)
                .SetProperty(g => g.Json, json)
                .SetProperty(g => g.Points, points)
                .SetProperty(g => g.UpdatedAt, now));
        if (rows == 1) return (new GameStateSavedDto(baseRevision + 1, now), null);
        var latest = await db.UserGameStates.AsNoTracking().FirstOrDefaultAsync(g => g.UserId == userId);
        return (null, latest == null ? null : ToDto(latest));
    }

    // Points are written by the client, so this is damage control rather than proof: the price and
    // credit amount come from the server, the deduction and the credit grant are one transaction,
    // and the number of credits obtainable per day is capped.
    public async Task<(PointsClaimStatus, PointsClaimResultDto?)> ClaimAsync(int userId, string reason, string reference)
    {
        if (reason != "ai_credits" || !AiCreditPacks.TryGetValue(reference, out var pack))
            return (PointsClaimStatus.UnknownItem, null);

        await using var db = dbFactory.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();

        var row = await db.UserGameStates.AsNoTracking().FirstOrDefaultAsync(g => g.UserId == userId);
        if (row == null || row.Points < pack.Cost) return (PointsClaimStatus.InsufficientPoints, null);

        var today = DateTime.UtcNow.Date;
        var exchangedToday = await db.AiCreditTransactions
            .Where(t => t.UserId == userId && t.Reason == ExchangeReason && t.CreatedAt >= today)
            .SumAsync(t => (int?)t.Delta) ?? 0;
        if (exchangedToday + pack.Credits > MaxExchangedCreditsPerDay) return (PointsClaimStatus.DailyLimit, null);

        // Keep the blob consistent with the column so the client sees the spent points on its next GET.
        var node = JsonNode.Parse(row.Json) as JsonObject ?? new JsonObject();
        var newPoints = row.Points - pack.Cost;
        if (node["points"] is not JsonObject pointsNode) node["points"] = pointsNode = new JsonObject();
        pointsNode["value"] = newPoints;

        var now = DateTime.UtcNow;
        var json = node.ToJsonString(StoreOptions);
        var revision = row.Revision;
        var rows = await db.UserGameStates
            .Where(g => g.UserId == userId && g.Revision == revision)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.Revision, revision + 1)
                .SetProperty(g => g.Json, json)
                .SetProperty(g => g.Points, newPoints)
                .SetProperty(g => g.UpdatedAt, now));
        if (rows == 0) return (PointsClaimStatus.Conflict, null);

        await db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Credits, u => u.Credits + pack.Credits));
        db.AiCreditTransactions.Add(new AiCreditTransaction
            { UserId = userId, Delta = pack.Credits, Reason = ExchangeReason });
        await db.SaveChangesAsync();
        var credits = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Credits).FirstAsync();
        await tx.CommitAsync();

        return (PointsClaimStatus.Ok, new PointsClaimResultDto(credits, newPoints, revision + 1));
    }

    private static GameStateDto ToDto(UserGameState g) =>
        new(g.Revision, DateTime.SpecifyKind(g.UpdatedAt, DateTimeKind.Utc), JsonNode.Parse(g.Json)!);

    private static double ReadPoints(JsonNode state)
    {
        if (state["points"] is JsonObject p && p["value"] is JsonValue v && v.TryGetValue<double>(out var d) && double.IsFinite(d))
            return Math.Max(0, d);
        return 0;
    }
}
