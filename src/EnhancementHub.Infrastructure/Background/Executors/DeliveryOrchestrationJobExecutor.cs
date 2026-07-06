using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.Services.Delivery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class DeliveryOrchestrationJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveryOrchestrationJobExecutor> _logger;

    public DeliveryOrchestrationJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<DeliveryOrchestrationJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var orchestration = scope.ServiceProvider.GetRequiredService<IDeliveryOrchestrationService>();
        await orchestration.ProcessActiveRunsAsync(cancellationToken);
    }

    public Task ProcessRequestAsync(Guid enhancementRequestId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Delivery processing triggered for request {RequestId}", enhancementRequestId);
        return ExecuteAsync(cancellationToken);
    }
}
