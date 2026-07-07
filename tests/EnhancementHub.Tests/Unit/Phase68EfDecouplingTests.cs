using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase68EfDecouplingTests
{
    [Fact]
    public void Infrastructure_RegistersAggregateRepositories()
    {
        var extensions = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs"));
        extensions.Should().Contain("IEnhancementRequestRepository, EnhancementRequestRepository");
        extensions.Should().Contain("IApplicationRepository, ApplicationRepository");
        extensions.Should().Contain("ITeamRepository, TeamRepository");
        extensions.Should().Contain("IGitRepositoryRepository, GitRepositoryRepository");
        extensions.Should().Contain("IEnhancementAnalysisRepository, EnhancementAnalysisRepository");
    }

    [Fact]
    public void EnhancementHubDbContext_AppliesTenantQueryFilters()
    {
        var dbContext = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Persistence/EnhancementHubDbContext.cs"));
        dbContext.Should().Contain("FilterTenantId");
        dbContext.Should().Contain("ApplyTenantQueryFilters");
        dbContext.Should().Contain("HasQueryFilter");
    }

    [Fact]
    public void MigratedHandlers_UseRepositories()
    {
        var listTeams = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Admin/Queries/ListTeamsQueryHandler.cs"));
        listTeams.Should().Contain("ITeamRepository");
        listTeams.Should().NotContain("IEnhancementHubDbContext");

        var login = File.ReadAllText(GetPath("src/EnhancementHub.Application/Auth/LoginCommand.cs"));
        login.Should().Contain("IUserRepository");
        login.Should().NotContain("IEnhancementHubDbContext");
    }

    [Fact]
    public void EfHandlerAllowlist_ExistsAndIsEnforcedInCi()
    {
        var allowlist = File.ReadAllText(GetPath("docs/ef-handler-allowlist.txt"));
        allowlist.Should().Contain("ListEnhancementRequestsQuery.cs");
        allowlist.Should().NotContain("ListTeamsQueryHandler.cs");

        var ci = File.ReadAllText(GetPath(".github/workflows/ci.yml"));
        ci.Should().Contain("check-ef-handler-allowlist.mjs");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
