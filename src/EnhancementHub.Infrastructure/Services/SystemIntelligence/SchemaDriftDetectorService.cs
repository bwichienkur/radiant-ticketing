using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class SchemaDriftDetectorService : ISchemaDriftDetector
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly INotificationPublisher _notifications;
    private readonly INotificationService _notificationService;
    private readonly ISystemIntelligenceFingerprintService _fingerprintService;
    private readonly SystemIntelligenceOptions _options;
    private readonly ILogger<SchemaDriftDetectorService> _logger;

    public SchemaDriftDetectorService(
        IEnhancementHubDbContext dbContext,
        INotificationPublisher notifications,
        INotificationService notificationService,
        ISystemIntelligenceFingerprintService fingerprintService,
        IOptions<SystemIntelligenceOptions> options,
        ILogger<SchemaDriftDetectorService> logger)
    {
        _dbContext = dbContext;
        _notifications = notifications;
        _notificationService = notificationService;
        _fingerprintService = fingerprintService;
        _options = options.Value;
        _logger = logger;
    }

    public Task<DriftReport> DetectDriftAsync(Guid databaseConnectionId, CancellationToken cancellationToken = default) =>
        DetectDriftInternalAsync(databaseConnectionId, cancellationToken);

    public async Task<DriftReport> DetectDriftIfStaleAsync(
        Guid databaseConnectionId,
        bool forceFullScan = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceFullScan && _options.DiffOnlyDriftEnabled)
        {
            var isStale = await _fingerprintService.IsDriftScanStaleAsync(databaseConnectionId, cancellationToken);
            if (!isStale)
            {
                _logger.LogInformation(
                    "Skipping drift scan for connection {ConnectionId}; source data unchanged since last scan",
                    databaseConnectionId);
                return await LoadExistingReportAsync(databaseConnectionId, cancellationToken);
            }
        }

        return await DetectDriftInternalAsync(databaseConnectionId, cancellationToken);
    }

    private async Task<DriftReport> DetectDriftInternalAsync(
        Guid databaseConnectionId,
        CancellationToken cancellationToken)
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
            .Include(m => m.Properties)
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

        connection.LastDriftScanAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Drift detection for connection {ConnectionId} found {Count} issues",
            databaseConnectionId, findings.Count);

        await _notifications.PublishAsync(
            "schema.drift.detected",
            "Schema drift scan complete",
            $"Detected {findings.Count} drift finding(s).",
            new { databaseConnectionId, findingCount = findings.Count },
            cancellationToken);

        var criticalCount = findings.Count(f => f.Severity == DriftSeverity.Critical);
        if (criticalCount > 0)
        {
            await _notificationService.NotifyAdminsOfCriticalDriftAsync(
                databaseConnectionId,
                connection.Name,
                criticalCount,
                null,
                cancellationToken);
        }

        return new DriftReport
        {
            DatabaseConnectionId = databaseConnectionId,
            GeneratedAt = DateTime.UtcNow,
            Findings = findings
        };
    }

    private async Task<DriftReport> LoadExistingReportAsync(
        Guid databaseConnectionId,
        CancellationToken cancellationToken)
    {
        var findings = await _dbContext.SchemaDriftFindings
            .AsNoTracking()
            .Where(f => f.DatabaseConnectionId == databaseConnectionId && !f.IsResolved)
            .OrderByDescending(f => f.DetectedAt)
            .Select(f => new DriftFindingDto
            {
                DriftType = f.DriftType,
                Severity = f.Severity,
                Title = f.Title,
                Description = f.Description,
                CodeReference = f.CodeReference,
                DatabaseReference = f.DatabaseReference,
                RepositoryId = f.RepositoryId
            })
            .ToListAsync(cancellationToken);

        var lastScan = await _dbContext.DatabaseConnections
            .AsNoTracking()
            .Where(c => c.Id == databaseConnectionId)
            .Select(c => c.LastDriftScanAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new DriftReport
        {
            DatabaseConnectionId = databaseConnectionId,
            GeneratedAt = lastScan ?? DateTime.UtcNow,
            Findings = findings
        };
    }

    private static void CompareColumns(CodeEntityMapping mapping, DatabaseTable dbTable, List<DriftFindingDto> findings)
    {
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
            return;
        }

        if (mapping.Properties.Count == 0)
        {
            return;
        }

        var dbColumns = dbTable.Columns.ToDictionary(
            c => c.Name,
            c => c,
            StringComparer.OrdinalIgnoreCase);

        var matchedDbColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in mapping.Properties)
        {
            var columnName = property.ColumnName ?? property.PropertyName;
            if (!dbColumns.TryGetValue(columnName, out var dbColumn))
            {
                findings.Add(new DriftFindingDto
                {
                    DriftType = DriftType.MissingInDatabase,
                    Severity = DriftSeverity.High,
                    Title = $"Column missing in database: {mapping.TableName}.{columnName}",
                    Description = $"Property {mapping.EntityClassName}.{property.PropertyName} maps to column '{columnName}' but it was not found in the live database.",
                    CodeReference = $"{mapping.EntityFilePath} ({property.PropertyName}: {property.ClrType})",
                    DatabaseReference = $"{mapping.SchemaName}.{mapping.TableName}",
                    RepositoryId = mapping.RepositoryId
                });
                continue;
            }

            matchedDbColumns.Add(dbColumn.Name);

            if (property.IsNullable != dbColumn.IsNullable)
            {
                findings.Add(new DriftFindingDto
                {
                    DriftType = DriftType.NullableMismatch,
                    Severity = DriftSeverity.Medium,
                    Title = $"Nullable mismatch: {mapping.TableName}.{columnName}",
                    Description = $"Code expects nullable={property.IsNullable} but database column is nullable={dbColumn.IsNullable}.",
                    CodeReference = $"{mapping.EntityFilePath} ({property.PropertyName})",
                    DatabaseReference = $"{dbColumn.DataType} nullable={dbColumn.IsNullable}",
                    RepositoryId = mapping.RepositoryId
                });
            }

            if (!TypesCompatible(property.ClrType, dbColumn.DataType))
            {
                findings.Add(new DriftFindingDto
                {
                    DriftType = DriftType.ColumnTypeMismatch,
                    Severity = DriftSeverity.High,
                    Title = $"Type mismatch: {mapping.TableName}.{columnName}",
                    Description = $"Code type '{property.ClrType}' does not match database type '{dbColumn.DataType}'.",
                    CodeReference = $"{mapping.EntityFilePath} ({property.PropertyName}: {property.ClrType})",
                    DatabaseReference = dbColumn.DataType,
                    RepositoryId = mapping.RepositoryId
                });
            }
        }

        foreach (var dbColumn in dbTable.Columns)
        {
            if (!matchedDbColumns.Contains(dbColumn.Name))
            {
                findings.Add(new DriftFindingDto
                {
                    DriftType = DriftType.MissingInCode,
                    Severity = DriftSeverity.Medium,
                    Title = $"Column missing in code: {mapping.TableName}.{dbColumn.Name}",
                    Description = $"Database column '{dbColumn.Name}' exists but has no mapped EF property on {mapping.EntityClassName}.",
                    CodeReference = mapping.EntityFilePath,
                    DatabaseReference = $"{dbColumn.DataType} nullable={dbColumn.IsNullable}",
                    RepositoryId = mapping.RepositoryId
                });
            }
        }
    }

    public static bool TypesCompatible(string clrType, string dbType)
    {
        var normalizedClr = NormalizeTypeName(clrType);
        var normalizedDb = NormalizeTypeName(dbType);

        if (normalizedClr == normalizedDb)
        {
            return true;
        }

        return (normalizedClr, normalizedDb) switch
        {
            ("int", "integer") => true,
            ("int", "int") => true,
            ("int32", "integer") => true,
            ("long", "bigint") => true,
            ("int64", "bigint") => true,
            ("string", "text") => true,
            ("string", "varchar") => true,
            ("string", "nvarchar") => true,
            ("string", "character varying") => true,
            ("bool", "boolean") => true,
            ("bool", "bit") => true,
            ("boolean", "bit") => true,
            ("datetime", "timestamp") => true,
            ("datetime", "datetime") => true,
            ("datetimeoffset", "timestamp with time zone") => true,
            ("guid", "uuid") => true,
            ("guid", "uniqueidentifier") => true,
            ("decimal", "numeric") => true,
            ("decimal", "decimal") => true,
            ("double", "double precision") => true,
            ("float", "real") => true,
            ("float", "float") => true,
            ("byte[]", "blob") => true,
            ("byte[]", "bytea") => true,
            _ when normalizedDb.Contains(normalizedClr) || normalizedClr.Contains(normalizedDb) => true,
            _ => false
        };
    }

    private static string NormalizeTypeName(string type) =>
        type.Replace("System.", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToLowerInvariant()
            .Split('(')[0];

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
