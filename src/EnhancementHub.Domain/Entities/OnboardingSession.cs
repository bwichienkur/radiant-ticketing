using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class OnboardingSession : BaseEntity
{
    public Guid? ApplicationId { get; set; }
    public OnboardingStep CurrentStep { get; set; } = OnboardingStep.ApplicationBasics;
    public OnboardingSessionStatus Status { get; set; } = OnboardingSessionStatus.InProgress;
    public Guid StartedByUserId { get; set; }
    public bool SkipDatabase { get; set; }
    public DiscoveryJobState DiscoveryJobState { get; set; } = DiscoveryJobState.None;
    public string? DiscoveryStatus { get; set; }
    public string? LastError { get; set; }
    public DateTime? DiscoveryCompletedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? OnPremAgentId { get; set; }
    public Guid? OnPremConnectionId { get; set; }

    public Application? Application { get; set; }
}
