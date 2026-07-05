namespace EnhancementHub.Application.Features.Templates.Dtos;

public sealed record EnhancementTemplateDto(
    Guid Id,
    string Name,
    string DomainCategory,
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    string? SupportingNotes,
    bool IsActive);

public sealed record EnhancementTemplateSummaryDto(
    Guid Id,
    string Name,
    string DomainCategory,
    string Title,
    string Priority);
