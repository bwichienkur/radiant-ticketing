using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services.Delivery;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Tests.Unit;

public sealed class TestCaseCatalogTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EnhancementHubDbContext _dbContext;

    public TestCaseCatalogTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<EnhancementHubDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new EnhancementHubDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void ParseTestingPlan_SplitsMultilinePlanIntoCases()
    {
        var cases = TestCasePlanParser.ParseTestingPlan(
            "- Unit tests for validation\n- Integration tests for cancel API\n- UAT with Finance",
            "Managers can cancel orders",
            "Cancel flow");

        cases.Should().HaveCount(3);
        cases[0].Title.Should().Contain("Unit tests");
        cases[0].Steps.Should().ContainSingle();
    }

    [Fact]
    public async Task PrepareQaRun_CreatesDraftCasesAndRegressionManifest()
    {
        var (appId, requestId, runId) = await SeedAsync();

        var catalog = new TestCaseCatalogService(_dbContext);
        var run = await _dbContext.EnhancementDeliveryRuns.FindAsync(runId);
        run!.TestUrl = "https://demo-test.example.com";

        var manifest = await catalog.PrepareQaRunAsync(run);

        manifest.Cases.Should().NotBeEmpty();
        manifest.ApplicationId.Should().Be(appId);

        var suite = await _dbContext.ApplicationTestSuites
            .Include(s => s.TestCases)
            .FirstAsync(s => s.ApplicationId == appId);
        suite.TestCases.Should().Contain(c => c.SourceEnhancementRequestId == requestId);
        suite.TestCases.Where(c => c.SourceEnhancementRequestId == requestId)
            .Should()
            .OnlyContain(c => c.Status == TestCaseStatus.Draft);
    }

    [Fact]
    public async Task PromotePassedCasesToRegression_ActivatesDraftCases()
    {
        var (_, requestId, runId) = await SeedAsync();
        var catalog = new TestCaseCatalogService(_dbContext);
        var run = await _dbContext.EnhancementDeliveryRuns.FindAsync(runId);
        run!.TestUrl = "https://demo-test.example.com";
        var manifest = await catalog.PrepareQaRunAsync(run);
        var draftCase = manifest.Cases.First(c => !c.IsRegressionCase);

        _dbContext.DeliveryRunTestResults.Add(new DeliveryRunTestResult
        {
            Id = Guid.NewGuid(),
            EnhancementDeliveryRunId = runId,
            TestCaseId = draftCase.TestCaseId,
            TestCaseVersionId = draftCase.TestCaseVersionId,
            TestCaseTitle = draftCase.Title,
            IsRegressionCase = false,
            Passed = true,
            DurationMs = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await catalog.PromotePassedCasesToRegressionAsync(requestId, runId);

        var promoted = await _dbContext.TestCases.FindAsync(draftCase.TestCaseId);
        promoted!.Status.Should().Be(TestCaseStatus.Active);
        promoted.Origin.Should().Be(TestCaseOrigin.Promoted);
    }

    private async Task<(Guid AppId, Guid RequestId, Guid RunId)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t", CreatedAt = now, UpdatedAt = now });
        _dbContext.Teams.Add(new Team { Id = teamId, Name = "Team", TenantId = tenantId, CreatedAt = now, UpdatedAt = now });
        _dbContext.Users.Add(new User
        {
            Id = userId,
            Email = "user@test.local",
            DisplayName = "User",
            PasswordHash = "x",
            TenantId = tenantId,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.Applications.Add(new Domain.Entities.Application
        {
            Id = appId,
            Name = "App",
            OwnerTeamId = teamId,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.EnhancementRequests.Add(new EnhancementRequest
        {
            Id = requestId,
            Title = "Feature",
            BusinessDescription = "Desc",
            DesiredOutcome = "Outcome",
            Priority = "Medium",
            SubmittedByUserId = userId,
            TargetApplicationId = appId,
            TeamId = teamId,
            Status = EnhancementRequestStatus.Approved,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.EnhancementAnalyses.Add(new EnhancementAnalysis
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = requestId,
            Version = 1,
            FeatureSummary = "Summary",
            TestingPlan = "Verify export\nValidate totals",
            ConfidenceScore = 0.9,
            RiskLevel = RiskLevel.Low,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.EnhancementDeliveryRuns.Add(new EnhancementDeliveryRun
        {
            Id = runId,
            EnhancementRequestId = requestId,
            RunNumber = 1,
            Phase = DeliveryRunPhase.RunningQa,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await _dbContext.SaveChangesAsync();
        return (appId, requestId, runId);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
