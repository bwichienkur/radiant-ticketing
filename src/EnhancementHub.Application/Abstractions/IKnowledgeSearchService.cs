namespace EnhancementHub.Application.Abstractions;

public sealed record KnowledgeSearchResult(
    Guid ArticleId,
    string Title,
    float Score,
    string Snippet);

public interface IKnowledgeSearchService
{
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string query,
        IReadOnlyDictionary<string, string>? metadataFilters = null,
        int topK = 10,
        CancellationToken cancellationToken = default);
}
