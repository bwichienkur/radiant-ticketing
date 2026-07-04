namespace EnhancementHub.Application.Abstractions;

public sealed record RepositoryArchiveExtractResult(
    bool Succeeded,
    string? LocalPath,
    string? ErrorMessage);

public interface IRepositoryArchiveExtractService
{
    Task<RepositoryArchiveExtractResult> ExtractZipAsync(
        Stream archiveStream,
        CancellationToken cancellationToken = default);
}
