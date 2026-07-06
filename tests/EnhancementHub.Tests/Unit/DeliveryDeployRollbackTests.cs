using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services.Delivery;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EnhancementHub.Tests.Unit;

public sealed class DeliveryDeployRollbackTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EnhancementHubDbContext _dbContext;

    public DeliveryDeployRollbackTests()
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
    public async Task TriggerProductionDeploy_CompletesWithArtifactPromotion()
    {
        var (requestId, _) = await SeedAsync(requiresHumanProdDeploy: true);
        var orchestration = CreateOrchestrationService();
        await orchestration.StartDeliveryRunAsync(requestId);

        for (var i = 0; i < 6; i++)
        {
            await orchestration.ProcessActiveRunsAsync();
        }

        await orchestration.SignUatAsync(requestId, Guid.NewGuid(), true, "ok");
        await orchestration.TriggerProductionDeployAsync(requestId);

        var run = await _dbContext.EnhancementDeliveryRuns
            .OrderByDescending(r => r.RunNumber)
            .FirstAsync(r => r.EnhancementRequestId == requestId);

        run.Phase.Should().Be(DeliveryRunPhase.Completed);
        run.ProdArtifactReference.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SpaDeliveryController_ExposesDeployAndRollbackEndpoints()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Controllers/Spa/SpaDeliveryController.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("deploy-production");
        content.Should().Contain("rollback-production");
        content.Should().Contain("DeployProductionCommand");
        content.Should().Contain("RollbackProductionCommand");
    }

    private DeliveryOrchestrationService CreateOrchestrationService()
    {
        var gitHubRepo = new Mock<EnhancementHub.Application.Abstractions.IGitHubAppRepositoryService>();
        gitHubRepo.Setup(x => x.IsConfigured).Returns(false);
        gitHubRepo.Setup(x => x.CreateImplementationPullRequestAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnhancementHub.Application.Abstractions.GitHubPullRequestResult(
                true, "eh/branch", "https://github.com/demo/app/pull/1", 1, "commit123", true, null));
        gitHubRepo.Setup(x => x.UpsertBranchFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnhancementHub.Application.Abstractions.GitHubFileUpsertResult(true, "sha", true, null));

        var gitHubClone = new Mock<EnhancementHub.Application.Abstractions.IGitHubAppCloneService>();
        gitHubClone.Setup(x => x.IsConfigured).Returns(false);

        var configBuilder = new DeploymentConfigBundleBuilder();
        var adapters = new EnhancementHub.Application.Abstractions.IDeploymentAdapter[]
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
        var fileStorage = new EnhancementHub.Infrastructure.Services.LocalFileStorageService(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Local",
                ["Storage:LocalRoot"] = Path.Combine(Path.GetTempPath(), "eh-deploy-rollback-tests")
            }).Build());

        return new DeliveryOrchestrationService(
            _dbContext,
            gitHubRepo.Object,
            configBuilder,
            adapterFactory,
            new TestCaseCatalogService(_dbContext),
            new TestCaseRepoExporter(new TestCaseCatalogService(_dbContext), gitHubRepo.Object, NullLogger<TestCaseRepoExporter>.Instance),
            new SimulatedQaRunner(fileStorage),
            new ChangeWindowEvaluator(),
            new EnhancementHub.Infrastructure.Services.AuditService(_dbContext, Mock.Of<EnhancementHub.Application.Abstractions.ICurrentUserService>()),
            new ConfigurationBuilder().Build(),
            NullLogger<DeliveryOrchestrationService>.Instance);
    }

    private async Task<(Guid RequestId, Guid UserId)> SeedAsync(bool requiresHumanProdDeploy)
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
            TenantId = tenantId,
            CreatedAt = now,
            UpdatedAt = now,
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
            AllowOneClickProdDeploy = true,
            AllowOneClickRollback = true,
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
            RequiresHumanProdDeploy = requiresHumanProdDeploy,
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
            RollbackPlan = "Disable feature flag",
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
