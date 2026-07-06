using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class NightlyRegressionJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NightlyRegressionJobExecutor> _logger;

    public NightlyRegressionJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<NightlyRegressionJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting nightly regression job");
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INightlyRegressionService>();
        await service.RunScheduledRegressionAsync(cancellationToken);
    }
}
