using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IGitRepositoryHistoryService
{
    string? GetHeadCommitHash(string repositoryPath);

    Task<GitRepositoryChanges> GetChangesSinceAsync(
        string repositoryPath,
        string sinceCommitHash,
        CancellationToken cancellationToken = default);
}
