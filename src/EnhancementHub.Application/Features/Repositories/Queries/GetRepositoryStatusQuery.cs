using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Repositories.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Application.Options;
using MediatR;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Application.Features.Repositories.Queries;

public sealed record GetRepositoryStatusQuery(Guid RepositoryId) : IRequest<RepositoryStatusDto>;

public sealed class GetRepositoryStatusQueryHandler
    : IRequestHandler<GetRepositoryStatusQuery, RepositoryStatusDto>
{
    private readonly IGitRepositoryRepository _repositories;
    private readonly IndexingOptions _indexingOptions;

    public GetRepositoryStatusQueryHandler(
        IGitRepositoryRepository repositories,
        IOptions<IndexingOptions> indexingOptions)
    {
        _repositories = repositories;
        _indexingOptions = indexingOptions.Value;
    }

    public async Task<RepositoryStatusDto> Handle(
        GetRepositoryStatusQuery request,
        CancellationToken cancellationToken)
    {
        var repository = await _repositories.GetByIdAsync(request.RepositoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Repository), request.RepositoryId);

        var branch = await _repositories.GetDefaultBranchAsync(request.RepositoryId, cancellationToken);
        var indexedFileCount = await _repositories.CountIndexedFilesAsync(request.RepositoryId, cancellationToken);

        return new RepositoryStatusDto(
            repository.Id,
            repository.Name,
            repository.IndexingStatus,
            repository.LastIndexedAt,
            indexedFileCount,
            branch?.LastCommitHash,
            _indexingOptions.IncrementalEnabled);
    }
}
