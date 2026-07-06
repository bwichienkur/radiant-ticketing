using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public sealed record GitHubPullRequestResult(
    bool Succeeded,
    string? BranchName,
    string? PullRequestUrl,
    int? PullRequestNumber,
    string? CommitSha,
    bool IsSimulation,
    string? ErrorMessage);

public interface IGitHubAppRepositoryService
{
    bool IsConfigured { get; }

    Task<GitHubPullRequestResult> CreateImplementationPullRequestAsync(
        string owner,
        string repository,
        string baseBranch,
        string branchName,
        string title,
        string body,
        string implementationMarkdown,
        long? installationId = null,
        CancellationToken cancellationToken = default);
}

public sealed record DeploymentConfigBundle(
    DeploymentEnvironmentType EnvironmentType,
    string EnvironmentName,
    string? BaseUrl,
    string ConfigJson,
    IReadOnlyDictionary<string, string> ConnectionSecretRefs,
    IReadOnlyDictionary<string, string> EnvironmentVariables);

public sealed record DeploymentResult(
    bool Succeeded,
    string? DeploymentReference,
    string? DeployedUrl,
    bool IsSimulation,
    string? ErrorMessage);

public sealed record DeploymentContext(
    Guid EnhancementRequestId,
    Guid ApplicationId,
    Guid DeliveryRunId,
    DeploymentEnvironmentType EnvironmentType,
    CicdProvider Provider,
    string? PipelineReference,
    DeploymentMechanism Mechanism,
    DeploymentConfigBundle ConfigBundle,
    string? RepositoryOwner,
    string? RepositoryName,
    string? DefaultBranch);

public interface IDeploymentConfigBundleBuilder
{
    Task<DeploymentConfigBundle> BuildAsync(
        ApplicationDeliveryProfile appProfile,
        TenantDeploymentEnvironment environment,
        CancellationToken cancellationToken = default);
}

public interface IDeploymentAdapter
{
    CicdProvider Provider { get; }

    bool CanHandle(DeploymentContext context);

    Task<DeploymentResult> DeployAsync(DeploymentContext context, CancellationToken cancellationToken = default);
}

public interface IDeploymentAdapterFactory
{
    IDeploymentAdapter Resolve(DeploymentContext context);
}

public sealed record QaTestStepResult(string Step, bool Passed, string Detail);

public sealed record QaEvidenceResult(
    bool Passed,
    IReadOnlyList<QaTestStepResult> Steps,
    string? VideoStoragePath,
    string? ReportStoragePath,
    bool IsSimulation);

public interface IQaEvidenceService
{
    Task<QaEvidenceResult> RunQaAsync(
        Guid requestId,
        Guid deliveryRunId,
        string testUrl,
        string? testingPlan,
        string desiredOutcome,
        CancellationToken cancellationToken = default);
}

public interface IChangeWindowEvaluator
{
    bool IsProductionDeployAllowed(DateTime utcNow, string? changeWindowNotes, bool requireChangeWindow);
}

public interface IDeliveryOrchestrationService
{
    Task<Guid> StartDeliveryRunAsync(Guid enhancementRequestId, CancellationToken cancellationToken = default);

    Task ProcessActiveRunsAsync(CancellationToken cancellationToken = default);

    Task SignUatAsync(
        Guid enhancementRequestId,
        Guid userId,
        bool approved,
        string? notes,
        CancellationToken cancellationToken = default);

    Task AdvancePastPullRequestReviewAsync(Guid enhancementRequestId, CancellationToken cancellationToken = default);
}

public interface IDeliveryOrchestrationDispatcher
{
    void EnqueueProcessing(Guid? enhancementRequestId = null);
}
