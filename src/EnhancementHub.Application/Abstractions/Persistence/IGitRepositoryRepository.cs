using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Abstractions.Persistence;

public interface IGitRepositoryRepository
{
    Task<IReadOnlyList<Repository>> ListAccessibleAsync(
        Guid? applicationId,
        CancellationToken cancellationToken = default);
    Task<Repository?> GetByIdAsync(Guid repositoryId, CancellationToken cancellationToken = default);
    Task<RepositoryBranch?> GetDefaultBranchAsync(Guid repositoryId, CancellationToken cancellationToken = default);
    Task<int> CountIndexedFilesAsync(Guid repositoryId, CancellationToken cancellationToken = default);
}
