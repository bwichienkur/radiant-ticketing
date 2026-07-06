using EnhancementHub.Application.Features.Reporting.Queries;
using EnhancementHub.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase48RouteConsolidationTests
{
    [Fact]
    public async Task GetDashboardInsights_IncludesPortfolioHealthCounts()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.EnsureDatabaseInitializedAsync();

        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();

        var insights = await mediator.Send(new GetDashboardInsightsQuery());

        insights.UnresolvedDriftFindings.Should().BeGreaterThanOrEqualTo(0);
        insights.StaleRepositoryCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void SidebarNav_UsesSpaRoutesOnlyForWorkSection()
    {
        var nav = File.ReadAllText(Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));

        nav.Should().Contain("/Spa/RequestList");
        nav.Should().Contain("/Spa/ApprovalQueue");
        nav.Should().Contain("/Spa/CreateRequest");
        nav.Should().NotContain("/EnhancementRequests");
        nav.Should().NotContain("asp-page=\"/Onboarding");
        nav.Should().NotContain("asp-page=\"/SystemMap");
    }

    [Fact]
    public void EnhancementRequestLegacyPages_AreMarkedObsolete()
    {
        var root = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Pages/EnhancementRequests");
        foreach (var file in Directory.GetFiles(root, "*.cshtml.cs"))
        {
            File.ReadAllText(file).Should().Contain("[Obsolete(");
        }
    }

    [Fact]
    public void TeamDetail_IncludesAdminSubNav()
    {
        var page = File.ReadAllText(Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Pages/Admin/TeamDetail.cshtml"));
        page.Should().Contain("_AdminNav");
        page.Should().Contain("PageHeaderTitle");
    }

    [Fact]
    public void SiteJs_DocumentsSpaVsFullPageRoutes()
    {
        var siteJs = File.ReadAllText(Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/wwwroot/js/site.js"));
        siteJs.Should().Contain("SPA vs full-page navigation");
        siteJs.Should().Contain("initCommandPaletteKbd");
    }

    private static string GetRepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
