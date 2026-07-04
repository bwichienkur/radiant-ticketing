using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Onboarding.Commands;

public sealed record StartOnboardingSessionCommand : IRequest<OnboardingSessionDto>;

public sealed class StartOnboardingSessionCommandHandler
    : IRequestHandler<StartOnboardingSessionCommand, OnboardingSessionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public StartOnboardingSessionCommandHandler(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<OnboardingSessionDto> Handle(
        StartOnboardingSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated to start onboarding.");
        }

        var existing = await _dbContext.OnboardingSessions
            .Include(s => s.Application)
            .Where(s => s.StartedByUserId == _currentUser.UserId.Value
                && s.Status == OnboardingSessionStatus.InProgress)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return OnboardingSessionMapper.ToDto(existing);
        }

        var now = DateTime.UtcNow;
        var session = new OnboardingSession
        {
            Id = Guid.NewGuid(),
            CurrentStep = OnboardingStep.ApplicationBasics,
            Status = OnboardingSessionStatus.InProgress,
            StartedByUserId = _currentUser.UserId.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.OnboardingSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return OnboardingSessionMapper.ToDto(session);
    }
}

public sealed record AdvanceOnboardingSessionCommand(
    Guid SessionId,
    OnboardingStep Step,
    Guid? ApplicationId = null,
    bool? SkipDatabase = null) : IRequest<OnboardingSessionDto>;

public sealed class AdvanceOnboardingSessionCommandHandler
    : IRequestHandler<AdvanceOnboardingSessionCommand, OnboardingSessionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public AdvanceOnboardingSessionCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<OnboardingSessionDto> Handle(
        AdvanceOnboardingSessionCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.OnboardingSessions
            .Include(s => s.Application)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(OnboardingSession), request.SessionId);

        if (request.ApplicationId.HasValue)
        {
            session.ApplicationId = request.ApplicationId;
            session.Application = await _dbContext.Applications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);
        }

        if (request.SkipDatabase.HasValue)
        {
            session.SkipDatabase = request.SkipDatabase.Value;
        }

        session.CurrentStep = request.Step;
        session.WizardError = null;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return OnboardingSessionMapper.ToDto(session);
    }
}

public sealed record CompleteOnboardingSessionCommand(Guid SessionId) : IRequest<OnboardingSessionDto>;

public sealed class CompleteOnboardingSessionCommandHandler
    : IRequestHandler<CompleteOnboardingSessionCommand, OnboardingSessionDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public CompleteOnboardingSessionCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<OnboardingSessionDto> Handle(
        CompleteOnboardingSessionCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.OnboardingSessions
            .Include(s => s.Application)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(OnboardingSession), request.SessionId);

        var now = DateTime.UtcNow;
        session.Status = OnboardingSessionStatus.Completed;
        session.CurrentStep = OnboardingStep.Complete;
        session.CompletedAt = now;
        session.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return OnboardingSessionMapper.ToDto(session);
    }
}
