using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services;

public sealed class NoOpAttachmentScanService : IAttachmentScanService
{
    public Task<AttachmentScanResult> ScanAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AttachmentScanResult(true, "Skipped", "Attachment scanning is disabled."));
}
