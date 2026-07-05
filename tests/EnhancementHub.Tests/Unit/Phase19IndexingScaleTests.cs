using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Persistence;
using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase19IndexingScaleTests
{
    [Theory]
    [InlineData("src/Backend/Controllers/Foo.cs", "src/Backend", "Controllers/Foo.cs")]
    [InlineData("src/Backend/Controllers/Foo.cs", null, "src/Backend/Controllers/Foo.cs")]
    [InlineData("docs/readme.md", "src/Backend", null)]
    [InlineData("src/Backend", "src/Backend", "")]
    public void MapGitPathToIndexPath_ScopesMonorepoPaths(
        string gitPath,
        string? subdirectory,
        string? expected)
    {
        var mapped = RepositoryIndexerService.MapGitPathToIndexPath(gitPath, subdirectory);
        mapped.Should().Be(expected);
    }

    [Fact]
    public async Task IndexFreshnessService_ComputesFreshnessPercent()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnhancementHubDbContext>();

        var teamId = Guid.NewGuid();
        db.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Freshness Team",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var appId = Guid.NewGuid();
        db.Applications.Add(new Domain.Entities.Application
        {
            Id = appId,
            Name = "Freshness App",
            OwnerTeamId = teamId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var freshId = Guid.NewGuid();
        var staleId = Guid.NewGuid();
        db.Repositories.AddRange(
            new Repository
            {
                Id = freshId,
                ApplicationId = appId,
                Name = "Fresh Repo",
                Url = "/tmp/fresh",
                Provider = ExternalTicketProvider.GitHub,
                DefaultBranch = "main",
                IndexingStatus = IndexingStatus.Completed,
                LastIndexedAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Repository
            {
                Id = staleId,
                ApplicationId = appId,
                Name = "Stale Repo",
                Url = "/tmp/stale",
                Provider = ExternalTicketProvider.GitHub,
                DefaultBranch = "main",
                IndexingStatus = IndexingStatus.Completed,
                LastIndexedAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var service = new IndexFreshnessService(
            db,
            Options.Create(new IndexingOptions { FreshnessSlaHours = 24 }));

        var report = await service.GetReportAsync();

        report.TotalRepositories.Should().BeGreaterThanOrEqualTo(2);
        report.FreshCount.Should().BeGreaterThanOrEqualTo(1);
        report.StaleCount.Should().BeGreaterThanOrEqualTo(1);
        report.StaleRepositories.Should().Contain(r => r.Name == "Stale Repo");
    }

    [Fact]
    public async Task IndexFreshnessEndpoint_ReturnsReportForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/admin/indexing/freshness");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("freshnessPercent");
        json.Should().Contain("slaHours");
    }
}
