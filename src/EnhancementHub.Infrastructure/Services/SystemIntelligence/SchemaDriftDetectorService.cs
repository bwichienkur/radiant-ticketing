using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class SchemaDriftDetectorService : ISchemaDriftDetector
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ILogger<SchemaDriftDetectorService> _logger;

    public SchemaDriftDetectorService(
        IEnhancementHubDbContext dbContext,
        ILogger<SchemaDriftDetectorService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DriftReport> DetectDriftAsync(Guid databaseConnectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.DatabaseConnections
            .Include(c => c.Tables)
            .ThenInclude(t => t.Columns)
            .FirstOrDefaultAsync(c => c.Id == databaseConnectionId, cancellationToken)
            ?? throw new InvalidOperationException($"Database connection {databaseConnectionId} not found.");

        var applicationRepoIds = await _dbContext.Repositories
            .Where(r => r.ApplicationId == connection.ApplicationId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var codeMappings = await _dbContext.CodeEntityMappings
            .Where(m => applicationRepoIds.Contains(m.RepositoryId))
            .ToListAsync(cancellationToken);

        var findings = new List<DriftFindingDto>();
        var dbTableKeys = connection.Tables
            .Select(t => $"{t.SchemaName}.{t.TableName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var codeTableKeys = codeMappings
            .Select(m => $"{m.SchemaName}.{m.TableName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in codeMappings)
        {
            var key = $"{mapping.SchemaName}.{mapping.TableName}";
            var dbTable = connection.Tables.FirstOrDefault(t =>
                t.SchemaName.Equals(mapping.SchemaName, StringComparison.OrdinalIgnoreCase)
                && t.TableName.Equals(mapping.TableName, StringComparison.OrdinalIgnoreCase));

            if (dbTable is null)
            {
                findings.Add(new DriftFindingDto
                {
                    DriftType = DriftType.MissingInDatabase,
                    Severity = DriftSeverity.High,
                    Title = $"Table missing in database: {key}",
                    Description = $"Entity {mapping.EntityClassName} maps to {key} but no matching table was found in the live database.",
                    CodeReference = $"{mapping.EntityFilePath} ({mapping.EntityClassName})",
                    DatabaseReference = null,
                    RepositoryId = mapping.RepositoryId
                });
                continue;
            }

            CompareColumns(mapping, dbTable, findings);
        }

        foreach (var table in connection.Tables)
        {
            var key = $"{table.SchemaName}.{table.TableName}";
            if (!codeTableKeys.Contains(key))
            {
                findings.Add(new DriftFindingDto
                {
                    DriftType = DriftType.OrphanTable,
                    Severity = DriftSeverity.Medium,
                    Title = $"Orphan table in database: {key}",
                    Description = $"Table {key} exists in the database but has no EF entity mapping in indexed repositories.",
                    CodeReference = null,
                    DatabaseReference = key
                });
            }
        }

        foreach (var mapping in codeMappings)
        {
            var key = $"{mapping.SchemaName}.{mapping.TableName}";
            if (!dbTableKeys.Contains(key))
            {
                continue;
            }

            if (mapping.MappingSource == EntityMappingSource.Migration)
            {
                findings.Add(new DriftFindingDto
                {
                    DriftType = DriftType.MigrationDrift,
                    Severity = DriftSeverity.Low,
                    Title = $"Migration-sourced mapping for {mapping.EntityClassName}",
                    Description = $"Entity {mapping.EntityClassName} was discovered via migration files; verify against live schema.",
                    CodeReference = mapping.EntityFilePath,
                    DatabaseReference = key,
                    RepositoryId = mapping.RepositoryId
                });
            }
        }

        await PersistFindingsAsync(connection, findings, cancellationToken);

        _logger.LogInformation("Drift detection for connection {ConnectionId} found {Count} issues",
            databaseConnectionId, findings.Count);

        return new DriftReport
        {
            DatabaseConnectionId = databaseConnectionId,
            GeneratedAt = DateTime.UtcNow,
            Findings = findings
        };
    }

    private static void CompareColumns(CodeEntityMapping mapping, DatabaseTable dbTable, List<DriftFindingDto> findings)
    {
        // Phase 1: flag tables with no columns as potential drift
        if (dbTable.Columns.Count == 0)
        {
            findings.Add(new DriftFindingDto
            {
                DriftType = DriftType.MissingInCode,
                Severity = DriftSeverity.Low,
                Title = $"No column metadata for {mapping.TableName}",
                Description = $"Database table {mapping.SchemaName}.{mapping.TableName} has no captured columns.",
                CodeReference = mapping.EntityFilePath,
                DatabaseReference = $"{mapping.SchemaName}.{mapping.TableName}",
                RepositoryId = mapping.RepositoryId
            });
        }
    }

    private async Task PersistFindingsAsync(
        DatabaseConnection connection,
        IReadOnlyList<DriftFindingDto> findings,
        CancellationToken cancellationToken)
    {
        var unresolved = await _dbContext.SchemaDriftFindings
            .Where(f => f.DatabaseConnectionId == connection.Id && !f.IsResolved)
            .ToListAsync(cancellationToken);

        _dbContext.SchemaDriftFindings.RemoveRange(unresolved);

        var now = DateTime.UtcNow;
        foreach (var finding in findings)
        {
            _dbContext.SchemaDriftFindings.Add(new SchemaDriftFinding
            {
                Id = Guid.NewGuid(),
                DatabaseConnectionId = connection.Id,
                RepositoryId = finding.RepositoryId,
                DriftType = finding.DriftType,
                Severity = finding.Severity,
                Title = finding.Title,
                Description = finding.Description,
                CodeReference = finding.CodeReference,
                DatabaseReference = finding.DatabaseReference,
                IsResolved = false,
                DetectedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
