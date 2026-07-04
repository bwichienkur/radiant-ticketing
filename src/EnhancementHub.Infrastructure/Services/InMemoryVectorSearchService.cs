using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services;

public sealed class InMemoryVectorSearchService : IVectorSearchService
{
    private readonly IEnhancementHubDbContext _dbContext;

    public InMemoryVectorSearchService(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task IndexAsync(string sourceType, Guid sourceId, float[] embedding, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceType, "IndexedFile", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var file = await _dbContext.IndexedFiles.FirstOrDefaultAsync(f => f.Id == sourceId, cancellationToken);
        if (file is null)
        {
            return;
        }

        file.EmbeddingVector = embedding;
        file.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        var files = await _dbContext.IndexedFiles
            .AsNoTracking()
            .Where(f => f.EmbeddingVector != null)
            .ToListAsync(cancellationToken);

        return files
            .Select(f => new VectorSearchResult(f.Id, "IndexedFile", CosineSimilarity(queryEmbedding, f.EmbeddingVector!)))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    public async Task RemoveBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceType, "IndexedFile", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var file = await _dbContext.IndexedFiles.FirstOrDefaultAsync(f => f.Id == sourceId, cancellationToken);
        if (file is null)
        {
            return;
        }

        file.EmbeddingVector = null;
        file.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return 0f;
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0f;
        }

        return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
    }
}
