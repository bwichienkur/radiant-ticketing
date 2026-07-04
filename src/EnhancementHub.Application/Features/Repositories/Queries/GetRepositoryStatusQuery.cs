using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Repositories.Dtos;
using EnhancementHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Repositories.Queries;

public sealed record GetRepositoryStatusQuery(Guid RepositoryId) : IRequest<RepositoryStatusDto>;

public sealed class GetRepositoryStatusQueryHandler
    : IRequestHandler<GetRepositoryStatusQuery, RepositoryStatusDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetRepositoryStatusQueryHandler(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RepositoryStatusDto> Handle(
        GetRepositoryStatusQuery request,
        CancellationToken cancellationToken)
    {
        var repository = await _dbContext.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RepositoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Repository), request.RepositoryId);

        var indexedFileCount = await _dbContext.IndexedFiles
            .AsNoTracking()
            .CountAsync(f => f.RepositoryId == request.RepositoryId, cancellationToken);

        return new RepositoryStatusDto(
            repository.Id,
            repository.Name,
            repository.IndexingStatus,
            repository.LastIndexedAt,
            indexedFileCount);
    }
}
