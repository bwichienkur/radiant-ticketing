using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class DatabaseSchemaScanJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseSchemaScanJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(10);

    public DatabaseSchemaScanJob(IServiceScopeFactory scopeFactory, ILogger<DatabaseSchemaScanJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database schema scan job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
                var scannerFactory = scope.ServiceProvider.GetRequiredService<DatabaseSchemaScannerFactory>();
                var ingestionService = scope.ServiceProvider.GetRequiredService<DatabaseSchemaIngestionService>();
                var secretProtector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();

                var pendingConnections = await dbContext.DatabaseConnections
                    .Where(c => c.ScanStatus == "Pending" && !c.IsReadOnly)
                    .ToListAsync(stoppingToken);

                foreach (var connection in pendingConnections)
                {
                    await ScanConnectionAsync(connection, dbContext, scannerFactory, ingestionService, secretProtector, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Database schema scan job iteration failed.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
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
