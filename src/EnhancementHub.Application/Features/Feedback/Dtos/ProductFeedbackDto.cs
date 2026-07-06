namespace EnhancementHub.Application.Features.Feedback.Dtos;

public sealed record ProductFeedbackDto(
    Guid Id,
    string WorkflowKey,
    int NpsScore,
    string? Comment,
    DateTime CreatedAt);
