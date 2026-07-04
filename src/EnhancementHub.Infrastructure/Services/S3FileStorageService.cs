using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

/// <summary>
/// S3-compatible storage stub. Configure Storage:Provider=S3 and AWS credentials to enable.
/// </summary>
public sealed class S3FileStorageService : IFileStorageService
{
    private readonly ILogger<S3FileStorageService> _logger;
    private readonly string? _bucket;

    public S3FileStorageService(IConfiguration configuration, ILogger<S3FileStorageService> logger)
    {
        _logger = logger;
        _bucket = configuration["Storage:S3:Bucket"];
    }

    public Task<string> SaveAsync(string container, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("S3 storage is not fully configured. Bucket={Bucket}", _bucket ?? "(unset)");
        throw new InvalidOperationException("S3 storage is not configured. Set Storage:Provider=Local or configure Storage:S3.");
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("S3 storage is not configured.");

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("S3 storage is not configured.");
}
