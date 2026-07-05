using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EnhancementHub.Infrastructure.Services;

internal static class TenantSchemaProvisioner
{
    public static async Task ProvisionAsync(
        IEnhancementHubDbContext dbContext,
        string schemaName,
        IReadOnlyList<string> controlPlaneTables,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (dbContext is not DbContext context)
        {
            throw new InvalidOperationException("Schema provisioning requires a concrete DbContext.");
        }

        var provider = context.Database.ProviderName ?? string.Empty;
        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Skipping PostgreSQL schema provisioning for {Schema} on SQLite provider",
                schemaName);
            return;
        }

        if (!provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Dedicated schema provisioning requires PostgreSQL.");
        }

        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var createSchema = connection.CreateCommand();
        createSchema.CommandText = $"""CREATE SCHEMA IF NOT EXISTS "{schemaName}";""";
        await createSchema.ExecuteNonQueryAsync(cancellationToken);

        var excluded = controlPlaneTables
            .Select(t => t.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var tableNames = new List<string>();
        await using (var listTables = connection.CreateCommand())
        {
            listTables.CommandText = """
                SELECT tablename
                FROM pg_tables
                WHERE schemaname = 'public'
                ORDER BY tablename;
                """;
            await using var reader = await listTables.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var tableName = reader.GetString(0);
                if (!excluded.Contains(tableName.ToLowerInvariant()))
                {
                    tableNames.Add(tableName);
                }
            }
        }

        foreach (var tableName in tableNames)
        {
            await using var createTable = connection.CreateCommand();
            createTable.CommandText = $"""
                CREATE TABLE IF NOT EXISTS "{schemaName}"."{tableName}"
                (LIKE public."{tableName}" INCLUDING ALL);
                """;
            await createTable.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation(
            "Ensured {TableCount} tenant tables exist in schema {Schema}",
            tableNames.Count,
            schemaName);
    }

    internal static string BuildSearchPathSql(string schemaName)
    {
        if (!TenantSchemaNameResolver.IsValidSchemaName(schemaName))
        {
            throw new InvalidOperationException("Invalid tenant schema name.");
        }

        return $"""SET search_path TO "{schemaName}", public;""";
    }
}
