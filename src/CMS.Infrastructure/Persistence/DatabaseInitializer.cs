using CMS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and runs idempotent seeding. Resolved in a scope at
/// startup by the Web host. Kept as a service (rather than inline in Program) so
/// it can be unit/integration tested and reused by tooling.
/// </summary>
public sealed class DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Seeding database...");
            await DbSeeder.SeedAsync(context, cancellationToken);

            logger.LogInformation("Database initialization complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }
}
