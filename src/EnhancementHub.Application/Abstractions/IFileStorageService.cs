namespace EnhancementHub.Application.Abstractions;

public interface IFileStorageService
{
    Task<string> SaveAsync(string container, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
