using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase51GlobalSearchTests
{
    [Fact]
    public void SpaSearchController_ExposesUnifiedSearchEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaSearchController.cs"));
        controller.Should().Contain("web-api/spa");
        controller.Should().Contain("[HttpGet(\"search\")]");
        controller.Should().Contain("GlobalEntitySearchQuery");
        controller.Should().Contain("grouped");
    }

    [Fact]
    public void GlobalEntitySearchQuery_SearchesMultipleEntityTypes()
    {
        var handler = File.ReadAllText(GetPath("src/EnhancementHub.Application/Features/Search/Queries/GlobalEntitySearchQuery.cs"));
        handler.Should().Contain("SearchRequestsAsync");
        handler.Should().Contain("SearchApplicationsAsync");
        handler.Should().Contain("SearchRepositoriesAsync");
        handler.Should().Contain("SearchDriftFindingsAsync");
        handler.Should().Contain("SearchSymbolsAsync");
        handler.Should().Contain("SearchVectorArtifactsAsync");
        handler.Should().Contain("IVectorSearchService");
    }

    [Fact]
    public void SearchSpaPage_MountsUnifiedBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/Search.cshtml"));
        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa-shell.js");
    }

    [Fact]
    public void SpaShell_IncludesSearchRoute()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/Search");
        shell.Should().Contain("SearchApp");
    }

    [Fact]
    public void SiteJs_CommandPaletteUsesSpaSearchApi()
    {
        var siteJs = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/js/site.js"));
        siteJs.Should().Contain("/web-api/spa/search?q=");
        siteJs.Should().Contain("eh-recent-searches");
        siteJs.Should().Contain("/Spa/Search?q=");
    }

    [Fact]
    public void UxController_DelegatesSearchToGlobalEntitySearchQuery()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/UxController.cs"));
        controller.Should().Contain("GlobalEntitySearchQuery");
        controller.Should().NotContain("ListApplicationsQuery");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
