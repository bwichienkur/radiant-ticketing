using EnhancementHub.Infrastructure.Background.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class DriftAutopilotJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DriftAutopilotJobExecutor> _logger;

    public DriftAutopilotJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<DriftAutopilotJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var autopilot = scope.ServiceProvider.GetRequiredService<Application.Abstractions.IDriftAutopilotService>();
        var created = await autopilot.AutoDraftRequestsFromDriftAsync(cancellationToken);
        _logger.LogInformation("Drift autopilot job finished. Created {Count} request(s).", created);
    }
}
