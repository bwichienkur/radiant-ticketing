using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase61ShellUnificationTests
{
    [Fact]
    public void SpaShell_IncludesPhase61Routes()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/DatabaseConnections");
        shell.Should().Contain("/Spa/Documentation/Export");
        shell.Should().Contain("/Spa/Refactor/Analyze");
        shell.Should().Contain("/Spa/Refactor/Plans");
        shell.Should().Contain("DatabaseConnectionsApp");
        shell.Should().Contain("DocumentationExportApp");
        shell.Should().Contain("RefactorAnalyzeApp");
    }

    [Fact]
    public void SpaIntelligenceController_ExposesPhase61BffEndpoints()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaIntelligenceController.cs"));
        controller.Should().Contain("connections");
        controller.Should().Contain("documentation/export");
        controller.Should().Contain("refactor/analyze");
        controller.Should().Contain("refactor/plans");
    }

    [Fact]
    public void Phase61SpaPages_MountUnifiedBundle()
    {
        foreach (var page in new[]
        {
            "DatabaseConnections.cshtml",
            "DocumentationExport.cshtml",
            "RefactorAnalyze.cshtml",
            "RefactorPlans.cshtml",
        })
        {
            var content = File.ReadAllText(GetPath($"src/EnhancementHub.Web/Pages/Spa/{page}"));
            content.Should().Contain("_SpaRoot");
            content.Should().Contain("spa-shell.js");
        }
    }

    [Fact]
    public void LegacyIntelligencePages_RedirectToSpa()
    {
        var databases = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/DatabaseConnections/Index.cshtml.cs"));
        var docs = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Documentation/Export.cshtml.cs"));
        var refactor = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Refactor/Analyze.cshtml.cs"));
        var plans = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Refactor/Plans.cshtml.cs"));

        databases.Should().Contain("RedirectToPagePermanent(\"/Spa/DatabaseConnections\"");
        docs.Should().Contain("RedirectToPagePermanent(\"/Spa/DocumentationExport\"");
        refactor.Should().Contain("RedirectToPagePermanent(\"/Spa/RefactorAnalyze\"");
        plans.Should().Contain("RedirectToPagePermanent(\"/Spa/RefactorPlans\"");
    }

    [Fact]
    public void SidebarNav_PointsPhase61RoutesToSpa()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        nav.Should().Contain("/Spa/DatabaseConnections");
        nav.Should().Contain("/Spa/Portfolio");
        nav.Should().NotContain("asp-page=\"/DatabaseConnections/Index\"");
        nav.Should().NotContain("asp-page=\"/Documentation/Export\"");
        nav.Should().NotContain("asp-page=\"/Refactor/Analyze\"");
    }

    [Fact]
    public void SpaRoutes_IncludesPhase61Prefixes()
    {
        var routes = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/spaRoutes.ts"));
        routes.Should().Contain("/Spa/DatabaseConnections");
        routes.Should().Contain("/Spa/Documentation/Export");
        routes.Should().Contain("/Spa/Refactor/Analyze");
        routes.Should().Contain("/Spa/Refactor/Plans");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
