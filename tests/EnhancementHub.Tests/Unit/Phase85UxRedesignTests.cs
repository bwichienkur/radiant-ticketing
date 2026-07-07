using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase85UxRedesignTests
{
    [Fact]
    public void SpaShell_IncludesPortfolioHubRoute()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/Portfolio");
        shell.Should().Contain("PortfolioHubApp");
        shell.Should().Contain("resolveSpaPageMeta");
    }

    [Fact]
    public void HeroWorkflows_UseTabAndSegmentedControls()
    {
        var requestDetail = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/RequestDetailApp.tsx"));
        var createRequest = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/CreateRequestApp.tsx"));
        requestDetail.Should().Contain("TabBar");
        requestDetail.Should().Contain("overview");
        requestDetail.Should().Contain("analysis");
        requestDetail.Should().Contain("delivery");
        requestDetail.Should().Contain("activity");
        createRequest.Should().Contain("SegmentedControl");
        createRequest.Should().Contain("describe");
        createRequest.Should().Contain("template");
        createRequest.Should().Contain("manual");
    }

    [Fact]
    public void Dashboard_UsesOmniboxCtaInsteadOfInlineCopilotBar()
    {
        var dashboard = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/DashboardApp.tsx"));
        dashboard.Should().Contain("eh-omnibox-cta");
        dashboard.Should().Contain("data-command-trigger");
        dashboard.Should().NotContain("copilot-bar");
    }

    [Fact]
    public void DesignSystemV2_IncludesSharedStylesAndComponents()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/site.css"));
        var uiIndex = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/ui/index.ts"));
        css.Should().Contain(".eh-tab-bar");
        css.Should().Contain(".eh-segmented-control");
        css.Should().Contain(".eh-hub-grid");
        uiIndex.Should().Contain("TabBar");
        uiIndex.Should().Contain("SegmentedControl");
    }

    [Fact]
    public void SidebarNav_RestructuresPortfolioAndGovernanceSections()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        nav.Should().Contain("sidebar-section-label\">Portfolio");
        nav.Should().Contain("href=\"/Spa/Portfolio\"");
        nav.Should().Contain("sidebar-section-label\">Governance");
        nav.Should().Contain("href=\"/Spa/Search\"");
    }

    [Fact]
    public void LoginLayout_UsesSegmentedThemeSelector()
    {
        var layout = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_LoginLayout.cshtml"));
        layout.Should().Contain("data-theme-preference");
        layout.Should().Contain("eh-segmented-control");
        layout.Should().NotContain("data-theme-toggle");
    }

    [Fact]
    public void SpaPageMeta_ResolvesPortfolioAndSettingsRoutes()
    {
        var meta = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/spaPageMeta.ts"));
        meta.Should().Contain("'/Spa/Portfolio'");
        meta.Should().Contain("'/Spa/Settings/General'");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
