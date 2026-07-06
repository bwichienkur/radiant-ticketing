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

namespace EnhancementHub.Tests.Unit;

public sealed class NightlyRegressionServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EnhancementHubDbContext _dbContext;

    public NightlyRegressionServiceTests()
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
    public async Task RunScheduledRegression_RecordsApplicationRegressionRun()
    {
        var appId = await SeedActiveRegressionCaseAsync();
        var catalog = new TestCaseCatalogService(_dbContext);
        var fileStorage = new LocalFileStorageService(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:Provider"] = "Local",
                    ["Storage:LocalRoot"] = Path.Combine(Path.GetTempPath(), "eh-nightly-tests")
                }).Build());
        var qaRunner = new PlaywrightQaRunner(
            EmptyHttpClientFactory.Instance,
            fileStorage,
            new SimulatedQaRunner(fileStorage),
            new ConfigurationBuilder().Build(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PlaywrightQaRunner>.Instance);
        var service = new NightlyRegressionService(
            _dbContext,
            catalog,
            qaRunner,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<NightlyRegressionService>.Instance);

        await service.RunScheduledRegressionAsync();

        var runs = await _dbContext.ApplicationRegressionRuns
            .Where(r => r.ApplicationId == appId)
            .ToListAsync();
        runs.Should().ContainSingle();
        runs[0].CaseCount.Should().BeGreaterThan(0);
    }

    private async Task<Guid> SeedActiveRegressionCaseAsync()
    {
        var tenantId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var suiteId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t", CreatedAt = now, UpdatedAt = now });
        _dbContext.Teams.Add(new Team { Id = teamId, Name = "Team", TenantId = tenantId, CreatedAt = now, UpdatedAt = now });
        _dbContext.Applications.Add(new Domain.Entities.Application
        {
            Id = appId,
            Name = "App",
            OwnerTeamId = teamId,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.TenantDeploymentEnvironments.Add(new TenantDeploymentEnvironment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Test",
            EnvironmentType = DeploymentEnvironmentType.Test,
            BaseUrlTemplate = "https://unreachable-test.example",
            IsActive = true,
            SortOrder = 0,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.ApplicationTestSuites.Add(new ApplicationTestSuite
        {
            Id = suiteId,
            ApplicationId = appId,
            Name = "Regression",
            IsDefaultRegression = true,
            CreatedAt = now,
            UpdatedAt = now,
        });
        _dbContext.TestCases.Add(new TestCase
        {
            Id = Guid.NewGuid(),
            TestSuiteId = suiteId,
            Title = "Smoke",
            Status = TestCaseStatus.Active,
            Origin = TestCaseOrigin.Promoted,
            StepsJson = """[{"Order":1,"Action":"Open app","ExpectedResult":"OK"}]""",
            CurrentVersion = 1,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await _dbContext.SaveChangesAsync();
        return appId;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class EmptyHttpClientFactory : IHttpClientFactory
    {
        public static readonly EmptyHttpClientFactory Instance = new();

        public HttpClient CreateClient(string name) => new();
    }
}
