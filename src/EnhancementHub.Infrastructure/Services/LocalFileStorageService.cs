using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:LocalRoot"]
            ?? Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        string container,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var safeContainer = Sanitize(container);
        var safeFileName = Sanitize(fileName);
        var directory = Path.Combine(_rootPath, safeContainer);
        Directory.CreateDirectory(directory);

        var storagePath = Path.Combine(safeContainer, $"{Guid.NewGuid():N}_{safeFileName}");
        var absolutePath = Path.Combine(_rootPath, storagePath);

        await using var fileStream = File.Create(absolutePath);
        await content.CopyToAsync(fileStream, cancellationToken);
        return storagePath.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(_rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Stored file not found: {storagePath}");
        }

        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(_rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private static string Sanitize(string value) =>
        string.Concat(value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
}
