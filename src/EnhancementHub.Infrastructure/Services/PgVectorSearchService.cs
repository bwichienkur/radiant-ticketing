using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Pgvector;

namespace EnhancementHub.Infrastructure.Services;

public sealed class PgVectorSearchService : IVectorSearchService
{
    private readonly string _connectionString;
    private readonly int _dimensions;

    public PgVectorSearchService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required for pgvector search.");
        _dimensions = configuration.GetValue("VectorSearch:Dimensions", 64);
    }

    public async Task IndexAsync(string sourceType, Guid sourceId, float[] embedding, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceType, "IndexedFile", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await EnsureInfrastructureAsync(cancellationToken);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO indexed_file_embeddings ("IndexedFileId", "Embedding", "UpdatedAt")
            VALUES (@id, @embedding, @updatedAt)
            ON CONFLICT ("IndexedFileId")
            DO UPDATE SET "Embedding" = EXCLUDED."Embedding", "UpdatedAt" = EXCLUDED."UpdatedAt"
            """;
        command.Parameters.AddWithValue("id", sourceId);
        command.Parameters.AddWithValue("embedding", new Vector(Normalize(embedding)));
        command.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        await EnsureInfrastructureAsync(cancellationToken);

        var results = new List<VectorSearchResult>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT "IndexedFileId", 1 - ("Embedding" <=> @query) AS score
            FROM indexed_file_embeddings
            ORDER BY "Embedding" <=> @query
            LIMIT {topK}
            """;
        command.Parameters.AddWithValue("query", new Vector(Normalize(queryEmbedding)));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new VectorSearchResult(reader.GetGuid(0), "IndexedFile", reader.GetFloat(1)));
        }

        return results;
    }

    public async Task RemoveBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceType, "IndexedFile", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """DELETE FROM indexed_file_embeddings WHERE "IndexedFileId" = @id""";
        command.Parameters.AddWithValue("id", sourceId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureInfrastructureAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var extension = connection.CreateCommand())
        {
            extension.CommandText = "CREATE EXTENSION IF NOT EXISTS vector";
            await extension.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var create = connection.CreateCommand();
        create.CommandText = $"""
            CREATE TABLE IF NOT EXISTS indexed_file_embeddings (
                "IndexedFileId" uuid PRIMARY KEY,
                "Embedding" vector({_dimensions}) NOT NULL,
                "UpdatedAt" timestamptz NOT NULL
            )
            """;
        await create.ExecuteNonQueryAsync(cancellationToken);
    }

    private float[] Normalize(float[] embedding)
    {
        if (embedding.Length == _dimensions)
        {
            return embedding;
        }

        var normalized = new float[_dimensions];
        for (var i = 0; i < _dimensions; i++)
        {
            normalized[i] = embedding[i % embedding.Length];
        }

        return normalized;
    }
}
