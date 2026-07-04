using MediatR;

namespace EnhancementHub.Application.Features.Onboarding.Commands;

public sealed record SetOnboardingWizardErrorCommand(
    Guid SessionId,
    string? ErrorMessage) : IRequest<Unit>;
