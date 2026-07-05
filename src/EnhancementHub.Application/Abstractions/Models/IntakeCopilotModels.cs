namespace EnhancementHub.Application.Abstractions.Models;

public sealed class IntakeCopilotDraft
{
    public string Title { get; set; } = string.Empty;
    public string BusinessDescription { get; set; } = string.Empty;
    public string DesiredOutcome { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public Guid? TargetApplicationId { get; set; }
    public string? Department { get; set; }
    public string? SupportingNotes { get; set; }
    public string? SuggestedTemplateDomainCategory { get; set; }
}

public sealed class IntakeCopilotMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public sealed class IntakeCopilotTurnResult
{
    public string AssistantMessage { get; set; } = string.Empty;
    public IReadOnlyList<string> FollowUpQuestions { get; set; } = [];
    public bool IsComplete { get; set; }
    public IntakeCopilotDraft? Draft { get; set; }
    public Guid? SuggestedTemplateId { get; set; }
    public bool UsedMockAi { get; set; }
}

public sealed class IntakeCopilotSessionState
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TurnCount { get; set; }
    public IReadOnlyList<IntakeCopilotMessage> Messages { get; set; } = [];
    public IntakeCopilotDraft? Draft { get; set; }
    public Guid? SuggestedTemplateId { get; set; }
    public Guid? CreatedRequestId { get; set; }
    public string? LastAssistantMessage { get; set; }
}
