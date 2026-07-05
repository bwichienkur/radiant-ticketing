using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Options;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class DatabaseSchemaScanJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DatabaseScalingOptions _scalingOptions;
    private readonly ILogger<DatabaseSchemaScanJobExecutor> _logger;

    public DatabaseSchemaScanJobExecutor(
        IServiceScopeFactory scopeFactory,
        IOptions<DatabaseScalingOptions> scalingOptions,
        ILogger<DatabaseSchemaScanJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _scalingOptions = scalingOptions.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();

        var pendingConnections = await dbContext.DatabaseConnections
            .Where(c => c.ScanStatus == "Pending" && !c.IsReadOnly)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var maxConcurrency = Math.Clamp(_scalingOptions.SchemaScanMaxConcurrency, 1, 16);
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = pendingConnections.Select(async connectionId =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                using var innerScope = _scopeFactory.CreateScope();
                await ScanConnectionAsync(connectionId, innerScope.ServiceProvider, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task ScanConnectionAsync(
        Guid connectionId,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<IEnhancementHubDbContext>();
        var scannerFactory = services.GetRequiredService<DatabaseSchemaScannerFactory>();
        var ingestionService = services.GetRequiredService<DatabaseSchemaIngestionService>();
        var secretProtector = services.GetRequiredService<ISecretProtector>();

        var connection = await dbContext.DatabaseConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);

        if (connection is null || connection.ScanStatus != "Pending" || connection.IsReadOnly)
        {
            return;
        }

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
