using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Abstractions;

public interface IGitRepositoryScanner
{
    Task<RepositoryScanResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default);
}
