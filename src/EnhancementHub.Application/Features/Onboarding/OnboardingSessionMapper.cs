using EnhancementHub.Application.Features.Onboarding.Dtos;
using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Features.Onboarding;

public static class OnboardingSessionMapper
{
    public static OnboardingSessionDto ToDto(OnboardingSession session) =>
        new(
            session.Id,
            session.ApplicationId,
            session.Application?.Name,
            session.CurrentStep,
            session.Status,
            session.SkipDatabase,
            session.DiscoveryStatus,
            session.LastError,
            session.DiscoveryCompletedAt,
            session.CompletedAt,
            session.CreatedAt);
}
