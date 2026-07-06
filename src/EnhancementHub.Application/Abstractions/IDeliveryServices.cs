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

public sealed record GitHubFileUpsertResult(bool Succeeded, string? CommitSha, bool IsSimulation, string? ErrorMessage);

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

    Task<GitHubFileUpsertResult> UpsertBranchFileAsync(
        string owner,
        string repository,
        string branch,
        string path,
        string content,
        string commitMessage,
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
    string? DefaultBranch,
    string? ArtifactReference = null,
    string? PromoteFromEnvironment = null);

public sealed record RollbackContext(
    Guid EnhancementRequestId,
    Guid ApplicationId,
    Guid DeliveryRunId,
    CicdProvider Provider,
    string? PipelineReference,
    DeploymentMechanism Mechanism,
    string? RepositoryOwner,
    string? RepositoryName,
    string? DefaultBranch,
    string? RollbackDeployReference,
    string? RollbackCommitSha);

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

    Task<DeploymentResult> RollbackAsync(RollbackContext context, CancellationToken cancellationToken = default);
}

public interface IDeploymentAdapterFactory
{
    IDeploymentAdapter Resolve(DeploymentContext context);

    IDeploymentAdapter ResolveForRollback(RollbackContext context);
}

public sealed record QaTestStepResult(string Step, bool Passed, string Detail);

public sealed record TestCaseStepDefinition(
    int Order,
    string Action,
    string? ExpectedResult);

public sealed record QaManifestCase(
    Guid TestCaseId,
    Guid TestCaseVersionId,
    string Title,
    bool IsRegressionCase,
    IReadOnlyList<TestCaseStepDefinition> Steps);

public sealed record QaRunManifest(
    Guid EnhancementRequestId,
    Guid DeliveryRunId,
    Guid ApplicationId,
    string TestUrl,
    string DesiredOutcome,
    IReadOnlyList<QaManifestCase> Cases);

public sealed record QaCaseRunResult(
    Guid TestCaseId,
    Guid TestCaseVersionId,
    string Title,
    bool IsRegressionCase,
    bool Passed,
    int DurationMs,
    string Detail,
    IReadOnlyList<QaTestStepResult> Steps,
    string? ScreenshotStoragePath);

public sealed record QaEvidenceResult(
    bool Passed,
    IReadOnlyList<QaTestStepResult> Steps,
    IReadOnlyList<QaCaseRunResult> CaseResults,
    string? VideoStoragePath,
    string? ReportStoragePath,
    QaRunnerKind Runner,
    bool IsSimulation);

public interface ITestCaseCatalogService
{
    Task EnsureDraftCasesForRequestAsync(Guid enhancementRequestId, CancellationToken cancellationToken = default);

    Task<QaRunManifest> PrepareQaRunAsync(
        EnhancementDeliveryRun run,
        CancellationToken cancellationToken = default);

    Task<QaRunManifest> PrepareRegressionManifestAsync(
        Guid applicationId,
        string testUrl,
        CancellationToken cancellationToken = default);

    Task PromotePassedCasesToRegressionAsync(
        Guid enhancementRequestId,
        Guid deliveryRunId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestCaseExportItem>> GetExportableCasesForRequestAsync(
        Guid enhancementRequestId,
        CancellationToken cancellationToken = default);
}

public sealed record TestCaseExportItem(
    Guid TestCaseId,
    string Title,
    string StepsJson,
    string SuggestedRepositoryPath);

public interface ITestCaseRepoExporter
{
    Task ExportRequestCasesToBranchAsync(
        Guid enhancementRequestId,
        string owner,
        string repository,
        string branch,
        CancellationToken cancellationToken = default);
}

public interface INightlyRegressionService
{
    Task RunScheduledRegressionAsync(CancellationToken cancellationToken = default);
}

public interface IQaRunner
{
    QaRunnerKind RunnerKind { get; }

    Task<QaEvidenceResult> RunAsync(QaRunManifest manifest, CancellationToken cancellationToken = default);
}

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

    Task TriggerProductionDeployAsync(Guid enhancementRequestId, CancellationToken cancellationToken = default);

    Task RollbackProductionAsync(Guid enhancementRequestId, string? reason, CancellationToken cancellationToken = default);
}

public interface IDeliveryOrchestrationDispatcher
{
    void EnqueueProcessing(Guid? enhancementRequestId = null);
}
