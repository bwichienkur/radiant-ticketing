namespace EnhancementHub.Application.Abstractions;

public sealed record VectorSearchResult(
    Guid SourceId,
    string SourceType,
    float Score);

public interface IVectorSearchService
{
    Task IndexAsync(string sourceType, Guid sourceId, float[] embedding, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK = 10, CancellationToken cancellationToken = default);
    Task RemoveBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default);
}
