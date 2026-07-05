using EnhancementHub.Application.Abstractions.Models;

namespace EnhancementHub.Application.Features.IntakeCopilot.Dtos;

public sealed record IntakeCopilotSessionDto(
    Guid Id,
    string Status,
    int TurnCount,
    IReadOnlyList<IntakeCopilotMessageDto> Messages,
    IntakeCopilotDraftDto? Draft,
    Guid? SuggestedTemplateId,
    Guid? CreatedRequestId,
    string? LastAssistantMessage);

public sealed record IntakeCopilotMessageDto(
    string Role,
    string Content,
    DateTime OccurredAt);

public sealed record IntakeCopilotDraftDto(
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    Guid? TargetApplicationId,
    string? Department,
    string? SupportingNotes,
    string? SuggestedTemplateDomainCategory);

public sealed record IntakeCopilotTurnResponseDto(
    IntakeCopilotSessionDto Session,
    string AssistantMessage,
    IReadOnlyList<string> FollowUpQuestions,
    bool IsComplete,
    bool UsedMockAi);
