using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase70DriftAutopilotTests
{
    [Fact]
    public void SpaShell_IncludesPortfolioHealthRoute()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/PortfolioHealth");
        shell.Should().Contain("PortfolioHealthApp");
    }

    [Fact]
    public void SpaPortfolioController_ExposesHealthEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaPortfolioController.cs"));
        controller.Should().Contain("[Route(\"web-api/spa/portfolio\")]");
        controller.Should().Contain("Get(\"health\")");
        controller.Should().Contain("GetPortfolioHealthQuery");
        controller.Should().Contain("Admin,Approver");
    }

    [Fact]
    public void DriftAutopilotService_UsesFeatureFlagAndProvenance()
    {
        var service = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/Services/SystemIntelligence/DriftAutopilotService.cs"));
        service.Should().Contain("FeatureFlags.DriftAutopilot");
        service.Should().Contain("DriftRequestProvenance.BuildSupportingNotes");
        service.Should().Contain("DriftSeverity.Critical");
    }

    [Fact]
    public void HangfireInitializer_RegistersDriftAutopilotJob()
    {
        var initializer = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/Background/HangfireRecurringJobInitializer.cs"));
        initializer.Should().Contain("drift-autopilot");
        initializer.Should().Contain("DriftAutopilotJobExecutor");
    }

    [Fact]
    public void PortfolioHealthSpaPage_MountsUnifiedBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/PortfolioHealth.cshtml"));
        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa-shell.js");
    }

    [Fact]
    public void SidebarNav_IncludesPortfolioHealthLink()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        nav.Should().Contain("href=\"/Spa/PortfolioHealth\"");
    }

    [Fact]
    public void PortfolioHealthApp_RendersRiskHeatmap()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/PortfolioHealthApp.tsx"));
        app.Should().Contain("getPortfolioHealth");
        app.Should().Contain("portfolio-heatmap");
        app.Should().Contain("riskScore");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
