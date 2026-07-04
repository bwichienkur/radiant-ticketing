namespace EnhancementHub.Application.Abstractions;

public sealed record AttachmentScanResult(
    bool IsAllowed,
    string Status,
    string? Details);

public interface IAttachmentScanService
{
    Task<AttachmentScanResult> ScanAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);
}
