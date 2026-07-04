namespace EnhancementHub.Application.Abstractions;

public interface IRepositoryIndexer
{
    Task IndexRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default);
    Task ReindexStaleRepositoriesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default);
}
