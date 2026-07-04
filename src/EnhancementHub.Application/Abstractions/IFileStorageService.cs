namespace EnhancementHub.Application.Abstractions;

public interface IFileStorageService
{
    Task<string> SaveAsync(string container, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a time-limited download URL when the provider supports it (e.g. S3 presigned URLs).
    /// Returns null when the caller should stream the file via <see cref="OpenReadAsync"/>.
    /// </summary>
    Task<string?> GetPresignedDownloadUrlAsync(string storagePath, TimeSpan validity, CancellationToken cancellationToken = default);
}
