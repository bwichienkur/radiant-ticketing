using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Infrastructure.Services.Delivery;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EnhancementHub.Tests.Unit;

public sealed class DeliveryOrchestrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EnhancementHubDbContext _dbContext;

    public DeliveryOrchestrationTests()
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
    public async Task StartDeliveryRun_CreatesRunAndSetsImplementingStatus()
    {
        var (requestId, _) = await SeedDeliveryContextAsync();

        var orchestration = CreateOrchestrationService();
        var runId = await orchestration.StartDeliveryRunAsync(requestId);

        var run = await _dbContext.EnhancementDeliveryRuns.FindAsync(runId);
        var request = await _dbContext.EnhancementRequests.FindAsync(requestId);

        run.Should().NotBeNull();
        run!.Phase.Should().Be(DeliveryRunPhase.Pending);
        request!.Status.Should().Be(EnhancementRequestStatus.Implementing);
    }

    [Fact]
    public async Task ProcessActiveRuns_CompletesSimulatedPipeline()
    {
        var (requestId, _) = await SeedDeliveryContextAsync();

        var orchestration = CreateOrchestrationService();
        await orchestration.StartDeliveryRunAsync(requestId);

        for (var i = 0; i < 6; i++)
        {
            await orchestration.ProcessActiveRunsAsync();
        }

        var run = await _dbContext.EnhancementDeliveryRuns
            .OrderByDescending(r => r.RunNumber)
            .FirstAsync(r => r.EnhancementRequestId == requestId);

        run.Phase.Should().Be(DeliveryRunPhase.AwaitingUat);
        run.PullRequestUrl.Should().NotBeNullOrWhiteSpace();
        run.TestUrl.Should().NotBeNullOrWhiteSpace();
        run.QaPassed.Should().BeTrue();
        run.QaStepsJson.Should().Contain("Open test environment");

        var caseResults = await _dbContext.DeliveryRunTestResults
            .Where(r => r.EnhancementDeliveryRunId == run.Id)
            .ToListAsync();
        caseResults.Should().NotBeEmpty();
        caseResults.Should().OnlyContain(r => r.Passed);
    }

    [Fact]
    public async Task SignUat_Approved_SchedulesOrDeploysProduction()
    {
        var (requestId, userId) = await SeedDeliveryContextAsync();

        var orchestration = CreateOrchestrationService();
        await orchestration.StartDeliveryRunAsync(requestId);

        for (var i = 0; i < 6; i++)
        {
            await orchestration.ProcessActiveRunsAsync();
        }

        await orchestration.SignUatAsync(requestId, userId, approved: true, notes: "Looks good");

        var run = await _dbContext.EnhancementDeliveryRuns
            .OrderByDescending(r => r.RunNumber)
            .FirstAsync(r => r.EnhancementRequestId == requestId);

        run.UatApproved.Should().BeTrue();
        run.Phase.Should().BeOneOf(DeliveryRunPhase.ProdScheduled, DeliveryRunPhase.DeployingToProduction, DeliveryRunPhase.Completed);
    }

    [Fact]
    public void SpaDeliveryController_ExposesRunAndUatEndpoints()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Controllers/Spa/SpaDeliveryController.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("requests/{requestId:guid}/start");
        content.Should().Contain("requests/{requestId:guid}/uat");
        content.Should().Contain("applications/{applicationId:guid}/test-suite");
        content.Should().Contain("GetDeliveryRunQuery");
    }

    private DeliveryOrchestrationService CreateOrchestrationService()
    {
        var gitHubRepo = new Mock<IGitHubAppRepositoryService>();
        gitHubRepo.Setup(x => x.IsConfigured).Returns(false);
        gitHubRepo.Setup(x => x.CreateImplementationPullRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubPullRequestResult(true, "eh/demo-branch", "https://github.com/demo/app/pull/42", 42, "abc", true, null));

        var gitHubClone = new Mock<IGitHubAppCloneService>();
        gitHubClone.Setup(x => x.IsConfigured).Returns(false);

        var configBuilder = new DeploymentConfigBundleBuilder();
        var adapters = new IDeploymentAdapter[]
        {
            new GitHubActionsDeploymentAdapter(
                Mock.Of<IHttpClientFactory>(),
                gitHubClone.Object,
                new ConfigurationBuilder().Build(),
                NullLogger<GitHubActionsDeploymentAdapter>.Instance),
            new WebhookDeploymentAdapter(
                Mock.Of<IHttpClientFactory>(),
                new ConfigurationBuilder().Build(),
                NullLogger<WebhookDeploymentAdapter>.Instance),
        };
        var adapterFactory = new DeploymentAdapterFactory(adapters);
        var fileStorage = new LocalFileStorageService(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Local",
                ["Storage:LocalRoot"] = Path.Combine(Path.GetTempPath(), "eh-delivery-tests")
            }).Build());

        var testCaseCatalog = new TestCaseCatalogService(_dbContext);
        var qaRunner = new SimulatedQaRunner(fileStorage);
        var changeWindow = new ChangeWindowEvaluator();
        var audit = new AuditService(_dbContext, Mock.Of<ICurrentUserService>());
        var configuration = new ConfigurationBuilder().Build();

        return new DeliveryOrchestrationService(
            _dbContext,
            gitHubRepo.Object,
            configBuilder,
            adapterFactory,
            testCaseCatalog,
            qaRunner,
            changeWindow,
            audit,
            configuration,
            NullLogger<DeliveryOrchestrationService>.Instance);
    }

    private async Task<(Guid RequestId, Guid UserId)> SeedDeliveryContextAsync()
    {
        var tenantId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var repoId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t", CreatedAt = now, UpdatedAt = now });
        _dbContext.Teams.Add(new Team { Id = teamId, Name = "Team", TenantId = tenantId, CreatedAt = now, UpdatedAt = now });
        _dbContext.Users.Add(new User
        {
            Id = userId,
            Email = "user@test.local",
            DisplayName = "User",
            PasswordHash = "x",
            CreatedAt = now,
            UpdatedAt = now,
            TenantId = tenantId,
        });
        _dbContext.Applications.Add(new Domain.Entities.Application
        {
            Id = appId,
            Name = "Demo App",
            OwnerTeamId = teamId,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.Repositories.Add(new Repository
        {
            Id = repoId,
            ApplicationId = appId,
            Name = "demo-app",
            Url = "https://github.com/demo/demo-app",
            Provider = ExternalTicketProvider.GitHub,
            DefaultBranch = "main",
            IndexingStatus = IndexingStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.TenantDeliveryProfiles.Add(new TenantDeliveryProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AutoImplementOnApprove = true,
            RequirePullRequestReview = false,
            RequireUatSignoff = true,
            RequireProdChangeWindow = false,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.TenantDeploymentEnvironments.AddRange(
            new TenantDeploymentEnvironment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Test",
                EnvironmentType = DeploymentEnvironmentType.Test,
                BaseUrlTemplate = "https://demo-test.example.com",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new TenantDeploymentEnvironment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Production",
                EnvironmentType = DeploymentEnvironmentType.Production,
                BaseUrlTemplate = "https://demo.example.com",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        _dbContext.ApplicationDeliveryProfiles.Add(new ApplicationDeliveryProfile
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            PrimaryRepositoryId = repoId,
            CicdPipelineReference = ".github/workflows/deploy.yml",
            BranchNamingPattern = "eh/{requestId}-{slug}",
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.EnhancementRequests.Add(new EnhancementRequest
        {
            Id = requestId,
            Title = "Add reporting",
            BusinessDescription = "Need reporting",
            DesiredOutcome = "Managers can run reports",
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
            FeatureSummary = "Add report endpoint",
            TestingPlan = "Verify report export",
            ConfidenceScore = 0.9,
            RiskLevel = RiskLevel.Low,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await _dbContext.SaveChangesAsync();
        return (requestId, userId);
    }

    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "EnhancementHub.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new InvalidOperationException("Repo root not found");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
