using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Onboarding.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Onboarding.Queries;

public sealed class GetOnboardingWizardPrefillQueryHandler
    : IRequestHandler<GetOnboardingWizardPrefillQuery, OnboardingWizardPrefillDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetOnboardingWizardPrefillQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<OnboardingWizardPrefillDto> Handle(
        GetOnboardingWizardPrefillQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.OnboardingSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.OnboardingSession), request.SessionId);

        if (!session.ApplicationId.HasValue)
        {
            return new OnboardingWizardPrefillDto(null, null, null);
        }

        var applicationId = session.ApplicationId.Value;

        var application = await _dbContext.Applications
            .AsNoTracking()
            .Include(a => a.OwnerTeam)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        OnboardingStep1PrefillDto? step1 = application is null
            ? null
            : new OnboardingStep1PrefillDto(
                application.Name,
                application.BusinessDomain,
                application.Purpose,
                application.RiskSensitiveAreas,
                application.OwnerTeam?.Name);

        var repository = await _dbContext.Repositories
            .AsNoTracking()
            .Where(r => r.ApplicationId == applicationId)
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        OnboardingStep2PrefillDto? step2 = repository is null
            ? null
            : new OnboardingStep2PrefillDto(
                repository.Name,
                repository.Url,
                repository.DefaultBranch);

        var connection = await _dbContext.DatabaseConnections
            .AsNoTracking()
            .Where(c => c.ApplicationId == applicationId)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        OnboardingStep3PrefillDto? step3 = connection is null
            ? null
            : new OnboardingStep3PrefillDto(
                connection.Name,
                connection.Provider,
                connection.IsReadOnly);

        return new OnboardingWizardPrefillDto(step1, step2, step3);
    }
}
