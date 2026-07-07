using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Persistence.Repositories;

public sealed class GitRepositoryRepository : IGitRepositoryRepository
{
    private readonly EnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public GitRepositoryRepository(EnhancementHubDbContext dbContext, IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<Repository>> ListAccessibleAsync(
        Guid? applicationId,
        CancellationToken cancellationToken = default)
    {
        var accessibleApplicationIds = _accessService
            .ApplyVisibilityFilter(_dbContext.Applications.AsNoTracking())
            .Select(a => a.Id);

        var query = _dbContext.Repositories
            .AsNoTracking()
            .Include(r => r.Application)
            .Where(r => accessibleApplicationIds.Contains(r.ApplicationId));

        if (applicationId.HasValue)
        {
            query = query.Where(r => r.ApplicationId == applicationId.Value);
        }

        return await query.OrderBy(r => r.Name).ToListAsync(cancellationToken);
    }

    public Task<Repository?> GetByIdAsync(Guid repositoryId, CancellationToken cancellationToken = default) =>
        _dbContext.Repositories.AsNoTracking().FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken);

    public async Task<RepositoryBranch?> GetDefaultBranchAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        var repository = await _dbContext.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken);
        if (repository is null)
        {
            return null;
        }

        return await _dbContext.RepositoryBranches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.RepositoryId == repositoryId && b.BranchName == repository.DefaultBranch,
                cancellationToken);
    }

    public Task<int> CountIndexedFilesAsync(Guid repositoryId, CancellationToken cancellationToken = default) =>
        _dbContext.IndexedFiles.AsNoTracking().CountAsync(f => f.RepositoryId == repositoryId, cancellationToken);
}
