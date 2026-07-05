using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.Integrations;

public sealed class PolyglotSymbolIngestionService : IPolyglotSymbolIngestionService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IntegrationsOptions _options;

    public PolyglotSymbolIngestionService(
        IEnhancementHubDbContext dbContext,
        IOptions<IntegrationsOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<PolyglotIngestionResult> IngestAsync(
        Guid repositoryId,
        IReadOnlyList<PolyglotSymbolInput> symbols,
        string language,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Polyglot.Enabled)
        {
            return new PolyglotIngestionResult(false, 0, "Polyglot symbol ingestion is disabled.");
        }

        if (!_options.Polyglot.SupportedLanguages.Contains(language, StringComparer.OrdinalIgnoreCase))
        {
            return new PolyglotIngestionResult(
                false,
                0,
                $"Language '{language}' is not in the supported list.");
        }

        var repository = await _dbContext.Repositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken);

        if (repository is null)
        {
            return new PolyglotIngestionResult(false, 0, "Repository not found.");
        }

        var branch = await _dbContext.RepositoryBranches
            .FirstOrDefaultAsync(b => b.RepositoryId == repositoryId && b.BranchName == repository.DefaultBranch, cancellationToken);

        if (branch is null)
        {
            var now = DateTime.UtcNow;
            branch = new RepositoryBranch
            {
                Id = Guid.NewGuid(),
                RepositoryId = repositoryId,
                BranchName = repository.DefaultBranch,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.RepositoryBranches.Add(branch);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var ingested = 0;
        var grouped = symbols.GroupBy(s => s.FilePath, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            var file = await _dbContext.IndexedFiles
                .Include(f => f.Symbols)
                .FirstOrDefaultAsync(
                    f => f.RepositoryId == repositoryId
                         && f.BranchId == branch.Id
                         && f.FilePath == group.Key,
                    cancellationToken);

            var timestamp = DateTime.UtcNow;
            if (file is null)
            {
                file = new IndexedFile
                {
                    Id = Guid.NewGuid(),
                    RepositoryId = repositoryId,
                    BranchId = branch.Id,
                    FilePath = group.Key,
                    Language = language,
                    FileType = Path.GetExtension(group.Key).TrimStart('.'),
                    CommitHash = $"polyglot:{language}:{timestamp.Ticks}",
                    LastIndexedAt = timestamp,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                };
                _dbContext.IndexedFiles.Add(file);
            }
            else
            {
                _dbContext.IndexedSymbols.RemoveRange(file.Symbols);
                file.LastIndexedAt = timestamp;
                file.UpdatedAt = timestamp;
            }

            foreach (var symbol in group)
            {
                _dbContext.IndexedSymbols.Add(new IndexedSymbol
                {
                    Id = Guid.NewGuid(),
                    IndexedFileId = file.Id,
                    SymbolName = symbol.SymbolName,
                    SymbolKind = symbol.SymbolKind,
                    Summary = symbol.Summary ?? $"[{language}] {symbol.SymbolKind}",
                    LineStart = symbol.LineStart,
                    LineEnd = symbol.LineEnd,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                });
                ingested++;
            }
        }

        repository.LastIndexedAt = DateTime.UtcNow;
        repository.IndexingStatus = IndexingStatus.Completed;
        repository.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new PolyglotIngestionResult(true, ingested, null);
    }
}
