using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class NightlyRegressionService : INightlyRegressionService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ITestCaseCatalogService _catalog;
    private readonly IQaRunner _qaRunner;
    private readonly ILogger<NightlyRegressionService> _logger;

    public NightlyRegressionService(
        IEnhancementHubDbContext dbContext,
        ITestCaseCatalogService catalog,
        IQaRunner qaRunner,
        ILogger<NightlyRegressionService> logger)
    {
        _dbContext = dbContext;
        _catalog = catalog;
        _qaRunner = qaRunner;
        _logger = logger;
    }

    public async Task RunScheduledRegressionAsync(CancellationToken cancellationToken = default)
    {
        var applicationIds = await _dbContext.TestCases
            .AsNoTracking()
            .Where(c => c.Status == TestCaseStatus.Active)
            .Join(
                _dbContext.ApplicationTestSuites.AsNoTracking(),
                c => c.TestSuiteId,
                s => s.Id,
                (c, s) => s.ApplicationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var applicationId in applicationIds)
        {
            try
            {
                await RunForApplicationAsync(applicationId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nightly regression failed for application {ApplicationId}", applicationId);
            }
        }
    }

    private async Task RunForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var testUrl = await ResolveTestUrlAsync(applicationId, cancellationToken);
        if (string.IsNullOrWhiteSpace(testUrl))
        {
            _logger.LogDebug("Skipping nightly regression for {ApplicationId}: no test URL", applicationId);
            return;
        }

        var manifest = await _catalog.PrepareRegressionManifestAsync(applicationId, testUrl, cancellationToken);
        if (manifest.Cases.Count == 0)
        {
            return;
        }

        var result = await _qaRunner.RunAsync(manifest, cancellationToken);
        var now = DateTime.UtcNow;
        var run = new ApplicationRegressionRun
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            TestUrl = testUrl,
            Passed = result.Passed,
            QaRunner = result.Runner,
            IsSimulation = result.IsSimulation,
            CaseCount = result.CaseResults.Count,
            PassedCaseCount = result.CaseResults.Count(c => c.Passed),
            ReportStoragePath = result.ReportStoragePath,
            ResultsJson = JsonSerializer.Serialize(result.CaseResults.Select(c => new
            {
                c.TestCaseId,
                c.Title,
                c.Passed,
                c.DurationMs,
                c.Detail
            })),
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.ApplicationRegressionRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Nightly regression for application {ApplicationId}: {Passed}/{Total} cases passed",
            applicationId,
            run.PassedCaseCount,
            run.CaseCount);
    }

    private async Task<string?> ResolveTestUrlAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        return await (
            from app in _dbContext.Applications.AsNoTracking()
            join team in _dbContext.Teams.AsNoTracking() on app.OwnerTeamId equals team.Id
            join env in _dbContext.TenantDeploymentEnvironments.AsNoTracking() on team.TenantId equals env.TenantId
            where app.Id == applicationId
                && env.IsActive
                && env.EnvironmentType == DeploymentEnvironmentType.Test
            orderby env.SortOrder
            select env.BaseUrlTemplate).FirstOrDefaultAsync(cancellationToken);
    }
}
