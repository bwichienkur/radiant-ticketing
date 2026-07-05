using System.Security.Cryptography;
using System.Text;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services;

public sealed class RepositoryIndexerService : IRepositoryIndexer
{
    private static readonly string[] SourceExtensions = [".cs", ".ts", ".tsx", ".js", ".json", ".md"];

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IGitRepositoryScanner _scanner;
    private readonly IGitRepositoryHistoryService _gitHistory;
    private readonly ApplicationProfileGenerator _profileGenerator;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IAuditService _auditService;
    private readonly INotificationPublisher _notifications;
    private readonly IConfiguration _configuration;
    private readonly IndexingOptions _indexingOptions;
    private readonly ILogger<RepositoryIndexerService> _logger;

    public RepositoryIndexerService(
        IEnhancementHubDbContext dbContext,
        IGitRepositoryScanner scanner,
        IGitRepositoryHistoryService gitHistory,
        ApplicationProfileGenerator profileGenerator,
        IVectorSearchService vectorSearch,
        IAuditService auditService,
        INotificationPublisher notifications,
        IConfiguration configuration,
        IOptions<IndexingOptions> indexingOptions,
        ILogger<RepositoryIndexerService> logger)
    {
        _dbContext = dbContext;
        _scanner = scanner;
        _gitHistory = gitHistory;
        _profileGenerator = profileGenerator;
        _vectorSearch = vectorSearch;
        _auditService = auditService;
        _notifications = notifications;
        _configuration = configuration;
        _indexingOptions = indexingOptions.Value;
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
            var incrementalPlan = await ResolveIncrementalPlanAsync(rootPath, branch, cancellationToken);
            var isIncremental = incrementalPlan.IsIncremental;

            RepositoryScanResult? scan = null;
            if (!isIncremental || incrementalPlan.HasCSharpChanges)
            {
                scan = await _scanner.ScanAsync(rootPath, cancellationToken);
                await PersistEntityMappingsAsync(repositoryId, scan.EntityMappings, cancellationToken);
            }

            var existingFiles = await _dbContext.IndexedFiles
                .Where(f => f.RepositoryId == repositoryId && f.BranchId == branch.Id)
                .ToListAsync(cancellationToken);

            var existingByPath = existingFiles.ToDictionary(f => f.FilePath, StringComparer.OrdinalIgnoreCase);
            var processedCount = 0;
            var skippedUnchanged = 0;

            if (isIncremental)
            {
                foreach (var deletedPath in incrementalPlan.DeletedPaths)
                {
                    if (existingByPath.TryGetValue(deletedPath, out var stale))
                    {
                        await RemoveIndexedFileAsync(stale, cancellationToken);
                        existingByPath.Remove(deletedPath);
                    }
                }

                foreach (var relativePath in incrementalPlan.ChangedPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!ShouldIndexPath(relativePath))
                    {
                        continue;
                    }

                    var absolutePath = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(absolutePath))
                    {
                        continue;
                    }

                    var updated = await ProcessFileAsync(
                        repositoryId,
                        branch.Id,
                        rootPath,
                        absolutePath,
                        relativePath,
                        scan,
                        existingByPath,
                        cancellationToken);

                    if (updated)
                    {
                        processedCount++;
                    }
                    else
                    {
                        skippedUnchanged++;
                    }
                }
            }
            else
            {
                var files = Directory
                    .EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => SourceExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .Where(f => !IsExcludedPath(f))
                    .Take(_indexingOptions.MaxFilesPerRun)
                    .ToList();

                var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var absolutePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relativePath = Path.GetRelativePath(rootPath, absolutePath).Replace('\\', '/');
                    seenPaths.Add(relativePath);

                    var updated = await ProcessFileAsync(
                        repositoryId,
                        branch.Id,
                        rootPath,
                        absolutePath,
                        relativePath,
                        scan,
                        existingByPath,
                        cancellationToken);

                    if (updated)
                    {
                        processedCount++;
                    }
                    else
                    {
                        skippedUnchanged++;
                    }
                }

                foreach (var stale in existingFiles.Where(f => !seenPaths.Contains(f.FilePath)))
                {
                    await RemoveIndexedFileAsync(stale, cancellationToken);
                }
            }

            branch.LastIndexedAt = DateTime.UtcNow;
            branch.LastCommitHash = incrementalPlan.HeadCommitHash ?? branch.LastCommitHash;
            branch.UpdatedAt = DateTime.UtcNow;
            await _profileGenerator.GenerateAsync(repositoryId, cancellationToken);

            repository.IndexingStatus = IndexingStatus.Completed;
            repository.LastIndexedAt = DateTime.UtcNow;
            repository.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var modeLabel = isIncremental ? "incremental" : "full";
            var auditMessage = isIncremental
                ? $"Incremental index ({modeLabel}): processed {processedCount} changed files, skipped {skippedUnchanged} unchanged, removed {incrementalPlan.DeletedPaths.Count} deleted files on branch {branch.BranchName} @ {branch.LastCommitHash}."
                : $"Full index: processed {processedCount} files, skipped {skippedUnchanged} unchanged on branch {branch.BranchName} @ {branch.LastCommitHash}.";

            await _auditService.LogAsync(
                isIncremental ? "RepositoryIndexedIncremental" : "RepositoryIndexed",
                nameof(Repository),
                repositoryId,
                auditMessage,
                cancellationToken);

            _logger.LogInformation(
                "Repository {RepositoryId} indexed ({Mode}): {ProcessedCount} processed, {SkippedCount} skipped unchanged",
                repositoryId,
                modeLabel,
                processedCount,
                skippedUnchanged);

            await _notifications.PublishAsync(
                "repository.index.completed",
                "Repository indexed",
                $"Indexed repository {repository.Name} ({modeLabel}, {processedCount} files updated).",
                new { repositoryId, mode = modeLabel, processedCount, skippedUnchanged },
                cancellationToken);
        }
        catch (Exception ex)
        {
            repository.IndexingStatus = IndexingStatus.Failed;
            repository.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Failed to index repository {RepositoryId}", repositoryId);

            await _notifications.PublishAsync(
                "repository.index.failed",
                "Repository indexing failed",
                $"Indexing failed for {repository.Name}: {ex.Message}",
                new { repositoryId },
                cancellationToken);
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

    private async Task<IncrementalIndexingPlan> ResolveIncrementalPlanAsync(
        string rootPath,
        RepositoryBranch branch,
        CancellationToken cancellationToken)
    {
        var headCommit = _gitHistory.GetHeadCommitHash(rootPath);
        if (!_indexingOptions.IncrementalEnabled
            || string.IsNullOrWhiteSpace(branch.LastCommitHash)
            || string.IsNullOrWhiteSpace(headCommit))
        {
            return IncrementalIndexingPlan.Full(headCommit);
        }

        var changes = await _gitHistory.GetChangesSinceAsync(rootPath, branch.LastCommitHash, cancellationToken);
        if (changes.RequiresFullReindex)
        {
            _logger.LogInformation(
                "Falling back to full index for repository branch {BranchId}",
                branch.Id);
            return IncrementalIndexingPlan.Full(changes.HeadCommitHash ?? headCommit);
        }

        if (changes.ChangedPaths.Count == 0 && changes.DeletedPaths.Count == 0)
        {
            return new IncrementalIndexingPlan(
                IsIncremental: true,
                ChangedPaths: [],
                DeletedPaths: [],
                HasCSharpChanges: false,
                HeadCommitHash: changes.HeadCommitHash ?? headCommit);
        }

        var changedPaths = changes.ChangedPaths
            .Where(ShouldIndexPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new IncrementalIndexingPlan(
            IsIncremental: true,
            ChangedPaths: changedPaths,
            DeletedPaths: changes.DeletedPaths,
            HasCSharpChanges: changedPaths.Any(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)),
            HeadCommitHash: changes.HeadCommitHash ?? headCommit);
    }

    private async Task<bool> ProcessFileAsync(
        Guid repositoryId,
        Guid branchId,
        string rootPath,
        string absolutePath,
        string relativePath,
        RepositoryScanResult? scan,
        Dictionary<string, IndexedFile> existingByPath,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(absolutePath, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var scannedClass = scan?.Classes.FirstOrDefault(c => c.FilePath == relativePath);

        if (existingByPath.TryGetValue(relativePath, out var existing) && existing.CommitHash == hash)
        {
            return false;
        }

        var componentType = MapComponentType(scannedClass, relativePath);
        var summary = scannedClass is null
            ? null
            : $"{scannedClass.Namespace}.{scannedClass.Name} ({scannedClass.Methods.Count} methods)";

        if (existing is not null)
        {
            await RemoveSymbolsAsync(existing.Id, cancellationToken);
            existing.Language = DetectLanguage(absolutePath);
            existing.FileType = Path.GetExtension(absolutePath).TrimStart('.');
            existing.Namespace = scannedClass?.Namespace;
            existing.ClassName = scannedClass?.Name;
            existing.Summary = summary;
            existing.ComponentType = componentType;
            existing.CommitHash = hash;
            existing.LastIndexedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await IndexSymbolsAsync(existing, scannedClass, cancellationToken);
            await _vectorSearch.IndexAsync("IndexedFile", existing.Id, HashToEmbedding(hash), cancellationToken);
        }
        else
        {
            var indexed = new IndexedFile
            {
                Id = Guid.NewGuid(),
                RepositoryId = repositoryId,
                BranchId = branchId,
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
            existingByPath[relativePath] = indexed;
            await IndexSymbolsAsync(indexed, scannedClass, cancellationToken);
            await _vectorSearch.IndexAsync("IndexedFile", indexed.Id, HashToEmbedding(hash), cancellationToken);
        }

        return true;
    }

    private async Task RemoveIndexedFileAsync(IndexedFile file, CancellationToken cancellationToken)
    {
        await RemoveSymbolsAsync(file.Id, cancellationToken);
        await _vectorSearch.RemoveBySourceAsync("IndexedFile", file.Id, cancellationToken);
        _dbContext.IndexedFiles.Remove(file);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveSymbolsAsync(Guid indexedFileId, CancellationToken cancellationToken)
    {
        var symbols = await _dbContext.IndexedSymbols
            .Where(s => s.IndexedFileId == indexedFileId)
            .ToListAsync(cancellationToken);

        if (symbols.Count == 0)
        {
            return;
        }

        _dbContext.IndexedSymbols.RemoveRange(symbols);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool ShouldIndexPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var extension = Path.GetExtension(relativePath);
        if (!SourceExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalized = relativePath.Replace('\\', '/');
        return !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase);
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

    private async Task PersistEntityMappingsAsync(
        Guid repositoryId,
        IReadOnlyList<EntityMappingInfo> mappings,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.CodeEntityMappings
            .Where(m => m.RepositoryId == repositoryId)
            .ToListAsync(cancellationToken);

        _dbContext.CodeEntityMappings.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var mapping in mappings)
        {
            var mappingId = Guid.NewGuid();
            var entityMapping = new CodeEntityMapping
            {
                Id = mappingId,
                RepositoryId = repositoryId,
                EntityClassName = mapping.EntityClassName,
                EntityNamespace = mapping.EntityNamespace,
                EntityFilePath = mapping.EntityFilePath,
                TableName = mapping.TableName,
                SchemaName = mapping.SchemaName,
                DbContextType = mapping.DbContextType,
                MappingSource = mapping.MappingSource,
                ConfidenceScore = mapping.ConfidenceScore,
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var property in mapping.Properties)
            {
                entityMapping.Properties.Add(new CodeEntityProperty
                {
                    Id = Guid.NewGuid(),
                    CodeEntityMappingId = mappingId,
                    PropertyName = property.PropertyName,
                    ColumnName = property.ColumnName,
                    ClrType = property.ClrType,
                    IsNullable = property.IsNullable,
                    IsPrimaryKey = property.IsPrimaryKey,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            _dbContext.CodeEntityMappings.Add(entityMapping);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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

    private sealed record IncrementalIndexingPlan(
        bool IsIncremental,
        IReadOnlyList<string> ChangedPaths,
        IReadOnlyList<string> DeletedPaths,
        bool HasCSharpChanges,
        string? HeadCommitHash)
    {
        public static IncrementalIndexingPlan Full(string? headCommitHash) =>
            new(false, [], [], true, headCommitHash);
    }
}
