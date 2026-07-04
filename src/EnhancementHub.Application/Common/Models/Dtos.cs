using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Common.Models;

public sealed record LoginResult(string Token, Guid UserId, string Email, string DisplayName, UserRole Role);

public sealed record DashboardReportDto(
    int TotalRequests,
    int PendingApproval,
    int Approved,
    int InProgress,
    int Completed,
    int RepositoriesIndexed,
    int RepositoriesPending,
    int RecentAuditEvents);

public sealed record EnhancementRequestListItemDto(
    Guid Id,
    string Title,
    string Priority,
    EnhancementRequestStatus Status,
    string? TargetApplicationName,
    string SubmittedByName,
    DateTime CreatedAt);

public sealed record EnhancementRequestDetailDto(
    Guid Id,
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    EnhancementRequestStatus Status,
    string? Department,
    string? SupportingNotes,
    DateTime? RequestedDueDate,
    Guid? TargetApplicationId,
    string? TargetApplicationName,
    string SubmittedByName,
    DateTime CreatedAt,
    EnhancementAnalysisDto? LatestAnalysis,
    IReadOnlyList<ApprovalActionDto> ApprovalHistory,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<ExternalTicketDto> ExternalTickets);

public sealed record EnhancementAnalysisDto(
    Guid Id,
    int Version,
    string? FeatureSummary,
    string? TechnicalRequirements,
    string? TestingPlan,
    double ConfidenceScore,
    RiskLevel RiskLevel,
    string? RiskExplanation,
    bool NeedsClarification,
    IReadOnlyList<string> AffectedApplications,
    IReadOnlyList<string> AffectedRepositories,
    IReadOnlyList<DatabaseChangeDto> DatabaseChanges,
    IReadOnlyList<ApiChangeDto> ApiChanges);

public sealed record DatabaseChangeDto(string TableName, string ChangeType, string Description);
public sealed record ApiChangeDto(string Endpoint, string ChangeType, string Description);

public sealed record ApprovalActionDto(
    Guid Id,
    ApprovalActionType ActionType,
    string? Comments,
    string UserName,
    DateTime CreatedAt);

public sealed record CommentDto(Guid Id, string Content, string UserName, bool IsInternal, DateTime CreatedAt);

public sealed record ExternalTicketDto(
    Guid Id,
    ExternalTicketProvider Provider,
    string ExternalId,
    string ExternalUrl,
    DateTime CreatedAt);

public sealed record ApplicationListItemDto(Guid Id, string Name, string? BusinessDomain, int RepositoryCount);

public sealed record ApplicationDetailDto(
    Guid Id,
    string Name,
    string? BusinessDomain,
    string? Purpose,
    string? Description,
    string? RiskSensitiveAreas,
    IReadOnlyList<RepositorySummaryDto> Repositories,
    ApplicationProfileDto? Profile);

public sealed record ApplicationProfileDto(
    string? Purpose,
    string? KeyComponents,
    string? DatabaseUsage,
    string? ExternalIntegrations,
    string? InternalDependencies,
    string? DeploymentNotes,
    DateTime GeneratedAt);

public sealed record RepositorySummaryDto(
    Guid Id,
    string Name,
    string Url,
    IndexingStatus IndexingStatus,
    DateTime? LastIndexedAt);

public sealed record RepositoryListItemDto(
    Guid Id,
    string Name,
    string Url,
    string ApplicationName,
    IndexingStatus IndexingStatus,
    DateTime? LastIndexedAt);

public sealed record RepositoryStatusDto(
    Guid Id,
    string Name,
    IndexingStatus IndexingStatus,
    DateTime? LastIndexedAt,
    int IndexedFileCount);

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    string EntityType,
    Guid EntityId,
    string? UserName,
    string? Comments,
    DateTime CreatedAt);

public sealed record SystemSettingDto(Guid Id, string Key, string Value, string Category, string? Description);

public sealed record AiPromptConfigurationDto(
    Guid Id,
    string Name,
    string Version,
    string SystemPromptTemplate,
    string UserPromptTemplate,
    bool IsActive);

public sealed record KnowledgeSearchResultDto(Guid ArticleId, string Title, float Score, string Snippet);
