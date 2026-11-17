using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;

namespace Tobiso.Web.Api.Services;

public interface IAccountDeletionService
{
    /// <summary>
    /// Permanently deletes the account. Everything keyed to the user (bookmarks, notes, read progress, question attempts,
    /// AI chats and credits, game state, points ledger, activity days, class membership, own events) is removed by the
    /// database's cascading foreign keys. Returns false if the user doesn't exist.
    /// </summary>
    Task<bool> DeleteAsync(int userId);
}

public class AccountDeletionService(IDbContextFactory<TobisoDbContext> dbFactory) : IAccountDeletionService
{
    public async Task<bool> DeleteAsync(int userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync() > 0;
    }
}
