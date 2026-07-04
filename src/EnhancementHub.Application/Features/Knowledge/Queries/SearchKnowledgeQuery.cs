using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using MediatR;

namespace EnhancementHub.Application.Features.Knowledge.Queries;

public sealed record KnowledgeSearchResultDto(
    Guid ArticleId,
    string Title,
    float Score,
    string Snippet);

public sealed record SearchKnowledgeQuery(
    string Query,
    IReadOnlyDictionary<string, string>? MetadataFilters = null,
    int TopK = 10) : IRequest<IReadOnlyList<KnowledgeSearchResultDto>>;

public sealed class SearchKnowledgeQueryHandler
    : IRequestHandler<SearchKnowledgeQuery, IReadOnlyList<KnowledgeSearchResultDto>>
{
    private readonly IKnowledgeSearchService _knowledgeSearchService;

    public SearchKnowledgeQueryHandler(IKnowledgeSearchService knowledgeSearchService)
    {
        _knowledgeSearchService = knowledgeSearchService;
    }

    public async Task<IReadOnlyList<KnowledgeSearchResultDto>> Handle(
        SearchKnowledgeQuery request,
        CancellationToken cancellationToken)
    {
        var results = await _knowledgeSearchService.SearchAsync(
            request.Query,
            request.MetadataFilters,
            request.TopK,
            cancellationToken);

        return results
            .Select(r => new KnowledgeSearchResultDto(r.ArticleId, r.Title, r.Score, r.Snippet))
            .ToList();
    }
}
