using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class SqlServerSchemaScanner : IDatabaseSchemaScanner
{
    public async Task<DatabaseSchemaScanResult> ScanAsync(
        string connectionString,
        DatabaseProviderType provider,
        CancellationToken cancellationToken = default)
    {
        if (provider != DatabaseProviderType.SqlServer)
        {
            throw new ArgumentException("SqlServerSchemaScanner requires SqlServer provider.", nameof(provider));
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = connection.Database;
        var builder = new SqlConnectionStringBuilder(connectionString);

        var tables = new List<ScannedTable>();
        await using (var tableCmd = connection.CreateCommand())
        {
            tableCmd.CommandText = """
                SELECT TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME
                """;

            await using var reader = await tableCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(new ScannedTable
                {
                    SchemaName = reader.GetString(0),
                    TableName = reader.GetString(1),
                    Columns = Array.Empty<ScannedColumn>()
                });
            }
        }

        foreach (var table in tables)
        {
            table.Columns = await LoadColumnsAsync(connection, table.SchemaName, table.TableName, cancellationToken);
        }

        var relationships = await LoadRelationshipsAsync(connection, cancellationToken);

        return new DatabaseSchemaScanResult
        {
            DatabaseName = databaseName,
            Host = builder.DataSource,
            ScannedAt = DateTime.UtcNow,
            Tables = tables,
            Relationships = relationships
        };
    }

    private static async Task<IReadOnlyList<ScannedColumn>> LoadColumnsAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        var columns = new List<ScannedColumn>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH,
                CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNullable,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsForeignKey,
                c.ORDINAL_POSITION
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
                AND c.TABLE_NAME = pk.TABLE_NAME
                AND c.COLUMN_NAME = pk.COLUMN_NAME
            LEFT JOIN (
                SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                WHERE tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
            ) fk ON c.TABLE_SCHEMA = fk.TABLE_SCHEMA
                AND c.TABLE_NAME = fk.TABLE_NAME
                AND c.COLUMN_NAME = fk.COLUMN_NAME
            WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
            ORDER BY c.ORDINAL_POSITION
            """;
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ScannedColumn
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                MaxLength = reader.IsDBNull(2) ? null : Convert.ToInt32(reader.GetValue(2)),
                IsNullable = reader.GetInt32(3) == 1,
                IsPrimaryKey = reader.GetInt32(4) == 1,
                IsForeignKey = reader.GetInt32(5) == 1,
                OrdinalPosition = reader.GetInt32(6)
            });
        }

        return columns;
    }

    private static async Task<IReadOnlyList<ScannedRelationship>> LoadRelationshipsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var relationships = new List<ScannedRelationship>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                OBJECT_SCHEMA_NAME(fk.parent_object_id) AS FromSchema,
                OBJECT_NAME(fk.parent_object_id) AS FromTable,
                pc.name AS FromColumn,
                OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ToSchema,
                OBJECT_NAME(fk.referenced_object_id) AS ToTable,
                rc.name AS ToColumn
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.columns pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id
            INNER JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
            """;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            relationships.Add(new ScannedRelationship
            {
                FromSchema = reader.GetString(0),
                FromTable = reader.GetString(1),
                FromColumn = reader.GetString(2),
                ToSchema = reader.GetString(3),
                ToTable = reader.GetString(4),
                ToColumn = reader.GetString(5),
                RelationshipType = "OneToMany"
            });
        }

        return relationships;
    }
}
