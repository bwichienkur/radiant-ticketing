using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Application.Features.Knowledge.Queries;
using EnhancementHub.Application.Features.Search.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Search.Queries;

public sealed record GlobalEntitySearchQuery(string Query, int Limit = 20, bool Semantic = false)
    : IRequest<GlobalSearchResultDto>;

public sealed class GlobalEntitySearchQueryHandler
    : IRequestHandler<GlobalEntitySearchQuery, GlobalSearchResultDto>
{
    private readonly IMediator _mediator;
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _applicationAccess;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IFeatureService _featureService;

    public GlobalEntitySearchQueryHandler(
        IMediator mediator,
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService applicationAccess,
        IVectorSearchService vectorSearch,
        IFeatureService featureService)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _applicationAccess = applicationAccess;
        _vectorSearch = vectorSearch;
        _featureService = featureService;
    }

    public async Task<GlobalSearchResultDto> Handle(
        GlobalEntitySearchQuery request,
        CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            return new GlobalSearchResultDto(query, [], new Dictionary<string, IReadOnlyList<GlobalSearchItemDto>>());
        }

        var term = query.ToLowerInvariant();
        var limit = Math.Clamp(request.Limit, 1, 50);
        var semantic = request.Semantic && _featureService.IsEnabled(FeatureFlags.SemanticSearch);
        var items = new List<GlobalSearchItemDto>();

        if (semantic)
        {
            items.AddRange(await SearchVectorArtifactsAsync(term, cancellationToken));
            items.AddRange(await SearchKnowledgeAsync(query, cancellationToken));
            items.AddRange(await SearchSymbolsAsync(term, cancellationToken));
            items.AddRange(await SearchRequestsAsync(term, cancellationToken));
            items.AddRange(await SearchApplicationsAsync(term, cancellationToken));
            items.AddRange(await SearchRepositoriesAsync(term, cancellationToken));
            items.AddRange(await SearchDriftFindingsAsync(term, cancellationToken));
            items.AddRange(SearchPages(term));
        }
        else
        {
            items.AddRange(SearchPages(term));
            items.AddRange(await SearchRequestsAsync(term, cancellationToken));
            items.AddRange(await SearchApplicationsAsync(term, cancellationToken));
            items.AddRange(await SearchRepositoriesAsync(term, cancellationToken));
            items.AddRange(await SearchDriftFindingsAsync(term, cancellationToken));
            items.AddRange(await SearchSymbolsAsync(term, cancellationToken));
            items.AddRange(await SearchKnowledgeAsync(query, cancellationToken));
            items.AddRange(await SearchVectorArtifactsAsync(term, cancellationToken));
        }

        var deduped = items
            .GroupBy(i => $"{i.Type}:{i.Url}:{i.Title}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(i => i.Score).First())
            .OrderByDescending(i => i.Score)
            .ThenBy(i => i.Type, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();

        var groups = deduped
            .GroupBy(i => i.Type, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<GlobalSearchItemDto>)g.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var semanticHint = semantic ? BuildSemanticHint(deduped) : null;

        return new GlobalSearchResultDto(query, deduped, groups, semanticHint);
    }

    private static string? BuildSemanticHint(IReadOnlyList<GlobalSearchItemDto> items)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var top = items.Take(3).Select(i => i.Title);
        return $"Semantic matches across portfolio: {string.Join(" · ", top)}";
    }

    private static IEnumerable<GlobalSearchItemDto> SearchPages(string term) =>
        GlobalSearchPages.All
            .Where(page =>
                page.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || page.Keywords.Any(keyword => keyword.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(5)
            .Select(page => new GlobalSearchItemDto(
                "page",
                page.Title,
                "Navigate",
                page.Url,
                0.9f));

    private async Task<IEnumerable<GlobalSearchItemDto>> SearchRequestsAsync(
        string term,
        CancellationToken cancellationToken)
    {
        var requests = (await _mediator.Send(
            new ListEnhancementRequestsQuery(Search: term, PageSize: 8),
            cancellationToken)).Items;

        return requests.Select(request => new GlobalSearchItemDto(
            "request",
            request.Title,
            $"{request.Status} · {request.Priority}",
            $"/Spa/RequestDetail/{request.Id}",
            1.0f));
    }

    private async Task<IEnumerable<GlobalSearchItemDto>> SearchApplicationsAsync(
        string term,
        CancellationToken cancellationToken)
    {
        var applications = await _applicationAccess
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Where(app =>
                app.Name.ToLower().Contains(term)
                || (app.BusinessDomain != null && app.BusinessDomain.ToLower().Contains(term))
                || (app.Purpose != null && app.Purpose.ToLower().Contains(term))
                || (app.Description != null && app.Description.ToLower().Contains(term)))
            .OrderBy(app => app.Name)
            .Take(5)
            .ToListAsync(cancellationToken);

        return applications.Select(app => new GlobalSearchItemDto(
            "application",
            app.Name,
            app.BusinessDomain ?? "Application",
            "/Spa/Applications",
            0.95f));
    }

    private async Task<IEnumerable<GlobalSearchItemDto>> SearchRepositoriesAsync(
        string term,
        CancellationToken cancellationToken)
    {
        var accessibleApplicationIds = _applicationAccess
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Select(app => app.Id);

        var repositories = await _dbContext.Repositories
            .AsNoTracking()
            .Include(repo => repo.Application)
            .Where(repo => accessibleApplicationIds.Contains(repo.ApplicationId))
            .Where(repo =>
                repo.Name.ToLower().Contains(term)
                || repo.Url.ToLower().Contains(term))
            .OrderBy(repo => repo.Name)
            .Take(5)
            .ToListAsync(cancellationToken);

        return repositories.Select(repo => new GlobalSearchItemDto(
            "repository",
            repo.Name,
            repo.Application.Name,
            "/Spa/Repositories",
            0.9f));
    }

    private async Task<IEnumerable<GlobalSearchItemDto>> SearchDriftFindingsAsync(
        string term,
        CancellationToken cancellationToken)
    {
        var accessibleApplicationIds = _applicationAccess
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Select(app => app.Id);

        var findings = await _dbContext.SchemaDriftFindings
            .AsNoTracking()
            .Include(finding => finding.DatabaseConnection)
            .Where(finding => !finding.IsResolved)
            .Where(finding => accessibleApplicationIds.Contains(finding.DatabaseConnection.ApplicationId))
            .Where(finding =>
                finding.Title.ToLower().Contains(term)
                || finding.Description.ToLower().Contains(term)
                || (finding.CodeReference != null && finding.CodeReference.ToLower().Contains(term))
                || (finding.DatabaseReference != null && finding.DatabaseReference.ToLower().Contains(term)))
            .OrderByDescending(finding => finding.Severity)
            .ThenByDescending(finding => finding.DetectedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        return findings.Select(finding => new GlobalSearchItemDto(
            "drift",
            finding.Title,
            $"{finding.Severity} · {finding.DatabaseConnection.Name}",
            $"/Spa/SchemaDrift?connectionId={finding.DatabaseConnectionId}",
            0.85f));
    }

    private async Task<IEnumerable<GlobalSearchItemDto>> SearchSymbolsAsync(
        string term,
        CancellationToken cancellationToken)
    {
        var accessibleApplicationIds = _applicationAccess
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Select(app => app.Id);

        var symbols = await _dbContext.IndexedSymbols
            .AsNoTracking()
            .Include(symbol => symbol.IndexedFile)
            .ThenInclude(file => file.Repository)
            .ThenInclude(repo => repo.Application)
            .Where(symbol => accessibleApplicationIds.Contains(symbol.IndexedFile.Repository.ApplicationId))
            .Where(symbol =>
                symbol.SymbolName.ToLower().Contains(term)
                || (symbol.Summary != null && symbol.Summary.ToLower().Contains(term))
                || symbol.IndexedFile.FilePath.ToLower().Contains(term))
            .OrderBy(symbol => symbol.SymbolName)
            .Take(8)
            .ToListAsync(cancellationToken);

        return symbols.Select(symbol => new GlobalSearchItemDto(
            "symbol",
            symbol.SymbolName,
            $"{symbol.SymbolKind} · {symbol.IndexedFile.FilePath} · {symbol.IndexedFile.Repository.Application.Name}",
            $"/Spa/SystemMap?ApplicationId={symbol.IndexedFile.Repository.ApplicationId}",
            0.92f));
    }

    private async Task<IEnumerable<GlobalSearchItemDto>> SearchKnowledgeAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var knowledge = await _mediator.Send(new SearchKnowledgeQuery(query, TopK: 5), cancellationToken);
        return knowledge.Select(item => new GlobalSearchItemDto(
            "artifact",
            item.Title,
            item.Snippet,
            "/Spa/Repositories",
            item.Score));
    }

    private async Task<IEnumerable<GlobalSearchItemDto>> SearchVectorArtifactsAsync(
        string term,
        CancellationToken cancellationToken)
    {
        var embedding = TextEmbeddingUtility.CreateEmbeddingFromText(term);
        var vectorHits = await _vectorSearch.SearchAsync(embedding, topK: 5, cancellationToken);
        if (vectorHits.Count == 0)
        {
            return [];
        }

        var accessibleApplicationIds = await _applicationAccess
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Select(app => app.Id)
            .ToListAsync(cancellationToken);

        var fileIds = vectorHits.Select(hit => hit.SourceId).ToList();
        var files = await _dbContext.IndexedFiles
            .AsNoTracking()
            .Include(file => file.Repository)
            .ThenInclude(repo => repo.Application)
            .Where(file => fileIds.Contains(file.Id))
            .Where(file => accessibleApplicationIds.Contains(file.Repository.ApplicationId))
            .ToListAsync(cancellationToken);

        var scoreById = vectorHits.ToDictionary(hit => hit.SourceId, hit => hit.Score);
        return files
            .Where(file => scoreById.ContainsKey(file.Id))
            .Select(file => new GlobalSearchItemDto(
                "artifact",
                file.ClassName ?? file.FilePath,
                $"{file.Repository.Name} · {file.Repository.Application.Name}",
                $"/Spa/SystemMap?ApplicationId={file.Repository.ApplicationId}",
                Math.Min(1.0f, scoreById[file.Id] + 0.15f)));
    }
}
