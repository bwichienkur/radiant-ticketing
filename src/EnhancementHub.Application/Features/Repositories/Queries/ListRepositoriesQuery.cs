using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Repositories.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Repositories.Queries;

public sealed record ListRepositoriesQuery(Guid? ApplicationId = null)
    : IRequest<IReadOnlyList<RepositoryDto>>;

public sealed class ListRepositoriesQueryHandler
    : IRequestHandler<ListRepositoriesQuery, IReadOnlyList<RepositoryDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public ListRepositoriesQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<RepositoryDto>> Handle(
        ListRepositoriesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ApplicationId.HasValue)
        {
            await _accessService.EnsureAccessibleApplicationAsync(
                request.ApplicationId.Value,
                cancellationToken);
        }

        var accessibleApplicationIds = _accessService
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Select(a => a.Id);

        var query = _dbContext.Repositories
            .AsNoTracking()
            .Include(r => r.Application)
            .Where(r => accessibleApplicationIds.Contains(r.ApplicationId))
            .AsQueryable();

        if (request.ApplicationId.HasValue)
        {
            query = query.Where(r => r.ApplicationId == request.ApplicationId.Value);
        }

        var entities = await query
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDto()).ToList();
    }
}
