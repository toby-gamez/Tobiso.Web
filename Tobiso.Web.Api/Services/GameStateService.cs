using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

/// <summary>The submitted state can't be stored (maps to HTTP 400).</summary>
public class InvalidGameStateException(string message) : Exception(message);

public interface IGameStateService
{
    Task<GameStateDto?> GetAsync(int userId);
    /// <summary>Saved on success; otherwise the current server state (409).</summary>
    Task<(GameStateSavedDto? Saved, GameStateDto? Current)> PutAsync(int userId, int baseRevision, JsonNode state);
}

public class GameStateService(IDbContextFactory<TobisoDbContext> dbFactory, ILogger<GameStateService> logger) : IGameStateService
{
    // The blob is never embedded in HTML, so keep BMP text (Czech) unescaped. Characters outside the BMP
    // (emoji) stay escaped, so the stored text can be several times larger than the request body.
    private static readonly JsonSerializerOptions StoreOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public const int MaxStateBytes = 256 * 1024;

    public async Task<GameStateDto?> GetAsync(int userId)
    {
        await using var db = dbFactory.CreateDbContext();
        var row = await db.UserGameStates.AsNoTracking().FirstOrDefaultAsync(g => g.UserId == userId);
        return row == null ? null : ToDto(row);
    }

    public async Task<(GameStateSavedDto?, GameStateDto?)> PutAsync(int userId, int baseRevision, JsonNode state)
    {
        await using var db = dbFactory.CreateDbContext();
        if (state is not JsonObject) throw new InvalidGameStateException("state musí být JSON objekt.");
        string json;
        try { json = state.ToJsonString(StoreOptions); }
        catch (InvalidOperationException) { throw new InvalidGameStateException("state obsahuje neplatný unicode řetězec."); }
        var now = DateTime.UtcNow;

        if (baseRevision == 0)
        {
            db.UserGameStates.Add(new UserGameState
                { UserId = userId, Revision = 1, Json = json, UpdatedAt = now });
            try
            {
                await db.SaveChangesAsync();
                return (new GameStateSavedDto(1, now), null);
            }
            catch (DbUpdateException ex)
            {
                var current = await GetAsync(userId);
                // Only a lost race (the row now exists) is a conflict; anything else is a real failure.
                if (current == null) throw;
                logger.LogInformation(ex, "Game state insert for user {UserId} lost a race", userId);
                return (null, current);
            }
        }

        var rows = await db.UserGameStates
            .Where(g => g.UserId == userId && g.Revision == baseRevision)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.Revision, baseRevision + 1)
                .SetProperty(g => g.Json, json)
                .SetProperty(g => g.UpdatedAt, now));
        if (rows == 1) return (new GameStateSavedDto(baseRevision + 1, now), null);
        return (null, await GetAsync(userId));
    }

    private static GameStateDto ToDto(UserGameState g) =>
        new(g.Revision, DateTime.SpecifyKind(g.UpdatedAt, DateTimeKind.Utc), JsonNode.Parse(g.Json)!);
}
