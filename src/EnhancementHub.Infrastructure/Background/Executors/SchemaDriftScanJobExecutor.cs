using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class SchemaDriftScanJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SystemIntelligenceOptions _options;
    private readonly ILogger<SchemaDriftScanJobExecutor> _logger;

    public SchemaDriftScanJobExecutor(
        IServiceScopeFactory scopeFactory,
        IOptions<SystemIntelligenceOptions> options,
        ILogger<SchemaDriftScanJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
        var detector = scope.ServiceProvider.GetRequiredService<ISchemaDriftDetector>();
        var fingerprintService = scope.ServiceProvider.GetRequiredService<ISystemIntelligenceFingerprintService>();

        var connections = await dbContext.DatabaseConnections
            .AsNoTracking()
            .Where(c => !c.IsReadOnly && c.ScanStatus == "Completed")
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var scanned = 0;
        var skipped = 0;

        foreach (var connectionId in connections)
        {
            if (_options.DiffOnlyDriftEnabled
                && !await fingerprintService.IsDriftScanStaleAsync(connectionId, cancellationToken))
            {
                skipped++;
                continue;
            }

            await detector.DetectDriftIfStaleAsync(connectionId, forceFullScan: false, cancellationToken);
            scanned++;
        }

        _logger.LogInformation(
            "Scheduled drift scan complete: {Scanned} scanned, {Skipped} skipped (unchanged)",
            scanned,
            skipped);
    }
}
