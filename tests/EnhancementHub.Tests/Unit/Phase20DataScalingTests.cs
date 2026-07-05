using EnhancementHub.Infrastructure.Services;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase20DataScalingTests
{
    [Fact]
    public async Task DataScalingStatus_FlagsInMemoryVectorSearch()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Application.Abstractions.IEnhancementHubDbContext>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VectorSearch:Provider"] = "InMemory",
                ["Database:Provider"] = "Sqlite"
            })
            .Build();

        var service = new DataScalingStatusService(
            db,
            configuration,
            Microsoft.Extensions.Options.Options.Create(new Application.Options.DatabaseScalingOptions()),
            Microsoft.Extensions.Options.Options.Create(new Infrastructure.Options.RetentionOptions()));

        var status = await service.GetStatusAsync();

        status.VectorSearch.Provider.Should().Be("InMemory");
        status.VectorSearch.IsProductionReady.Should().BeFalse();
        status.VectorSearch.RecommendedProvider.Should().Be("Qdrant");
    }

    [Fact]
    public async Task DataScalingStatus_DetectsReadReplicaConfiguration()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Application.Abstractions.IEnhancementHubDbContext>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VectorSearch:Provider"] = "Qdrant",
                ["VectorSearch:Qdrant:Url"] = "http://localhost:6333",
                ["ConnectionStrings:Default"] = "Host=primary;",
                ["ConnectionStrings:Reporting"] = "Host=replica;",
                ["Database:Provider"] = "PostgreSQL"
            })
            .Build();

        var status = await new DataScalingStatusService(
            db,
            configuration,
            Microsoft.Extensions.Options.Options.Create(new Application.Options.DatabaseScalingOptions()),
            Microsoft.Extensions.Options.Options.Create(new Infrastructure.Options.RetentionOptions()))
            .GetStatusAsync();

        status.Database.ReadReplicaConfigured.Should().BeTrue();
        status.VectorSearch.IsProductionReady.Should().BeTrue();
    }

    [Fact]
    public async Task DataScalingEndpoint_ReturnsStatusForAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        var admin = await factory.CreateDataBuilder().CreateUserAsync(Domain.Enums.UserRole.Admin);

        using var client = await factory.CreateAuthenticatedClientAsync(admin);
        var response = await client.GetAsync("/api/admin/data-scaling/status");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("vectorSearch");
        json.Should().Contain("archival");
    }
}
