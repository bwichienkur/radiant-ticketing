using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Application.Features.Onboarding.Commands;

public sealed record RunApplicationDiscoveryCommand(
    Guid ApplicationId,
    Guid? OnboardingSessionId = null) : IRequest<ApplicationDiscoveryResultDto>;

public sealed class RunApplicationDiscoveryCommandHandler
    : IRequestHandler<RunApplicationDiscoveryCommand, ApplicationDiscoveryResultDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly Abstractions.INotificationPublisher _notifications;
    private readonly ILogger<RunApplicationDiscoveryCommandHandler> _logger;

    public RunApplicationDiscoveryCommandHandler(
        IEnhancementHubDbContext dbContext,
        IMediator mediator,
        Abstractions.INotificationPublisher notifications,
        ILogger<RunApplicationDiscoveryCommandHandler> logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ApplicationDiscoveryResultDto> Handle(
        RunApplicationDiscoveryCommand request,
        CancellationToken cancellationToken)
    {
        var session = request.OnboardingSessionId.HasValue
            ? await _dbContext.OnboardingSessions
                .FirstOrDefaultAsync(s => s.Id == request.OnboardingSessionId, cancellationToken)
            : null;

        if (session is not null)
        {
            session.DiscoveryStatus = "Starting discovery...";
            session.LastError = null;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var repositoriesIndexed = 0;
        var databasesScanned = 0;
        var driftFindingCount = 0;

        try
        {
            var repositories = await _dbContext.Repositories
                .Where(r => r.ApplicationId == request.ApplicationId)
                .ToListAsync(cancellationToken);

            foreach (var repository in repositories)
            {
                await UpdateDiscoveryStatusAsync(session, $"Indexing repository {repository.Name}...", cancellationToken);
                await _mediator.Send(new TriggerRepositoryIndexingCommand(repository.Id), cancellationToken);
                repositoriesIndexed++;
            }

            var connections = await _dbContext.DatabaseConnections
                .Where(c => c.ApplicationId == request.ApplicationId)
                .ToListAsync(cancellationToken);

            foreach (var connection in connections)
            {
                await UpdateDiscoveryStatusAsync(session, $"Scanning database {connection.Name}...", cancellationToken);
                await _mediator.Send(new TriggerDatabaseScanCommand(connection.Id), cancellationToken);
                databasesScanned++;
            }

            await UpdateDiscoveryStatusAsync(session, "Building system graph...", cancellationToken);
            var graph = await _mediator.Send(new BuildSystemGraphCommand(request.ApplicationId), cancellationToken);

            foreach (var connection in connections)
            {
                await UpdateDiscoveryStatusAsync(session, $"Checking schema drift for {connection.Name}...", cancellationToken);
                var drift = await _mediator.Send(new DetectSchemaDriftCommand(connection.Id), cancellationToken);
                driftFindingCount += drift.Findings.Count;
            }

            if (session is not null)
            {
                session.DiscoveryStatus = "Discovery completed successfully.";
                session.DiscoveryCompletedAt = DateTime.UtcNow;
                session.CurrentStep = OnboardingStep.ReviewExport;
                session.LastError = null;
                session.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await _notifications.PublishAsync(
                "onboarding.discovery.completed",
                "Application discovery completed",
                $"Discovery finished for application {request.ApplicationId}.",
                new { request.ApplicationId, repositoriesIndexed, databasesScanned },
                cancellationToken);

            return new ApplicationDiscoveryResultDto(
                request.ApplicationId,
                repositoriesIndexed,
                databasesScanned,
                graph.Nodes.Count,
                graph.Edges.Count,
                driftFindingCount,
                true,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application discovery failed for {ApplicationId}", request.ApplicationId);

            if (session is not null)
            {
                session.DiscoveryStatus = "Discovery failed.";
                session.LastError = ex.Message;
                session.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return new ApplicationDiscoveryResultDto(
                request.ApplicationId,
                repositoriesIndexed,
                databasesScanned,
                0,
                0,
                driftFindingCount,
                false,
                ex.Message);
        }
    }

    private async Task UpdateDiscoveryStatusAsync(
        Domain.Entities.OnboardingSession? session,
        string status,
        CancellationToken cancellationToken)
    {
        if (session is null)
        {
            return;
        }

        session.DiscoveryStatus = status;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
