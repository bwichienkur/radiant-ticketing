using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EnhancementHub.Infrastructure.Services;

public sealed class DataRetentionService : IDataRetentionService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly RetentionOptions _options;
    private readonly ILogger<DataRetentionService> _logger;

    public DataRetentionService(
        IEnhancementHubDbContext dbContext,
        IFileStorageService fileStorage,
        IOptions<RetentionOptions> options,
        ILogger<DataRetentionService> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DataRetentionStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var aiCutoff = GetAiPromptRunsCutoff();
        var attachmentCutoff = GetAttachmentsCutoff();

        var eligibleAiRuns = aiCutoff.HasValue
            ? await _dbContext.AiPromptRuns.CountAsync(r => r.CreatedAt < aiCutoff.Value, cancellationToken)
            : 0;

        var eligibleAttachments = attachmentCutoff.HasValue
            ? await _dbContext.EnhancementAttachments.CountAsync(a => a.CreatedAt < attachmentCutoff.Value, cancellationToken)
            : 0;

        return new DataRetentionStatusDto(
            _options.Enabled,
            _options.AiPromptRunsDays,
            _options.AttachmentsDays,
            _options.BatchSize,
            eligibleAiRuns,
            eligibleAttachments,
            aiCutoff,
            attachmentCutoff);
    }

    public async Task<DataRetentionResultDto> ApplyAsync(bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var aiCutoff = GetAiPromptRunsCutoff();
        var attachmentCutoff = GetAttachmentsCutoff();
        var batchSize = Math.Clamp(_options.BatchSize, 1, 5000);

        var aiPromptRunsDeleted = 0;
        var contextItemsDeleted = 0;
        var attachmentsDeleted = 0;
        var attachmentFilesDeleted = 0;

        if (aiCutoff.HasValue)
        {
            var runs = await _dbContext.AiPromptRuns
                .Where(r => r.CreatedAt < aiCutoff.Value)
                .OrderBy(r => r.CreatedAt)
                .Take(batchSize)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (runs.Count > 0)
            {
                var contextItems = await _dbContext.RetrievedContextItems
                    .Where(i => runs.Contains(i.AiPromptRunId))
                    .ToListAsync(cancellationToken);

                contextItemsDeleted = contextItems.Count;

                if (!dryRun)
                {
                    var entities = await _dbContext.AiPromptRuns
                        .Where(r => runs.Contains(r.Id))
                        .ToListAsync(cancellationToken);

                    if (_options.ArchiveAiPromptRunsBeforeDelete)
                    {
                        await ArchiveAiPromptRunsAsync(entities, cancellationToken);
                    }

                    _dbContext.RetrievedContextItems.RemoveRange(contextItems);
                    _dbContext.AiPromptRuns.RemoveRange(entities);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                aiPromptRunsDeleted = runs.Count;
            }
        }

        if (attachmentCutoff.HasValue)
        {
            var attachments = await _dbContext.EnhancementAttachments
                .Where(a => a.CreatedAt < attachmentCutoff.Value)
                .OrderBy(a => a.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var attachment in attachments)
            {
                if (!dryRun)
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(attachment.StoragePath, cancellationToken);
                        attachmentFilesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to delete attachment file {StoragePath} during retention",
                            attachment.StoragePath);
                    }
                }
                else
                {
                    attachmentFilesDeleted++;
                }
            }

            attachmentsDeleted = attachments.Count;

            if (!dryRun && attachments.Count > 0)
            {
                _dbContext.EnhancementAttachments.RemoveRange(attachments);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (!dryRun && (aiPromptRunsDeleted > 0 || attachmentsDeleted > 0))
        {
            _logger.LogInformation(
                "Data retention applied: {AiPromptRuns} prompt runs, {ContextItems} context items, {Attachments} attachments",
                aiPromptRunsDeleted,
                contextItemsDeleted,
                attachmentsDeleted);
        }

        return new DataRetentionResultDto(
            dryRun,
            aiPromptRunsDeleted,
            contextItemsDeleted,
            attachmentsDeleted,
            attachmentFilesDeleted,
            DateTime.UtcNow);
    }

    private DateTime? GetAiPromptRunsCutoff() =>
        _options.AiPromptRunsDays > 0
            ? DateTime.UtcNow.AddDays(-_options.AiPromptRunsDays)
            : null;

    private DateTime? GetAttachmentsCutoff() =>
        _options.AttachmentsDays > 0
            ? DateTime.UtcNow.AddDays(-_options.AttachmentsDays)
            : null;

    private async Task ArchiveAiPromptRunsAsync(
        IReadOnlyList<AiPromptRun> runs,
        CancellationToken cancellationToken)
    {
        if (runs.Count == 0)
        {
            return;
        }

        var archiveRoot = _options.ArchivePath
            ?? Path.Combine(AppContext.BaseDirectory, "archives", "ai-prompt-runs");
        Directory.CreateDirectory(archiveRoot);

        var fileName = $"ai-prompt-runs-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var filePath = Path.Combine(archiveRoot, fileName);

        var payload = runs.Select(r => new
        {
            r.Id,
            r.EnhancementRequestId,
            r.WorkflowStep,
            r.ModelName,
            r.Status,
            r.TotalTokens,
            r.EstimatedCostUsd,
            r.StartedAt,
            r.CompletedAt,
            r.CreatedAt
        });

        await File.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);

        _logger.LogInformation("Archived {Count} AI prompt runs to {FilePath}", runs.Count, filePath);
    }
}
