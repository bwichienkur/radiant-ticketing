using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Onboarding.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class ApplicationDiscoveryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApplicationDiscoveryJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(3);

    public ApplicationDiscoveryJob(IServiceScopeFactory scopeFactory, ILogger<ApplicationDiscoveryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Application discovery job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var queued = await dbContext.OnboardingSessions
                    .Where(s => s.DiscoveryJobState == DiscoveryJobState.Queued && s.ApplicationId != null)
                    .OrderBy(s => s.UpdatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (queued is not null)
                {
                    queued.DiscoveryJobState = DiscoveryJobState.Running;
                    queued.DiscoveryStatus = "Discovery started...";
                    queued.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);

                    var result = await mediator.Send(
                        new RunApplicationDiscoveryCommand(queued.ApplicationId!.Value, queued.Id),
                        stoppingToken);

                    queued.DiscoveryJobState = result.Succeeded
                        ? DiscoveryJobState.Completed
                        : DiscoveryJobState.Failed;
                    queued.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Application discovery job iteration failed.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
