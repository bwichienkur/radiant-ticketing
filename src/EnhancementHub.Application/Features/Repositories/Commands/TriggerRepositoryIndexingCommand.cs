using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Repositories.Commands;

public sealed record TriggerRepositoryIndexingCommand(Guid RepositoryId) : IRequest<Unit>;

public sealed class TriggerRepositoryIndexingCommandHandler
    : IRequestHandler<TriggerRepositoryIndexingCommand, Unit>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IRepositoryIndexer _repositoryIndexer;

    public TriggerRepositoryIndexingCommandHandler(
        IEnhancementHubDbContext dbContext,
        IRepositoryIndexer repositoryIndexer)
    {
        _dbContext = dbContext;
        _repositoryIndexer = repositoryIndexer;
    }

    public async Task<Unit> Handle(
        TriggerRepositoryIndexingCommand request,
        CancellationToken cancellationToken)
    {
        var repository = await _dbContext.Repositories
            .FirstOrDefaultAsync(r => r.Id == request.RepositoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Repository), request.RepositoryId);

        repository.IndexingStatus = IndexingStatus.InProgress;
        repository.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _repositoryIndexer.IndexRepositoryAsync(request.RepositoryId, cancellationToken);

        return Unit.Value;
    }
}
