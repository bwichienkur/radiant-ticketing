using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class DriftDigestJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DriftDigestJobExecutor> _logger;

    public DriftDigestJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<DriftDigestJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var unresolvedCount = await dbContext.SchemaDriftFindings
            .AsNoTracking()
            .CountAsync(f => !f.IsResolved, cancellationToken);

        if (unresolvedCount == 0)
        {
            _logger.LogInformation("Weekly drift digest skipped — no unresolved findings.");
            return;
        }

        var topFindings = await dbContext.SchemaDriftFindings
            .AsNoTracking()
            .Include(f => f.DatabaseConnection)
            .Where(f => !f.IsResolved)
            .OrderByDescending(f => f.Severity)
            .ThenByDescending(f => f.DetectedAt)
            .Take(5)
            .Select(f => new DriftDigestFindingSummary(
                f.Title,
                f.Severity.ToString(),
                f.DatabaseConnection.Name,
                f.DatabaseConnectionId))
            .ToListAsync(cancellationToken);

        await notificationService.NotifyArchitectsOfDriftDigestAsync(
            unresolvedCount,
            topFindings,
            cancellationToken);

        _logger.LogInformation(
            "Weekly drift digest sent for {UnresolvedCount} unresolved finding(s).",
            unresolvedCount);
    }
}
