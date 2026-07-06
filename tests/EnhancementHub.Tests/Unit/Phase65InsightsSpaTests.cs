using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase65InsightsSpaTests
{
    [Fact]
    public void SpaShell_IncludesInsightsRoute()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/Insights");
        shell.Should().Contain("InsightsApp");
    }

    [Fact]
    public void SpaInsightsController_ExposesRoiEndpoints()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaInsightsController.cs"));
        controller.Should().Contain("[Route(\"web-api/spa/insights\")]");
        controller.Should().Contain("Get(\"roi\")");
        controller.Should().Contain("roi/export");
        controller.Should().Contain("GetRoiReportQuery");
        controller.Should().Contain("Admin,Approver");
    }

    [Fact]
    public void InsightsSpaPage_MountsUnifiedBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/Insights.cshtml"));
        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa-shell.js");
    }

    [Fact]
    public void LegacyRoiPage_RedirectsToInsightsSpa()
    {
        var pageModel = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Roi.cshtml.cs"));
        pageModel.Should().Contain("RedirectPermanent(\"/Spa/Insights\"");
        pageModel.Should().Contain("[Obsolete");
    }

    [Fact]
    public void SidebarNav_IncludesInsightsLink()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        nav.Should().Contain("href=\"/Spa/Insights\"");
    }

    [Fact]
    public void InsightsApp_RendersRoiStatCardsAndExport()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/InsightsApp.tsx"));
        app.Should().Contain("getRoiReport");
        app.Should().Contain("exportRoiCsv");
        app.Should().Contain("Analyses completed");
        app.Should().Contain("Pilot NPS");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
