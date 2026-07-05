namespace EnhancementHub.Application.Abstractions.Models;

public sealed record GitRepositoryChanges(
    bool RequiresFullReindex,
    IReadOnlyList<string> ChangedPaths,
    IReadOnlyList<string> DeletedPaths,
    string? HeadCommitHash);
