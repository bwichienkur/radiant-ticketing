namespace EnhancementHub.Application.Abstractions;

public sealed record DocumentTextExtractionResult(
    bool Succeeded,
    string Text,
    string? ErrorMessage);

public interface IDocumentTextExtractor
{
    Task<DocumentTextExtractionResult> ExtractAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);
}

public sealed record PolicyUrlFetchResult(
    bool Succeeded,
    string Text,
    string? SourceTitle,
    string? ErrorMessage);

public interface IPolicyUrlFetcher
{
    Task<PolicyUrlFetchResult> FetchAsync(string url, CancellationToken cancellationToken = default);
}
