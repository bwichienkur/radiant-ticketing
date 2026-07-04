using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using Npgsql;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class PostgreSqlSchemaScanner : IDatabaseSchemaScanner
{
    public async Task<DatabaseSchemaScanResult> ScanAsync(
        string connectionString,
        DatabaseProviderType provider,
        CancellationToken cancellationToken = default)
    {
        if (provider != DatabaseProviderType.PostgreSQL)
        {
            throw new ArgumentException("PostgreSqlSchemaScanner requires PostgreSQL provider.", nameof(provider));
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;

        var tables = new List<ScannedTable>();
        await using (var tableCmd = connection.CreateCommand())
        {
            tableCmd.CommandText = """
                SELECT table_schema, table_name
                FROM information_schema.tables
                WHERE table_type = 'BASE TABLE'
                  AND table_schema NOT IN ('pg_catalog', 'information_schema')
                ORDER BY table_schema, table_name
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
            Host = builder.Host,
            ScannedAt = DateTime.UtcNow,
            Tables = tables,
            Relationships = relationships
        };
    }

    private static async Task<IReadOnlyList<ScannedColumn>> LoadColumnsAsync(
        NpgsqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        var columns = new List<ScannedColumn>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                c.column_name,
                c.data_type,
                c.character_maximum_length,
                CASE WHEN c.is_nullable = 'YES' THEN true ELSE false END,
                EXISTS (
                    SELECT 1 FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                        ON tc.constraint_name = kcu.constraint_name
                        AND tc.table_schema = kcu.table_schema
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                      AND kcu.table_schema = c.table_schema
                      AND kcu.table_name = c.table_name
                      AND kcu.column_name = c.column_name
                ) AS is_pk,
                EXISTS (
                    SELECT 1 FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                        ON tc.constraint_name = kcu.constraint_name
                        AND tc.table_schema = kcu.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY'
                      AND kcu.table_schema = c.table_schema
                      AND kcu.table_name = c.table_name
                      AND kcu.column_name = c.column_name
                ) AS is_fk,
                c.ordinal_position
            FROM information_schema.columns c
            WHERE c.table_schema = @schema AND c.table_name = @table
            ORDER BY c.ordinal_position
            """;
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ScannedColumn
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                MaxLength = reader.IsDBNull(2) ? null : Convert.ToInt32(reader.GetValue(2)),
                IsNullable = reader.GetBoolean(3),
                IsPrimaryKey = reader.GetBoolean(4),
                IsForeignKey = reader.GetBoolean(5),
                OrdinalPosition = reader.GetInt32(6)
            });
        }

        return columns;
    }

    private static async Task<IReadOnlyList<ScannedRelationship>> LoadRelationshipsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var relationships = new List<ScannedRelationship>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                ns_src.nspname AS from_schema,
                cls_src.relname AS from_table,
                att_src.attname AS from_column,
                ns_tgt.nspname AS to_schema,
                cls_tgt.relname AS to_table,
                att_tgt.attname AS to_column
            FROM pg_constraint con
            JOIN pg_class cls_src ON con.conrelid = cls_src.oid
            JOIN pg_namespace ns_src ON cls_src.relnamespace = ns_src.oid
            JOIN pg_class cls_tgt ON con.confrelid = cls_tgt.oid
            JOIN pg_namespace ns_tgt ON cls_tgt.relnamespace = ns_tgt.oid
            JOIN unnest(con.conkey) WITH ORDINALITY AS src_cols(attnum, ord) ON true
            JOIN unnest(con.confkey) WITH ORDINALITY AS tgt_cols(attnum, ord) ON src_cols.ord = tgt_cols.ord
            JOIN pg_attribute att_src ON att_src.attrelid = cls_src.oid AND att_src.attnum = src_cols.attnum
            JOIN pg_attribute att_tgt ON att_tgt.attrelid = cls_tgt.oid AND att_tgt.attnum = tgt_cols.attnum
            WHERE con.contype = 'f'
              AND ns_src.nspname NOT IN ('pg_catalog', 'information_schema')
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
