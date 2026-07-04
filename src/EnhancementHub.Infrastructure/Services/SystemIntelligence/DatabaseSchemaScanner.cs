using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using Microsoft.Data.Sqlite;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class DatabaseSchemaScanner : IDatabaseSchemaScanner
{
    private readonly DatabaseSchemaScannerFactory _factory;

    public DatabaseSchemaScanner(DatabaseSchemaScannerFactory factory)
    {
        _factory = factory;
    }

    public Task<DatabaseSchemaScanResult> ScanAsync(
        string connectionString,
        DatabaseProviderType provider,
        CancellationToken cancellationToken = default) =>
        provider switch
        {
            DatabaseProviderType.Sqlite => ScanSqliteAsync(connectionString, cancellationToken),
            DatabaseProviderType.SqlServer or DatabaseProviderType.PostgreSQL =>
                _factory.ScanAsync(connectionString, provider, cancellationToken),
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported.")
        };

    private static async Task<DatabaseSchemaScanResult> ScanSqliteAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var tables = new List<ScannedTable>();
        var tableNames = new List<string>();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        var relationships = new List<ScannedRelationship>();

        foreach (var tableName in tableNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var columns = new List<ScannedColumn>();
            var escaped = tableName.Replace("'", "''");

            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info('{escaped}')";
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    columns.Add(new ScannedColumn
                    {
                        Name = reader.GetString(1),
                        DataType = reader.GetString(2),
                        IsNullable = reader.GetInt32(3) == 0,
                        IsPrimaryKey = reader.GetInt32(5) == 1,
                        OrdinalPosition = reader.GetInt32(0)
                    });
                }
            }

            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA foreign_key_list('{escaped}')";
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    relationships.Add(new ScannedRelationship
                    {
                        FromSchema = "main",
                        FromTable = tableName,
                        FromColumn = reader.GetString(3),
                        ToSchema = "main",
                        ToTable = reader.GetString(2),
                        ToColumn = reader.GetString(4)
                    });
                }
            }

            tables.Add(new ScannedTable
            {
                SchemaName = "main",
                TableName = tableName,
                Columns = columns
            });
        }

        return new DatabaseSchemaScanResult
        {
            DatabaseName = connection.Database,
            Tables = tables,
            Relationships = relationships
        };
    }
}
