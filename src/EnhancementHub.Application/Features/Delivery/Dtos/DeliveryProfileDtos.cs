using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Delivery.Dtos;

public sealed record TenantDeploymentEnvironmentDto(
    Guid Id,
    string Name,
    DeploymentEnvironmentType EnvironmentType,
    string? BaseUrlTemplate,
    string? SecretReferencePrefix,
    bool IsActive,
    int SortOrder,
    bool RequiresApprovalForDeploy);

public sealed record TenantDeliveryProfileDto(
    Guid Id,
    Guid TenantId,
    CicdProvider DefaultCicdProvider,
    string? VaultSecretPrefix,
    bool AutoImplementOnApprove,
    bool AutoDeployToTest,
    bool RequirePullRequestReview,
    bool RequireUatSignoff,
    bool RequireProdChangeWindow,
    string? ChangeWindowNotes,
    int QaVideoRetentionDays,
    bool AllowOneClickProdDeploy,
    bool AllowOneClickRollback,
    TestDataStrategy TestDataStrategy,
    bool AllowProdToTestRefresh,
    IReadOnlyList<TenantDeploymentEnvironmentDto> Environments);

public sealed record ApplicationDeliveryProfileDto(
    Guid Id,
    Guid ApplicationId,
    DeploymentMechanism DeploymentMechanism,
    Guid? PrimaryRepositoryId,
    string BranchNamingPattern,
    string? CicdPipelineReference,
    CicdProvider? CicdProviderOverride,
    string SmokeTestPath,
    DatabaseMigrationStrategy DatabaseMigrationStrategy,
    bool RequiresHumanProdDeploy,
    string? ConfigTransformsJson,
    string? ConnectionMappingsJson,
    bool IsConfigured,
    IReadOnlyList<string> ValidationMessages);

public sealed record DeliveryProfileValidationResultDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
