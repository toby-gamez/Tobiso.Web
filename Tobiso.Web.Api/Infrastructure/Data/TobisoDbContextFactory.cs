namespace Tobiso.Api.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Builds standalone <see cref="TobisoDbContext"/> instances outside the normal per-circuit/per-request
/// DI scope. Registered as a singleton with its own <see cref="DbContextOptions{TContext}"/> built directly
/// from configuration - deliberately not EF Core's AddDbContextFactory/AddPooledDbContextFactory helpers,
/// since those add their own DbContextOptions&lt;TobisoDbContext&gt; registration, which conflicts with the
/// scoped one AddDbContext already registers for the same context type.
/// </summary>
public class TobisoDbContextFactory : IDbContextFactory<TobisoDbContext>
{
    private readonly DbContextOptions<TobisoDbContext> _options;

    public TobisoDbContextFactory(IConfiguration configuration)
    {
        _options = new DbContextOptionsBuilder<TobisoDbContext>()
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .Options;
    }

    public TobisoDbContext CreateDbContext() => new(_options);
}
