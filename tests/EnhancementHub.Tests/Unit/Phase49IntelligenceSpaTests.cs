using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase49IntelligenceSpaTests
{
    [Fact]
    public void SpaShell_IncludesIntelligenceRoutes()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/Applications");
        shell.Should().Contain("/Spa/SchemaDrift");
        shell.Should().Contain("/Spa/Repositories");
        shell.Should().Contain("/Spa/Audit");
        shell.Should().Contain("ApplicationsApp");
        shell.Should().Contain("SchemaDriftApp");
    }

    [Fact]
    public void SpaIntelligenceController_ExposesBffEndpoints()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaIntelligenceController.cs"));
        controller.Should().Contain("drift/connections");
        controller.Should().Contain("drift/report");
        controller.Should().Contain("drift/detect");
        controller.Should().Contain("repositories");
        controller.Should().Contain("audit/logs");
        controller.Should().Contain("audit/export");
    }

    [Fact]
    public void IntelligenceSpaPages_MountUnifiedBundle()
    {
        foreach (var page in new[]
        {
            "Applications.cshtml",
            "SchemaDrift.cshtml",
            "Repositories.cshtml",
            "Audit.cshtml",
        })
        {
            var content = File.ReadAllText(GetPath($"src/EnhancementHub.Web/Pages/Spa/{page}"));
            content.Should().Contain("_SpaRoot");
            content.Should().Contain("spa-shell.js");
        }
    }

    [Fact]
    public void ViteConfig_SplitsVendorChunks()
    {
        var vite = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/vite.config.ts"));
        vite.Should().Contain("manualChunks");
        vite.Should().Contain("vendor-react");
    }

    [Fact]
    public void LegacyIntelligencePages_RedirectToSpa()
    {
        var applications = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Applications/Index.cshtml.cs"));
        var drift = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/SchemaDrift/Index.cshtml.cs"));
        var repositories = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Repositories/Index.cshtml.cs"));
        var audit = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Audit/Index.cshtml.cs"));

        applications.Should().Contain("RedirectToPagePermanent(\"/Spa/Applications\"");
        drift.Should().Contain("RedirectToPagePermanent(\"/Spa/SchemaDrift\"");
        repositories.Should().Contain("RedirectToPagePermanent(\"/Spa/Repositories\"");
        audit.Should().Contain("RedirectToPagePermanent(\"/Spa/Audit\"");
    }

    [Fact]
    public void SidebarNav_PointsIntelligenceListRoutesToSpa()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        nav.Should().Contain("/Spa/Applications");
        nav.Should().Contain("/Spa/SchemaDrift");
        nav.Should().Contain("/Spa/Repositories");
        nav.Should().Contain("/Spa/Audit");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
