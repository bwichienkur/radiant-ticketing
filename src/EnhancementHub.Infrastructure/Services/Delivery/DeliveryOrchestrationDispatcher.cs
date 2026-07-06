using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.Background.Executors;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class DeliveryOrchestrationDispatcher : IDeliveryOrchestrationDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveryOrchestrationDispatcher> _logger;

    public DeliveryOrchestrationDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<DeliveryOrchestrationDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void EnqueueProcessing(Guid? enhancementRequestId = null)
    {
        try
        {
            BackgroundJob.Enqueue<DeliveryOrchestrationJobExecutor>(executor =>
                executor.ProcessRequestAsync(enhancementRequestId ?? Guid.Empty, CancellationToken.None));
        }
        catch (InvalidOperationException)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orchestration = scope.ServiceProvider.GetRequiredService<IDeliveryOrchestrationService>();
                    await orchestration.ProcessActiveRunsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Inline delivery processing failed.");
                }
            });
        }
    }
}
