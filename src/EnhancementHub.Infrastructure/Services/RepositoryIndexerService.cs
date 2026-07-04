using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class RepositoryIndexerService : IRepositoryIndexer
{
    private static readonly string[] SourceExtensions = [".cs", ".ts", ".tsx", ".js", ".json", ".md"];

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IGitRepositoryScanner _scanner;
    private readonly ApplicationProfileGenerator _profileGenerator;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RepositoryIndexerService> _logger;

    public RepositoryIndexerService(
        IEnhancementHubDbContext dbContext,
        IGitRepositoryScanner scanner,
        ApplicationProfileGenerator profileGenerator,
        IVectorSearchService vectorSearch,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<RepositoryIndexerService> logger)
    {
        _dbContext = dbContext;
        _scanner = scanner;
        _profileGenerator = profileGenerator;
        _vectorSearch = vectorSearch;
        _auditService = auditService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task IndexRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        var repository = await _dbContext.Repositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Repository {repositoryId} not found.");

        repository.IndexingStatus = IndexingStatus.InProgress;
        repository.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var rootPath = ResolveRepositoryPath(repository);
            var branch = await GetOrCreateBranchAsync(repository, cancellationToken);
            var scan = await _scanner.ScanAsync(rootPath, cancellationToken);

            var files = Directory
                .EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f => SourceExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .Where(f => !IsExcludedPath(f))
                .Take(5000)
                .ToList();

            var existingFiles = await _dbContext.IndexedFiles
                .Where(f => f.RepositoryId == repositoryId && f.BranchId == branch.Id)
                .ToListAsync(cancellationToken);

            var existingByPath = existingFiles.ToDictionary(f => f.FilePath, StringComparer.OrdinalIgnoreCase);
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var absolutePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(rootPath, absolutePath).Replace('\\', '/');
                seenPaths.Add(relativePath);

                var content = await File.ReadAllTextAsync(absolutePath, cancellationToken);
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
                var scannedClass = scan.Classes.FirstOrDefault(c => c.FilePath == relativePath);

                if (existingByPath.TryGetValue(relativePath, out var existing) && existing.CommitHash == hash)
                {
                    continue;
                }

                var componentType = MapComponentType(scannedClass, relativePath);
                var summary = scannedClass is null
                    ? null
                    : $"{scannedClass.Namespace}.{scannedClass.Name} ({scannedClass.Methods.Count} methods)";

                if (existing is not null)
                {
                    existing.Language = DetectLanguage(absolutePath);
                    existing.FileType = Path.GetExtension(absolutePath).TrimStart('.');
                    existing.Namespace = scannedClass?.Namespace;
                    existing.ClassName = scannedClass?.Name;
                    existing.Summary = summary;
                    existing.ComponentType = componentType;
                    existing.CommitHash = hash;
                    existing.LastIndexedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _vectorSearch.IndexAsync("IndexedFile", existing.Id, HashToEmbedding(hash), cancellationToken);
                }
                else
                {
                    var indexed = new IndexedFile
                    {
                        Id = Guid.NewGuid(),
                        RepositoryId = repositoryId,
                        BranchId = branch.Id,
                        FilePath = relativePath,
                        Language = DetectLanguage(absolutePath),
                        FileType = Path.GetExtension(absolutePath).TrimStart('.'),
                        Namespace = scannedClass?.Namespace,
                        ClassName = scannedClass?.Name,
                        Summary = summary,
                        ComponentType = componentType,
                        CommitHash = hash,
                        LastIndexedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.IndexedFiles.Add(indexed);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await IndexSymbolsAsync(indexed, scannedClass, cancellationToken);
                    await _vectorSearch.IndexAsync("IndexedFile", indexed.Id, HashToEmbedding(hash), cancellationToken);
                }
            }

            foreach (var stale in existingFiles.Where(f => !seenPaths.Contains(f.FilePath)))
            {
                await _vectorSearch.RemoveBySourceAsync("IndexedFile", stale.Id, cancellationToken);
                _dbContext.IndexedFiles.Remove(stale);
            }

            branch.LastIndexedAt = DateTime.UtcNow;
            branch.UpdatedAt = DateTime.UtcNow;
            await _profileGenerator.GenerateAsync(repositoryId, cancellationToken);

            repository.IndexingStatus = IndexingStatus.Completed;
            repository.LastIndexedAt = DateTime.UtcNow;
            repository.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                "RepositoryIndexed",
                nameof(Repository),
                repositoryId,
                $"Indexed {seenPaths.Count} files from branch {branch.BranchName}.",
                cancellationToken);

            _logger.LogInformation("Repository {RepositoryId} indexed successfully ({FileCount} files)", repositoryId, seenPaths.Count);
        }
        catch (Exception ex)
        {
            repository.IndexingStatus = IndexingStatus.Failed;
            repository.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Failed to index repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task ReindexStaleRepositoriesAsync(TimeSpan staleThreshold, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - staleThreshold;
        var staleIds = await _dbContext.Repositories
            .Where(r => r.LastIndexedAt == null || r.LastIndexedAt < cutoff)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in staleIds)
        {
            await IndexRepositoryAsync(id, cancellationToken);
        }
    }

    private string ResolveRepositoryPath(Repository repository)
    {
        if (Directory.Exists(repository.Url))
        {
            return repository.Url;
        }

        var localRoot = _configuration["Repositories:LocalRoot"];
        if (!string.IsNullOrWhiteSpace(localRoot))
        {
            var candidate = Path.Combine(localRoot, repository.Id.ToString());
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException($"Repository path for '{repository.Name}' is not available.");
    }

    private async Task<RepositoryBranch> GetOrCreateBranchAsync(Repository repository, CancellationToken cancellationToken)
    {
        var branch = await _dbContext.RepositoryBranches
            .FirstOrDefaultAsync(b => b.RepositoryId == repository.Id && b.BranchName == repository.DefaultBranch, cancellationToken);

        if (branch is not null)
        {
            return branch;
        }

        branch = new RepositoryBranch
        {
            Id = Guid.NewGuid(),
            RepositoryId = repository.Id,
            BranchName = repository.DefaultBranch,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.RepositoryBranches.Add(branch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return branch;
    }

    private async Task IndexSymbolsAsync(IndexedFile file, ScannedClass? scannedClass, CancellationToken cancellationToken)
    {
        if (scannedClass is null)
        {
            return;
        }

        foreach (var method in scannedClass.Methods)
        {
            _dbContext.IndexedSymbols.Add(new IndexedSymbol
            {
                Id = Guid.NewGuid(),
                IndexedFileId = file.Id,
                SymbolName = method,
                SymbolKind = "Method",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsExcludedPath(string filePath) =>
        filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        || filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        || filePath.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static ComponentType MapComponentType(ScannedClass? scannedClass, string relativePath)
    {
        if (scannedClass is not null && scannedClass.Name.EndsWith("Controller", StringComparison.Ordinal))
        {
            return ComponentType.Controller;
        }

        if (scannedClass is not null && scannedClass.Name.EndsWith("DbContext", StringComparison.Ordinal))
        {
            return ComponentType.DbContext;
        }

        return relativePath.Contains("Test", StringComparison.OrdinalIgnoreCase)
            ? ComponentType.Test
            : ComponentType.Other;
    }

    private static string DetectLanguage(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".cs" => "csharp",
        ".ts" or ".tsx" => "typescript",
        ".js" => "javascript",
        ".json" => "json",
        ".md" => "markdown",
        _ => "text"
    };

    internal static float[] HashToEmbedding(string hashHex, int dimensions = 64)
    {
        var bytes = Convert.FromHexString(hashHex);
        var embedding = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            embedding[i] = bytes[i % bytes.Length] / 255f;
        }

        return embedding;
    }
}
