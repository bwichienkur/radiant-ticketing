using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services.Delivery;

public sealed class TestCaseCatalogService : ITestCaseCatalogService
{
    private readonly IEnhancementHubDbContext _dbContext;

    public TestCaseCatalogService(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<QaRunManifest> PrepareQaRunAsync(
        EnhancementDeliveryRun run,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.EnhancementRequests
            .Include(r => r.Analyses)
            .FirstAsync(r => r.Id == run.EnhancementRequestId, cancellationToken);

        if (!request.TargetApplicationId.HasValue)
        {
            throw new InvalidOperationException("Request must target an application for QA.");
        }

        var applicationId = request.TargetApplicationId.Value;
        var suite = await EnsureRegressionSuiteAsync(applicationId, cancellationToken);
        var analysis = request.Analyses.OrderByDescending(a => a.Version).FirstOrDefault();
        await SyncDraftCasesFromAnalysisAsync(suite, request, analysis, cancellationToken);

        var casesToRun = await _dbContext.TestCases
            .Where(c => c.TestSuiteId == suite.Id
                && (c.Status == TestCaseStatus.Active
                    || (c.Status == TestCaseStatus.Draft && c.SourceEnhancementRequestId == request.Id)))
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Title)
            .ToListAsync(cancellationToken);

        var manifestCases = new List<QaManifestCase>();
        foreach (var testCase in casesToRun)
        {
            var version = await SnapshotVersionAsync(testCase, cancellationToken);
            var steps = TestCasePlanParser.DeserializeSteps(version.StepsJson);
            manifestCases.Add(new QaManifestCase(
                testCase.Id,
                version.Id,
                version.Title,
                testCase.Status == TestCaseStatus.Active,
                steps));
        }

        if (manifestCases.Count == 0)
        {
            var fallback = await EnsureFallbackCaseAsync(suite, request, analysis, cancellationToken);
            var version = await SnapshotVersionAsync(fallback, cancellationToken);
            manifestCases.Add(new QaManifestCase(
                fallback.Id,
                version.Id,
                version.Title,
                false,
                TestCasePlanParser.DeserializeSteps(version.StepsJson)));
        }

        return new QaRunManifest(
            request.Id,
            run.Id,
            applicationId,
            run.TestUrl ?? string.Empty,
            request.DesiredOutcome,
            manifestCases);
    }

    public async Task PromotePassedCasesToRegressionAsync(
        Guid enhancementRequestId,
        Guid deliveryRunId,
        CancellationToken cancellationToken = default)
    {
        var passedDraftIds = await _dbContext.DeliveryRunTestResults
            .Where(r => r.EnhancementDeliveryRunId == deliveryRunId
                && r.Passed
                && !r.IsRegressionCase)
            .Select(r => r.TestCaseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (passedDraftIds.Count == 0)
        {
            return;
        }

        var cases = await _dbContext.TestCases
            .Where(c => passedDraftIds.Contains(c.Id)
                && c.SourceEnhancementRequestId == enhancementRequestId
                && c.Status == TestCaseStatus.Draft)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var testCase in cases)
        {
            testCase.Status = TestCaseStatus.Active;
            testCase.Origin = TestCaseOrigin.Promoted;
            testCase.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationTestSuite> EnsureRegressionSuiteAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var suite = await _dbContext.ApplicationTestSuites
            .FirstOrDefaultAsync(
                s => s.ApplicationId == applicationId && s.IsDefaultRegression,
                cancellationToken);

        if (suite is not null)
        {
            return suite;
        }

        var now = DateTime.UtcNow;
        suite = new ApplicationTestSuite
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Name = "Regression",
            Description = "Default regression suite for automated delivery QA.",
            IsDefaultRegression = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _dbContext.ApplicationTestSuites.Add(suite);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return suite;
    }

    private async Task SyncDraftCasesFromAnalysisAsync(
        ApplicationTestSuite suite,
        EnhancementRequest request,
        EnhancementAnalysis? analysis,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.TestCases
            .AnyAsync(
                c => c.TestSuiteId == suite.Id && c.SourceEnhancementRequestId == request.Id,
                cancellationToken);
        if (existing)
        {
            return;
        }

        var definitions = TestCasePlanParser.ParseTestingPlan(
            analysis?.TestingPlan,
            request.DesiredOutcome,
            request.Title);

        if (definitions.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var order = 0;
        foreach (var definition in definitions)
        {
            var stepsJson = JsonSerializer.Serialize(definition.Steps);
            var testCase = new TestCase
            {
                Id = Guid.NewGuid(),
                TestSuiteId = suite.Id,
                Title = definition.Title,
                Description = definition.Description,
                Status = TestCaseStatus.Draft,
                Origin = TestCaseOrigin.AiGenerated,
                SourceEnhancementRequestId = request.Id,
                SourceEnhancementAnalysisId = analysis?.Id,
                StepsJson = stepsJson,
                SortOrder = order++,
                CurrentVersion = 1,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _dbContext.TestCases.Add(testCase);
            _dbContext.TestCaseVersions.Add(new TestCaseVersion
            {
                Id = Guid.NewGuid(),
                TestCaseId = testCase.Id,
                Version = 1,
                Title = testCase.Title,
                StepsJson = stepsJson,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TestCase> EnsureFallbackCaseAsync(
        ApplicationTestSuite suite,
        EnhancementRequest request,
        EnhancementAnalysis? analysis,
        CancellationToken cancellationToken)
    {
        var steps = TestCasePlanParser.BuildSmokeSteps(request.DesiredOutcome, analysis?.TestingPlan);
        var stepsJson = JsonSerializer.Serialize(steps);
        var now = DateTime.UtcNow;
        var testCase = new TestCase
        {
            Id = Guid.NewGuid(),
            TestSuiteId = suite.Id,
            Title = $"Smoke: {request.Title}",
            Status = TestCaseStatus.Draft,
            Origin = TestCaseOrigin.AiGenerated,
            SourceEnhancementRequestId = request.Id,
            SourceEnhancementAnalysisId = analysis?.Id,
            StepsJson = stepsJson,
            CurrentVersion = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _dbContext.TestCases.Add(testCase);
        _dbContext.TestCaseVersions.Add(new TestCaseVersion
        {
            Id = Guid.NewGuid(),
            TestCaseId = testCase.Id,
            Version = 1,
            Title = testCase.Title,
            StepsJson = stepsJson,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return testCase;
    }

    private async Task<TestCaseVersion> SnapshotVersionAsync(TestCase testCase, CancellationToken cancellationToken)
    {
        var latest = await _dbContext.TestCaseVersions
            .Where(v => v.TestCaseId == testCase.Id)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is not null && latest.Version == testCase.CurrentVersion)
        {
            return latest;
        }

        var nextVersion = (latest?.Version ?? testCase.CurrentVersion) + 1;
        var now = DateTime.UtcNow;
        var version = new TestCaseVersion
        {
            Id = Guid.NewGuid(),
            TestCaseId = testCase.Id,
            Version = nextVersion,
            Title = testCase.Title,
            StepsJson = testCase.StepsJson,
            CreatedAt = now,
            UpdatedAt = now,
        };
        testCase.CurrentVersion = nextVersion;
        testCase.UpdatedAt = now;
        _dbContext.TestCaseVersions.Add(version);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return version;
    }
}
