using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class DatabaseSchemaIngestionService : IDatabaseSchemaIngestionService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IDatabaseSchemaScanner _scanner;
    private readonly IConnectionStringProtector _connectionStringProtector;
    private readonly INotificationPublisher _notifications;
    private readonly ILogger<DatabaseSchemaIngestionService> _logger;

    public DatabaseSchemaIngestionService(
        IEnhancementHubDbContext dbContext,
        IDatabaseSchemaScanner scanner,
        IConnectionStringProtector connectionStringProtector,
        INotificationPublisher notifications,
        ILogger<DatabaseSchemaIngestionService> logger)
    {
        _dbContext = dbContext;
        _scanner = scanner;
        _connectionStringProtector = connectionStringProtector;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task IngestAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.DatabaseConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken)
            ?? throw new InvalidOperationException($"Database connection {connectionId} not found.");

        try
        {
            connection.ScanStatus = "InProgress";
            connection.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var connectionString = _connectionStringProtector.Unprotect(connection.ConnectionStringProtected);
            var scanResult = await _scanner.ScanAsync(connectionString, connection.Provider, cancellationToken);
            await IngestScanResultAsync(connectionId, scanResult, cancellationToken);
        }
        catch (Exception ex)
        {
            connection.ScanStatus = "Failed";
            connection.ScanError = ex.Message;
            connection.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task IngestScanResultAsync(
        Guid databaseConnectionId,
        DatabaseSchemaScanResult scanResult,
        CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.DatabaseConnections
            .FirstOrDefaultAsync(c => c.Id == databaseConnectionId, cancellationToken)
            ?? throw new InvalidOperationException($"Database connection {databaseConnectionId} not found.");

        var existingTables = await _dbContext.DatabaseTables
            .Include(t => t.Columns)
            .Where(t => t.DatabaseConnectionId == databaseConnectionId)
            .ToListAsync(cancellationToken);

        var tableLookup = existingTables.ToDictionary(
            t => $"{t.SchemaName}.{t.TableName}",
            StringComparer.OrdinalIgnoreCase);

        var capturedAt = scanResult.ScannedAt;
        var tableIdByKey = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var scanned in scanResult.Tables)
        {
            var key = $"{scanned.SchemaName}.{scanned.TableName}";
            if (!tableLookup.TryGetValue(key, out var table))
            {
                table = new DatabaseTable
                {
                    Id = Guid.NewGuid(),
                    DatabaseConnectionId = databaseConnectionId,
                    SchemaName = scanned.SchemaName,
                    TableName = scanned.TableName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.DatabaseTables.Add(table);
                tableLookup[key] = table;
            }

            table.RowCountEstimate = scanned.RowCountEstimate;
            table.Description = scanned.Description;
            table.CapturedAt = capturedAt;
            table.UpdatedAt = DateTime.UtcNow;

            var existingColumns = table.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var col in scanned.Columns)
            {
                if (!existingColumns.TryGetValue(col.Name, out var column))
                {
                    column = new DatabaseColumn
                    {
                        Id = Guid.NewGuid(),
                        DatabaseTableId = table.Id,
                        Name = col.Name,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    table.Columns.Add(column);
                    _dbContext.DatabaseColumns.Add(column);
                }

                column.DataType = col.DataType;
                column.MaxLength = col.MaxLength;
                column.IsNullable = col.IsNullable;
                column.IsPrimaryKey = col.IsPrimaryKey;
                column.IsForeignKey = col.IsForeignKey;
                column.OrdinalPosition = col.OrdinalPosition;
                column.UpdatedAt = DateTime.UtcNow;
            }

            tableIdByKey[key] = table.Id;
        }

        var existingRelationships = await _dbContext.DatabaseRelationships
            .Where(r => r.DatabaseConnectionId == databaseConnectionId)
            .ToListAsync(cancellationToken);

        _dbContext.DatabaseRelationships.RemoveRange(existingRelationships);

        foreach (var rel in scanResult.Relationships)
        {
            var fromKey = $"{rel.FromSchema}.{rel.FromTable}";
            var toKey = $"{rel.ToSchema}.{rel.ToTable}";
            if (!tableIdByKey.TryGetValue(fromKey, out var fromTableId)
                || !tableIdByKey.TryGetValue(toKey, out var toTableId))
            {
                continue;
            }

            _dbContext.DatabaseRelationships.Add(new DatabaseRelationship
            {
                Id = Guid.NewGuid(),
                DatabaseConnectionId = databaseConnectionId,
                FromTableId = fromTableId,
                FromColumnName = rel.FromColumn,
                ToTableId = toTableId,
                ToColumnName = rel.ToColumn,
                RelationshipType = rel.RelationshipType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        connection.LastScannedAt = capturedAt;
        connection.Host = scanResult.Host ?? connection.Host;
        connection.DatabaseName = scanResult.DatabaseName ?? connection.DatabaseName;
        connection.ScanStatus = "Completed";
        connection.ScanError = null;
        connection.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Ingested schema scan for connection {ConnectionId}: {TableCount} tables",
            databaseConnectionId, scanResult.Tables.Count);

        await _notifications.PublishAsync(
            "database.scan.completed",
            "Database scan completed",
            $"Captured {scanResult.Tables.Count} tables for {connection.Name}.",
            new { connectionId = databaseConnectionId, tableCount = scanResult.Tables.Count },
            cancellationToken);
    }
}
