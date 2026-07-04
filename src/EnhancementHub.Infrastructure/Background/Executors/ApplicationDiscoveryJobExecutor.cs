using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Onboarding.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class ApplicationDiscoveryJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApplicationDiscoveryJobExecutor> _logger;

    public ApplicationDiscoveryJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<ApplicationDiscoveryJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var queued = await dbContext.OnboardingSessions
            .Where(s => s.DiscoveryJobState == DiscoveryJobState.Queued && s.ApplicationId != null)
            .OrderBy(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (queued is null)
        {
            return;
        }

        queued.DiscoveryJobState = DiscoveryJobState.Running;
        queued.DiscoveryStatus = "Discovery started...";
        queued.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = await mediator.Send(
            new RunApplicationDiscoveryCommand(queued.ApplicationId!.Value, queued.Id),
            cancellationToken);

        queued.DiscoveryJobState = result.Succeeded
            ? DiscoveryJobState.Completed
            : DiscoveryJobState.Failed;
        queued.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Discovery job for session {SessionId} finished with state {State}",
            queued.Id,
            queued.DiscoveryJobState);
    }
}
