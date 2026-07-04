namespace EnhancementHub.Application.Abstractions;

public sealed record GitCloneResult(
    bool Succeeded,
    string? LocalPath,
    string? ErrorMessage);

public interface IGitRepositoryCloneService
{
    Task<GitCloneResult> CloneAsync(
        string repositoryUrl,
        string? branch = null,
        string? accessToken = null,
        CancellationToken cancellationToken = default);
}
