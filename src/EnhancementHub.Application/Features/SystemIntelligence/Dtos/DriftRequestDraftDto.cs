namespace EnhancementHub.Application.Features.SystemIntelligence.Dtos;

public sealed record DriftRequestDraftDto(
    Guid FindingId,
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    Guid? TargetApplicationId,
    string? SupportingNotes,
    Guid DatabaseConnectionId,
    string? ConnectionName,
    string Severity);
