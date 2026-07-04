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
    DiscoveryJobState DiscoveryJobState,
    string? DiscoveryStatus,
    string? LastError,
    string? WizardError,
    DateTime? DiscoveryCompletedAt,
    DateTime? CompletedAt,
    Guid? OnPremAgentId,
    Guid? OnPremConnectionId,
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

public sealed record GitCloneRequestDto(
    bool Succeeded,
    string? LocalPath,
    string? ErrorMessage);

public sealed record OnPremAgentSetupDto(
    Guid AgentId,
    Guid ConnectionId,
    string ConnectionName,
    string ApiBaseUrl,
    string AgentConfigSnippet,
    string RunCommand);

public sealed record DatabaseConnectionStringDto(string ConnectionString);
