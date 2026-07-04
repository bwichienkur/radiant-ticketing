using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Onboarding.Commands;
using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Onboarding.Queries;

public sealed record GetOnboardingStatusQuery : IRequest<OnboardingStatusDto>;

public sealed class GetOnboardingStatusQueryHandler
    : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetOnboardingStatusQueryHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<OnboardingStatusDto> Handle(
        GetOnboardingStatusQuery request,
        CancellationToken cancellationToken)
    {
        var applicationCount = await _dbContext.Applications.CountAsync(cancellationToken);
        var repositoryCount = await _dbContext.Repositories.CountAsync(cancellationToken);
        var databaseCount = await _dbContext.DatabaseConnections.CountAsync(cancellationToken);
        var hasIndexedRepository = await _dbContext.Repositories
            .AnyAsync(r => r.IndexingStatus == IndexingStatus.Completed, cancellationToken);
        var hasScannedDatabase = await _dbContext.DatabaseConnections
            .AnyAsync(c => c.LastScannedAt != null, cancellationToken);
        var hasSystemGraph = await _dbContext.SystemGraphNodes.AnyAsync(cancellationToken);

        OnboardingSession? activeSession = null;
        if (_currentUser.UserId.HasValue)
        {
            activeSession = await _dbContext.OnboardingSessions
                .Include(s => s.Application)
                .Where(s => s.StartedByUserId == _currentUser.UserId.Value
                    && s.Status == OnboardingSessionStatus.InProgress)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new OnboardingStatusDto(
            applicationCount,
            repositoryCount,
            databaseCount,
            hasIndexedRepository,
            hasScannedDatabase,
            hasSystemGraph,
            activeSession?.Id,
            activeSession?.Application?.Name,
            activeSession?.CurrentStep);
    }
}

public sealed record GetOnboardingSessionQuery(Guid SessionId) : IRequest<OnboardingSessionDto>;

public sealed class GetOnboardingSessionQueryHandler
    : IRequestHandler<GetOnboardingSessionQuery, OnboardingSessionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetOnboardingSessionQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<OnboardingSessionDto> Handle(
        GetOnboardingSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.OnboardingSessions
            .Include(s => s.Application)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(OnboardingSession), request.SessionId);

        return OnboardingSessionMapper.ToDto(session);
    }
}

public sealed record ValidateRepositoryPathQuery(string Path) : IRequest<RepositoryPathValidationDto>;

public sealed class ValidateRepositoryPathQueryHandler
    : IRequestHandler<ValidateRepositoryPathQuery, RepositoryPathValidationDto>
{
    private readonly IGitRepositoryScanner _scanner;

    public ValidateRepositoryPathQueryHandler(IGitRepositoryScanner scanner) =>
        _scanner = scanner;

    public async Task<RepositoryPathValidationDto> Handle(
        ValidateRepositoryPathQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return new RepositoryPathValidationDto(false, "Repository path is required.", 0, 0, 0, 0);
        }

        var path = request.Path.Trim();
        if (!Directory.Exists(path))
        {
            return new RepositoryPathValidationDto(false, $"Path not found: {path}", 0, 0, 0, 0);
        }

        try
        {
            var csharpFiles = Directory
                .EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
                .Count(f => !f.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    && !f.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

            var scan = await _scanner.ScanAsync(path, cancellationToken);
            return new RepositoryPathValidationDto(
                true,
                null,
                csharpFiles,
                scan.Controllers.Count,
                scan.DbContextTypes.Count,
                scan.EntityMappings.Count);
        }
        catch (Exception ex)
        {
            return new RepositoryPathValidationDto(false, ex.Message, 0, 0, 0, 0);
        }
    }
}

public sealed record GetOnboardingReviewQuery(Guid ApplicationId) : IRequest<OnboardingReviewDto>;

public sealed class GetOnboardingReviewQueryHandler
    : IRequestHandler<GetOnboardingReviewQuery, OnboardingReviewDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetOnboardingReviewQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<OnboardingReviewDto> Handle(
        GetOnboardingReviewQuery request,
        CancellationToken cancellationToken)
    {
        var application = await _dbContext.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Application), request.ApplicationId);

        var repositoryCount = await _dbContext.Repositories
            .CountAsync(r => r.ApplicationId == request.ApplicationId, cancellationToken);
        var databaseCount = await _dbContext.DatabaseConnections
            .CountAsync(c => c.ApplicationId == request.ApplicationId, cancellationToken);
        var graphNodeCount = await _dbContext.SystemGraphNodes
            .CountAsync(n => n.ApplicationId == request.ApplicationId, cancellationToken);
        var nodeIds = await _dbContext.SystemGraphNodes
            .Where(n => n.ApplicationId == request.ApplicationId)
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);
        var graphEdgeCount = await _dbContext.SystemGraphEdges
            .CountAsync(e => nodeIds.Contains(e.SourceNodeId) || nodeIds.Contains(e.TargetNodeId), cancellationToken);
        var connectionIds = await _dbContext.DatabaseConnections
            .Where(c => c.ApplicationId == request.ApplicationId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        var driftFindingCount = await _dbContext.SchemaDriftFindings
            .CountAsync(f => connectionIds.Contains(f.DatabaseConnectionId), cancellationToken);
        var profiles = await _dbContext.ApplicationProfiles
            .AsNoTracking()
            .Where(p => p.ApplicationId == request.ApplicationId)
            .OrderByDescending(p => p.GeneratedAt)
            .ToListAsync(cancellationToken);

        var latestProfile = profiles.FirstOrDefault();
        var summary = latestProfile is null
            ? null
            : $"DbContexts: {latestProfile.DatabaseUsage}; APIs: {latestProfile.ExternalIntegrations}";

        return new OnboardingReviewDto(
            application.Id,
            application.Name,
            repositoryCount,
            databaseCount,
            graphNodeCount,
            graphEdgeCount,
            driftFindingCount,
            profiles.Count,
            summary);
    }
}
