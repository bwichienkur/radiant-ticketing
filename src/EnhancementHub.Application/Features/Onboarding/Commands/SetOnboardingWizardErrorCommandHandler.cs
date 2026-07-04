using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Onboarding.Commands;

public sealed class SetOnboardingWizardErrorCommandHandler
    : IRequestHandler<SetOnboardingWizardErrorCommand, Unit>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public SetOnboardingWizardErrorCommandHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<Unit> Handle(SetOnboardingWizardErrorCommand request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.OnboardingSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.OnboardingSession), request.SessionId);

        session.WizardError = string.IsNullOrWhiteSpace(request.ErrorMessage) ? null : request.ErrorMessage.Trim();
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
