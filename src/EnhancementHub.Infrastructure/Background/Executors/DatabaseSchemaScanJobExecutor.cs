using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class DatabaseSchemaScanJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseSchemaScanJobExecutor> _logger;

    public DatabaseSchemaScanJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseSchemaScanJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
        var scannerFactory = scope.ServiceProvider.GetRequiredService<DatabaseSchemaScannerFactory>();
        var ingestionService = scope.ServiceProvider.GetRequiredService<DatabaseSchemaIngestionService>();
        var secretProtector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();

        var pendingConnections = await dbContext.DatabaseConnections
            .Where(c => c.ScanStatus == "Pending" && !c.IsReadOnly)
            .ToListAsync(cancellationToken);

        foreach (var connection in pendingConnections)
        {
            await ScanConnectionAsync(
                connection,
                dbContext,
                scannerFactory,
                ingestionService,
                secretProtector,
                cancellationToken);
        }
    }

    private async Task ScanConnectionAsync(
        DatabaseConnection connection,
        IEnhancementHubDbContext dbContext,
        DatabaseSchemaScannerFactory scannerFactory,
        DatabaseSchemaIngestionService ingestionService,
        ISecretProtector secretProtector,
        CancellationToken cancellationToken)
    {
        connection.ScanStatus = "InProgress";
        connection.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var connectionString = secretProtector.Unprotect(connection.ConnectionStringProtected);
            var scanResult = await scannerFactory.ScanAsync(connectionString, connection.Provider, cancellationToken);
            await ingestionService.IngestScanResultAsync(connection.Id, scanResult, cancellationToken);
            _logger.LogInformation("Completed schema scan for connection {ConnectionId}", connection.Id);
        }
        catch (Exception ex)
        {
            connection.ScanStatus = "Failed";
            connection.ScanError = ex.Message;
            connection.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Schema scan failed for connection {ConnectionId}", connection.Id);
        }
    }
}
