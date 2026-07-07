using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Repositories.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Repositories.Queries;

public sealed record ListRepositoriesQuery(Guid? ApplicationId = null)
    : IRequest<IReadOnlyList<RepositoryDto>>;

public sealed class ListRepositoriesQueryHandler
    : IRequestHandler<ListRepositoriesQuery, IReadOnlyList<RepositoryDto>>
{
    private readonly IGitRepositoryRepository _repositories;
    private readonly IApplicationAccessService _accessService;

    public ListRepositoriesQueryHandler(
        IGitRepositoryRepository repositories,
        IApplicationAccessService accessService)
    {
        _repositories = repositories;
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

        var entities = await _repositories.ListAccessibleAsync(request.ApplicationId, cancellationToken);
        return entities.Select(e => e.ToDto()).ToList();
    }
}
