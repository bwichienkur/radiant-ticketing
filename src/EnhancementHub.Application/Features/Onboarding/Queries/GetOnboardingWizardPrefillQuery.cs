using EnhancementHub.Application.Features.Onboarding.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Onboarding.Queries;

public sealed record GetOnboardingWizardPrefillQuery(Guid SessionId) : IRequest<OnboardingWizardPrefillDto>;
