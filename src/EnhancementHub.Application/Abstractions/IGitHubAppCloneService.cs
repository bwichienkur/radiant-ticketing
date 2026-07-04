namespace EnhancementHub.Application.Abstractions;

public sealed record GitHubAppCloneResult(
    bool Succeeded,
    string? LocalPath,
    string? ErrorMessage);

public interface IGitHubAppCloneService
{
    bool IsConfigured { get; }

    Task<GitHubAppCloneResult> CloneRepositoryAsync(
        string owner,
        string repository,
        string branch = "main",
        long? installationId = null,
        CancellationToken cancellationToken = default);
}
