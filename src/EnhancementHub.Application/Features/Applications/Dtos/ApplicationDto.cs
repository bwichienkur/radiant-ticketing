namespace EnhancementHub.Application.Features.Applications.Dtos;

public sealed record ApplicationDto(
    Guid Id,
    string Name,
    string? BusinessDomain,
    string? Purpose,
    string? Description,
    Guid OwnerTeamId,
    string? RiskSensitiveAreas,
    int RepositoryCount);

public sealed record ApplicationProfileDto(
    Guid Id,
    Guid ApplicationId,
    Guid RepositoryId,
    string? Purpose,
    string? BusinessDomain,
    string? KeyComponents,
    string? DatabaseUsage,
    string? ExternalIntegrations,
    string? InternalDependencies,
    string? DeploymentNotes,
    string? RiskSensitiveAreas,
    string? OwnershipMetadata,
    DateTime GeneratedAt);
