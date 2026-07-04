using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Onboarding.Dtos;

public sealed record OnboardingStatusDto(
    int ApplicationCount,
    int RepositoryCount,
    int DatabaseConnectionCount,
    bool HasIndexedRepository,
    bool HasScannedDatabase,
    bool HasSystemGraph,
    Guid? ActiveSessionId,
    string? ActiveSessionApplicationName,
    OnboardingStep? ActiveSessionStep);

public sealed record OnboardingSessionDto(
    Guid Id,
    Guid? ApplicationId,
    string? ApplicationName,
    OnboardingStep CurrentStep,
    OnboardingSessionStatus Status,
    bool SkipDatabase,
    string? DiscoveryStatus,
    string? LastError,
    DateTime? DiscoveryCompletedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);

public sealed record RepositoryPathValidationDto(
    bool IsValid,
    string? ErrorMessage,
    int CSharpFileCount,
    int ControllerCount,
    int DbContextCount,
    int EntityMappingCount);

public sealed record ApplicationDiscoveryResultDto(
    Guid ApplicationId,
    int RepositoriesIndexed,
    int DatabasesScanned,
    int GraphNodeCount,
    int GraphEdgeCount,
    int DriftFindingCount,
    bool Succeeded,
    string? ErrorMessage);

public sealed record OnboardingReviewDto(
    Guid ApplicationId,
    string ApplicationName,
    int RepositoryCount,
    int DatabaseConnectionCount,
    int GraphNodeCount,
    int GraphEdgeCount,
    int DriftFindingCount,
    int ProfileCount,
    string? LatestProfileSummary);
