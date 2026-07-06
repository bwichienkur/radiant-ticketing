namespace EnhancementHub.Application.Features.IntakeCopilot.Dtos;

public sealed record IntakeQualityScoreDto(
    int Score,
    bool ReadyToSubmit,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> Suggestions);

public sealed record ScoreIntakeDraftRequest(
    string? Title,
    string? BusinessDescription,
    string? DesiredOutcome,
    string? Priority,
    Guid? TargetApplicationId,
    string? Department,
    string? SupportingNotes);
