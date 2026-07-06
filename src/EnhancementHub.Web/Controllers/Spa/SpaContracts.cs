using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Templates.Dtos;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Web.Controllers.Spa;

public sealed record SpaApprovalActionRequest(ApprovalActionType ActionType, string? Comments);

public sealed record SpaBulkApprovalActionRequest(
    IReadOnlyList<Guid> RequestIds,
    ApprovalActionType ActionType,
    string? Comments);

public sealed record SpaAddCommentRequest(string Content, bool IsInternal = false);

public sealed record SpaCreateRequestInput(
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    Guid? TargetApplicationId,
    DateTime? RequestedDueDate,
    string? Department,
    string? SupportingNotes,
    Guid? TemplateId);

public sealed record SpaCreateRequestFormResponse(
    IReadOnlyList<ApplicationDto> Applications,
    IReadOnlyList<EnhancementTemplateSummaryDto> Templates);

public sealed record SpaValidatePathRequest(string Path);

public sealed record SpaOnboardingBasicsRequest(
    string Name,
    string? BusinessDomain,
    string? Purpose,
    string? RiskSensitiveAreas,
    string? OwnerTeamName,
    string? DeploymentNotes);

public sealed record SpaOnboardingRepositoryRequest(
    string RepositoryName,
    string RepositoryPath,
    string DefaultBranch);

public sealed record SpaOnboardingDatabaseRequest(
    string ConnectionName,
    DatabaseProviderType Provider,
    string ConnectionString,
    bool IsReadOnly);

public sealed record SpaGitHubAppCloneRequest(
    string RepositoryName,
    string Owner,
    string Repository,
    string DefaultBranch,
    long? InstallationId);

public sealed record SpaGitCloneRequest(
    string RepositoryName,
    string RepositoryUrl,
    string DefaultBranch,
    string? AccessToken);

public sealed record SpaBuildConnectionStringRequest(
    DatabaseProviderType Provider,
    string Host,
    int Port,
    string Database,
    string? Username,
    string? Password,
    bool IntegratedSecurity);

public sealed record SpaOnPremAgentRequest(
    Guid ApplicationId,
    string ConnectionName,
    DatabaseProviderType Provider);
