using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class DeliveryOrchestrationService : IDeliveryOrchestrationService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IGitHubAppRepositoryService _gitHubRepository;
    private readonly IDeploymentConfigBundleBuilder _configBundleBuilder;
    private readonly IDeploymentAdapterFactory _adapterFactory;
    private readonly ITestCaseCatalogService _testCaseCatalog;
    private readonly ITestCaseRepoExporter _testCaseRepoExporter;
    private readonly IQaRunner _qaRunner;
    private readonly IChangeWindowEvaluator _changeWindowEvaluator;
    private readonly IAuditService _auditService;
    private readonly ILogger<DeliveryOrchestrationService> _logger;
    private readonly string? _demoOwner;

    public DeliveryOrchestrationService(
        IEnhancementHubDbContext dbContext,
        IGitHubAppRepositoryService gitHubRepository,
        IDeploymentConfigBundleBuilder configBundleBuilder,
        IDeploymentAdapterFactory adapterFactory,
        ITestCaseCatalogService testCaseCatalog,
        ITestCaseRepoExporter testCaseRepoExporter,
        IQaRunner qaRunner,
        IChangeWindowEvaluator changeWindowEvaluator,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<DeliveryOrchestrationService> logger)
    {
        _dbContext = dbContext;
        _gitHubRepository = gitHubRepository;
        _configBundleBuilder = configBundleBuilder;
        _adapterFactory = adapterFactory;
        _testCaseCatalog = testCaseCatalog;
        _testCaseRepoExporter = testCaseRepoExporter;
        _qaRunner = qaRunner;
        _changeWindowEvaluator = changeWindowEvaluator;
        _auditService = auditService;
        _logger = logger;
        _demoOwner = configuration["GitHubApp:DemoOwner"];
    }

    public async Task<Guid> StartDeliveryRunAsync(Guid enhancementRequestId, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.EnhancementRequests
            .Include(r => r.TargetApplication)
            .FirstOrDefaultAsync(r => r.Id == enhancementRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found.");

        if (request.Status is not (EnhancementRequestStatus.Approved or EnhancementRequestStatus.ReadyForDevelopment))
        {
            throw new InvalidOperationException("Delivery can only start from Approved or Ready for development status.");
        }

        var active = await _dbContext.EnhancementDeliveryRuns
            .AnyAsync(
                r => r.EnhancementRequestId == enhancementRequestId
                    && r.Phase != DeliveryRunPhase.Completed
                    && r.Phase != DeliveryRunPhase.Failed,
                cancellationToken);
        if (active)
        {
            throw new InvalidOperationException("A delivery run is already in progress.");
        }

        var runNumber = await _dbContext.EnhancementDeliveryRuns
            .Where(r => r.EnhancementRequestId == enhancementRequestId)
            .Select(r => (int?)r.RunNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTime.UtcNow;
        var run = new EnhancementDeliveryRun
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = enhancementRequestId,
            RunNumber = runNumber + 1,
            Phase = DeliveryRunPhase.Pending,
            TimelineJson = DeliveryTimeline.AppendEvent(null, "Delivery run created."),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.EnhancementDeliveryRuns.Add(run);
        request.Status = EnhancementRequestStatus.Implementing;
        request.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            "DeliveryRunStarted",
            nameof(EnhancementDeliveryRun),
            run.Id,
            $"Started delivery run #{run.RunNumber} for request {enhancementRequestId}.",
            cancellationToken);

        return run.Id;
    }

    public async Task ProcessActiveRunsAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _dbContext.EnhancementDeliveryRuns
            .Include(r => r.EnhancementRequest)
            .ThenInclude(req => req!.TargetApplication)
            .Include(r => r.EnhancementRequest)
            .ThenInclude(req => req!.Analyses)
            .Where(r => r.Phase != DeliveryRunPhase.Completed && r.Phase != DeliveryRunPhase.Failed)
            .OrderBy(r => r.UpdatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var run in runs)
        {
            try
            {
                await ProcessRunAsync(run, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delivery run {RunId} failed", run.Id);
                run.Phase = DeliveryRunPhase.Failed;
                run.LastError = ex.Message;
                run.TimelineJson = DeliveryTimeline.AppendEvent(run.TimelineJson, $"Failed: {ex.Message}");
                run.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task SignUatAsync(
        Guid enhancementRequestId,
        Guid userId,
        bool approved,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var run = await GetActiveRunAsync(enhancementRequestId, cancellationToken);
        if (run.Phase != DeliveryRunPhase.AwaitingUat)
        {
            throw new InvalidOperationException("Request is not awaiting UAT.");
        }

        run.UatSignedOffByUserId = userId;
        run.UatSignedOffAt = DateTime.UtcNow;
        run.UatNotes = notes?.Trim();
        run.UatApproved = approved;
        run.UpdatedAt = DateTime.UtcNow;

        if (!approved)
        {
            run.Phase = DeliveryRunPhase.Failed;
            run.LastError = notes ?? "UAT rejected.";
            run.TimelineJson = DeliveryTimeline.AppendEvent(run.TimelineJson, "UAT rejected by requester.");
            run.EnhancementRequest.Status = EnhancementRequestStatus.NeedsClarification;
        }
        else
        {
            run.Phase = DeliveryRunPhase.UatApproved;
            run.TimelineJson = DeliveryTimeline.AppendEvent(run.TimelineJson, "UAT approved by requester.");
            run.EnhancementRequest.Status = EnhancementRequestStatus.UatApproved;
            await ScheduleProductionIfAllowedAsync(run, cancellationToken);
        }

        run.EnhancementRequest.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AdvancePastPullRequestReviewAsync(Guid enhancementRequestId, CancellationToken cancellationToken = default)
    {
        var run = await GetActiveRunAsync(enhancementRequestId, cancellationToken);
        if (run.Phase != DeliveryRunPhase.AwaitingPullRequestReview)
        {
            throw new InvalidOperationException("Delivery run is not awaiting pull request review.");
        }

        run.Phase = DeliveryRunPhase.DeployingToTest;
        run.TimelineJson = DeliveryTimeline.AppendEvent(run.TimelineJson, "Pull request approved — deploying to test.");
        run.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessRunAsync(EnhancementDeliveryRun run, CancellationToken cancellationToken)
    {
        switch (run.Phase)
        {
            case DeliveryRunPhase.Pending:
            case DeliveryRunPhase.Implementing:
                await ImplementAsync(run, cancellationToken);
                break;
            case DeliveryRunPhase.AwaitingPullRequestReview:
                break;
            case DeliveryRunPhase.DeployingToTest:
                await DeployToTestAsync(run, cancellationToken);
                break;
            case DeliveryRunPhase.RunningQa:
                await RunQaAsync(run, cancellationToken);
                break;
            case DeliveryRunPhase.AwaitingUat:
                break;
            case DeliveryRunPhase.UatApproved:
                await ScheduleProductionIfAllowedAsync(run, cancellationToken);
                break;
            case DeliveryRunPhase.ProdScheduled:
                await DeployToProductionAsync(run, cancellationToken);
                break;
            case DeliveryRunPhase.DeployingToProduction:
                await CompleteProductionDeployAsync(run, cancellationToken);
                break;
        }
    }

    private async Task ImplementAsync(EnhancementDeliveryRun run, CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(run.EnhancementRequest, cancellationToken);
        if (context.AppProfile is null || context.Repository is null)
        {
            throw new InvalidOperationException("Application delivery profile and repository are required.");
        }

        run.Phase = DeliveryRunPhase.Implementing;
        var slug = Slugify(run.EnhancementRequest.Title);
        var branchName = context.AppProfile.BranchNamingPattern
            .Replace("{requestId}", run.EnhancementRequestId.ToString("N")[..8], StringComparison.OrdinalIgnoreCase)
            .Replace("{slug}", slug, StringComparison.OrdinalIgnoreCase);

        var analysis = run.EnhancementRequest.Analyses.OrderByDescending(a => a.Version).FirstOrDefault();
        var implementationBody = BuildImplementationMarkdown(run.EnhancementRequest, analysis);

        await _testCaseCatalog.EnsureDraftCasesForRequestAsync(run.EnhancementRequestId, cancellationToken);

        var (owner, repo) = RepositoryCoordinates.Resolve(
            context.Repository.Url,
            context.Repository.Name,
            _demoOwner);

        var pr = await _gitHubRepository.CreateImplementationPullRequestAsync(
            owner,
            repo,
            context.Repository.DefaultBranch,
            branchName,
            $"Delivery: {run.EnhancementRequest.Title}",
            $"Automated implementation for EnhancementHub request {run.EnhancementRequestId}.",
            implementationBody,
            cancellationToken: cancellationToken);

        if (!pr.Succeeded)
        {
            throw new InvalidOperationException(pr.ErrorMessage ?? "Failed to create pull request.");
        }

        if (!string.IsNullOrWhiteSpace(pr.BranchName))
        {
            try
            {
                await _testCaseRepoExporter.ExportRequestCasesToBranchAsync(
                    run.EnhancementRequestId,
                    owner,
                    repo,
                    pr.BranchName,
                    cancellationToken);
                run.TimelineJson = DeliveryTimeline.AppendEvent(
                    run.TimelineJson,
                    "Playwright test specs exported to branch.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to export Playwright specs for request {RequestId}", run.EnhancementRequestId);
            }
        }

        run.BranchName = pr.BranchName;
        run.PullRequestUrl = pr.PullRequestUrl;
        run.PullRequestNumber = pr.PullRequestNumber;
        run.CommitSha = pr.CommitSha;
        run.IsSimulation = pr.IsSimulation;
        run.TimelineJson = DeliveryTimeline.AppendEvent(
            run.TimelineJson,
            $"Pull request created: {pr.PullRequestUrl}");

        if (context.TenantProfile?.RequirePullRequestReview == true && !pr.IsSimulation)
        {
            run.Phase = DeliveryRunPhase.AwaitingPullRequestReview;
            run.EnhancementRequest.Status = EnhancementRequestStatus.Implementing;
        }
        else
        {
            run.Phase = DeliveryRunPhase.DeployingToTest;
            run.TimelineJson = DeliveryTimeline.AppendEvent(run.TimelineJson, "Proceeding to test deployment.");
        }

        run.UpdatedAt = DateTime.UtcNow;
        run.EnhancementRequest.Status = DeliveryStatusMapper.MapPhaseToRequestStatus(run.Phase);
        run.EnhancementRequest.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeployToTestAsync(EnhancementDeliveryRun run, CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(run.EnhancementRequest, cancellationToken);
        if (context.AppProfile is null || context.TestEnvironment is null)
        {
            throw new InvalidOperationException("Test environment and application profile are required.");
        }

        var bundle = await _configBundleBuilder.BuildAsync(context.AppProfile, context.TestEnvironment, cancellationToken);
        var (owner, repo) = RepositoryCoordinates.Resolve(
            context.Repository?.Url,
            context.Repository?.Name ?? "app",
            _demoOwner);

        var deployContext = new DeploymentContext(
            run.EnhancementRequestId,
            context.AppProfile.ApplicationId,
            run.Id,
            DeploymentEnvironmentType.Test,
            context.AppProfile.CicdProviderOverride ?? context.TenantProfile?.DefaultCicdProvider ?? CicdProvider.GitHubActions,
            context.AppProfile.CicdPipelineReference,
            context.AppProfile.DeploymentMechanism,
            bundle,
            owner,
            repo,
            context.Repository?.DefaultBranch);

        var adapter = _adapterFactory.Resolve(deployContext);
        var result = await adapter.DeployAsync(deployContext, cancellationToken);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Test deployment failed.");
        }

        run.TestDeployReference = result.DeploymentReference;
        run.TestUrl = result.DeployedUrl ?? bundle.BaseUrl;
        run.Phase = DeliveryRunPhase.RunningQa;
        run.TimelineJson = DeliveryTimeline.AppendEvent(
            run.TimelineJson,
            $"Deployed to test: {run.TestUrl}");
        run.EnhancementRequest.Status = EnhancementRequestStatus.QaInProgress;
        run.UpdatedAt = DateTime.UtcNow;
        run.EnhancementRequest.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RunQaAsync(EnhancementDeliveryRun run, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(run.TestUrl))
        {
            throw new InvalidOperationException("Test URL is missing.");
        }

        run.QaStartedAt = DateTime.UtcNow;
        run.QaRunner = _qaRunner.RunnerKind;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var manifest = await _testCaseCatalog.PrepareQaRunAsync(run, cancellationToken);
        var qa = await _qaRunner.RunAsync(manifest, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var caseResult in qa.CaseResults)
        {
            _dbContext.DeliveryRunTestResults.Add(new DeliveryRunTestResult
            {
                Id = Guid.NewGuid(),
                EnhancementDeliveryRunId = run.Id,
                TestCaseId = caseResult.TestCaseId,
                TestCaseVersionId = caseResult.TestCaseVersionId,
                TestCaseTitle = caseResult.Title,
                IsRegressionCase = caseResult.IsRegressionCase,
                Passed = caseResult.Passed,
                DurationMs = caseResult.DurationMs,
                Detail = caseResult.Detail,
                ScreenshotStoragePath = caseResult.ScreenshotStoragePath,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        run.QaPassed = qa.Passed;
        run.QaStepsJson = JsonSerializer.Serialize(qa.Steps);
        run.QaVideoStoragePath = qa.VideoStoragePath;
        run.QaReportStoragePath = qa.ReportStoragePath;
        run.QaFinishedAt = now;
        run.TimelineJson = DeliveryTimeline.AppendEvent(
            run.TimelineJson,
            qa.Passed
                ? $"QA passed ({qa.CaseResults.Count} cases, {qa.CaseResults.Count(c => c.IsRegressionCase)} regression)."
                : "QA failed.");

        if (!qa.Passed)
        {
            run.Phase = DeliveryRunPhase.Failed;
            run.LastError = "Automated QA failed.";
            run.EnhancementRequest.Status = EnhancementRequestStatus.InProgress;
        }
        else
        {
            await _testCaseCatalog.PromotePassedCasesToRegressionAsync(
                run.EnhancementRequestId,
                run.Id,
                cancellationToken);
            run.Phase = DeliveryRunPhase.AwaitingUat;
            run.EnhancementRequest.Status = EnhancementRequestStatus.AwaitingUat;
        }

        run.UpdatedAt = now;
        run.EnhancementRequest.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ScheduleProductionIfAllowedAsync(EnhancementDeliveryRun run, CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(run.EnhancementRequest, cancellationToken);
        var requireWindow = context.TenantProfile?.RequireProdChangeWindow ?? true;
        var notes = context.TenantProfile?.ChangeWindowNotes;

        if (_changeWindowEvaluator.IsProductionDeployAllowed(DateTime.UtcNow, notes, requireWindow))
        {
            run.Phase = DeliveryRunPhase.DeployingToProduction;
            run.TimelineJson = DeliveryTimeline.AppendEvent(run.TimelineJson, "Production deploy started.");
            run.EnhancementRequest.Status = EnhancementRequestStatus.DeployingToProduction;
        }
        else
        {
            run.Phase = DeliveryRunPhase.ProdScheduled;
            run.ProdScheduledAt = NextSundayWindowUtc(DateTime.UtcNow);
            run.TimelineJson = DeliveryTimeline.AppendEvent(
                run.TimelineJson,
                $"Production deploy scheduled for {run.ProdScheduledAt:u}.");
            run.EnhancementRequest.Status = EnhancementRequestStatus.ProdScheduled;
        }

        run.UpdatedAt = DateTime.UtcNow;
        run.EnhancementRequest.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeployToProductionAsync(EnhancementDeliveryRun run, CancellationToken cancellationToken)
    {
        if (run.Phase == DeliveryRunPhase.ProdScheduled
            && run.ProdScheduledAt.HasValue
            && run.ProdScheduledAt.Value > DateTime.UtcNow)
        {
            return;
        }

        var context = await LoadContextAsync(run.EnhancementRequest, cancellationToken);
        if (context.TenantProfile?.RequireUatSignoff == true && !run.UatApproved)
        {
            return;
        }

        if (context.AppProfile is null || context.ProdEnvironment is null)
        {
            throw new InvalidOperationException("Production environment and application profile are required.");
        }

        var bundle = await _configBundleBuilder.BuildAsync(context.AppProfile, context.ProdEnvironment, cancellationToken);
        var (owner, repo) = RepositoryCoordinates.Resolve(
            context.Repository?.Url,
            context.Repository?.Name ?? "app",
            _demoOwner);

        var deployContext = new DeploymentContext(
            run.EnhancementRequestId,
            context.AppProfile.ApplicationId,
            run.Id,
            DeploymentEnvironmentType.Production,
            context.AppProfile.CicdProviderOverride ?? context.TenantProfile?.DefaultCicdProvider ?? CicdProvider.GitHubActions,
            context.AppProfile.CicdPipelineReference,
            context.AppProfile.DeploymentMechanism,
            bundle,
            owner,
            repo,
            context.Repository?.DefaultBranch);

        var adapter = _adapterFactory.Resolve(deployContext);
        var result = await adapter.DeployAsync(deployContext, cancellationToken);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Production deployment failed.");
        }

        run.ProdDeployReference = result.DeploymentReference;
        run.ProdDeployedAt = DateTime.UtcNow;
        run.Phase = DeliveryRunPhase.Completed;
        run.TimelineJson = DeliveryTimeline.AppendEvent(run.TimelineJson, "Production deployment completed.");
        run.EnhancementRequest.Status = EnhancementRequestStatus.Completed;
        run.UpdatedAt = DateTime.UtcNow;
        run.EnhancementRequest.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task CompleteProductionDeployAsync(EnhancementDeliveryRun run, CancellationToken cancellationToken) =>
        DeployToProductionAsync(run, cancellationToken);

    private async Task<EnhancementDeliveryRun> GetActiveRunAsync(
        Guid enhancementRequestId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EnhancementDeliveryRuns
            .Include(r => r.EnhancementRequest)
            .Where(r => r.EnhancementRequestId == enhancementRequestId)
            .OrderByDescending(r => r.RunNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No delivery run found.");
    }

    private async Task<DeliveryContextBundle> LoadContextAsync(
        EnhancementRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.TargetApplicationId.HasValue)
        {
            throw new InvalidOperationException("Target application is required for delivery.");
        }

        var appId = request.TargetApplicationId.Value;
        var appProfile = await _dbContext.ApplicationDeliveryProfiles
            .FirstOrDefaultAsync(p => p.ApplicationId == appId, cancellationToken);

        Repository? repository = null;
        if (appProfile?.PrimaryRepositoryId is Guid repoId)
        {
            repository = await _dbContext.Repositories.FirstOrDefaultAsync(r => r.Id == repoId, cancellationToken);
        }
        else
        {
            repository = await _dbContext.Repositories
                .Where(r => r.ApplicationId == appId)
                .OrderBy(r => r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var application = await _dbContext.Applications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appId, cancellationToken);

        var team = request.TeamId.HasValue
            ? await _dbContext.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken)
            : null;

        Guid? tenantId = team?.TenantId;
        if (!tenantId.HasValue && application is not null)
        {
            var ownerTeam = await _dbContext.Teams.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == application.OwnerTeamId, cancellationToken);
            tenantId = ownerTeam?.TenantId;
        }

        TenantDeliveryProfile? tenantProfile = null;
        IReadOnlyList<TenantDeploymentEnvironment> environments = [];
        if (tenantId.HasValue)
        {
            tenantProfile = await _dbContext.TenantDeliveryProfiles
                .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value, cancellationToken);
            environments = await _dbContext.TenantDeploymentEnvironments
                .Where(e => e.TenantId == tenantId.Value && e.IsActive)
                .ToListAsync(cancellationToken);
        }

        return new DeliveryContextBundle(
            appProfile,
            repository,
            tenantProfile,
            environments.FirstOrDefault(e => e.EnvironmentType == DeploymentEnvironmentType.Test),
            environments.FirstOrDefault(e => e.EnvironmentType == DeploymentEnvironmentType.Production));
    }

    private static string BuildImplementationMarkdown(EnhancementRequest request, EnhancementAnalysis? analysis)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Implementation: {request.Title}");
        sb.AppendLine();
        sb.AppendLine("## Business context");
        sb.AppendLine(request.BusinessDescription);
        sb.AppendLine();
        sb.AppendLine("## Desired outcome");
        sb.AppendLine(request.DesiredOutcome);
        if (analysis is not null)
        {
            sb.AppendLine();
            sb.AppendLine("## Technical plan");
            sb.AppendLine(analysis.TechnicalRequirements ?? analysis.FeatureSummary ?? "See analysis in EnhancementHub.");
            sb.AppendLine();
            sb.AppendLine("## Testing plan");
            sb.AppendLine(analysis.TestingPlan ?? "Run regression and integration tests.");
        }

        return sb.ToString();
    }

    private static string Slugify(string value)
    {
        var chars = value.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-')
            .ToArray();
        var result = new string(chars).Replace(' ', '-').Trim('-');
        if (string.IsNullOrWhiteSpace(result))
        {
            return "change";
        }

        return result.Length > 40 ? result[..40].Trim('-') : result;
    }

    private static DateTime NextSundayWindowUtc(DateTime utcNow)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)utcNow.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0 && utcNow.Hour >= 6)
        {
            daysUntilSunday = 7;
        }

        var target = utcNow.Date.AddDays(daysUntilSunday).AddHours(2);
        return DateTime.SpecifyKind(target, DateTimeKind.Utc);
    }

    private sealed record DeliveryContextBundle(
        ApplicationDeliveryProfile? AppProfile,
        Repository? Repository,
        TenantDeliveryProfile? TenantProfile,
        TenantDeploymentEnvironment? TestEnvironment,
        TenantDeploymentEnvironment? ProdEnvironment);
}
