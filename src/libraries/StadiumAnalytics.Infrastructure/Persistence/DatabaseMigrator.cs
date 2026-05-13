using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace StadiumAnalytics.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    private const long MigrationLockKey = 727274L;

    public static async Task MigrateWithLockAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        logger.LogInformation("Acquiring migration advisory lock {Key}.", MigrationLockKey);
        await using (var acquire = new NpgsqlCommand($"SELECT pg_advisory_lock({MigrationLockKey})", connection))
        {
            await acquire.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count == 0)
            {
                logger.LogInformation("Database schema is up to date; no migrations to apply.");
                return;
            }

            logger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));

            await db.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Database migrations applied successfully.");
        }
        finally
        {
            await using var release = new NpgsqlCommand($"SELECT pg_advisory_unlock({MigrationLockKey})", connection);
            await release.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
