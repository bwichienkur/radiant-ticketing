using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase84MarketReadinessTests
{
    [Fact]
    public void MarketReadinessVerificationDoc_ExistsWithGates()
    {
        var doc = File.ReadAllText(GetPath("docs/MARKET_READINESS_VERIFICATION.md"));
        doc.Should().Contain("Launch readiness");
        doc.Should().Contain("Marketability");
        doc.Should().Contain("Phase 84");
        doc.Should().Contain("10 flows");
        doc.Should().Contain("Phase 85");
    }

    [Fact]
    public void AllAdminRazorPages_MarkedObsoleteWithSpaRedirects()
    {
        var adminDir = GetPath("src/EnhancementHub.Web/Pages/Admin");
        var pageModels = Directory.GetFiles(adminDir, "*.cshtml.cs");
        pageModels.Should().NotBeEmpty();

        foreach (var page in pageModels)
        {
            var content = File.ReadAllText(page);
            content.Should().Contain("[Obsolete", $"{Path.GetFileName(page)} should be obsolete");
            content.Should().Contain("/Spa/", $"{Path.GetFileName(page)} should redirect to SPA");
        }
    }

    [Fact]
    public void SpaShell_IncludesAdminAndNotificationRoutes()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/Admin/*");
        shell.Should().Contain("/Spa/Account/Notifications");
        shell.Should().Contain("/Spa/Applications/:id");
        shell.Should().Contain("/Spa/Portfolio");
    }

    [Fact]
    public void PortfolioHealth_HasCsvExportEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaPortfolioController.cs"));
        controller.Should().Contain("health/export");
    }

    [Fact]
    public void ProductScorecard_ReflectsMarketReadyTargets()
    {
        var scorecard = File.ReadAllText(GetPath("docs/PRODUCT_SCORECARD.md"));
        scorecard.Should().Contain("Market ready");
        scorecard.Should().Contain("Phase 84");
    }

    [Fact]
    public void AccessibilitySpec_CoversTenExpandedFlows()
    {
        var spec = File.ReadAllText(GetPath("tests/e2e/accessibility.spec.ts"));
        var flowCount = spec.Split("expectNoSeriousViolations", StringSplitOptions.None).Length - 1;
        flowCount.Should().BeGreaterThanOrEqualTo(10);
        spec.Should().Contain("/Spa/Portfolio");
        spec.Should().Contain("/Spa/PortfolioHealth");
        spec.Should().Contain("/Spa/Search");
    }

    [Fact]
    public void SettingsApp_UsesSinglePageHeaderWithSectionCards()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/SettingsApp.tsx"));
        app.Should().Contain("PageHeader");
        app.Should().Contain("title=\"Settings\"");

        var general = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/settings/SettingsGeneralSection.tsx"));
        general.Should().Contain("SectionCard");
        general.Should().NotContain("eh-section-title mb-1");
    }

    [Fact]
    public void UxHeuristicReview_IncludesWave5PostRedesignScores()
    {
        var review = File.ReadAllText(GetPath("docs/UX_HEURISTIC_REVIEW.md"));
        review.Should().Contain("Wave 5");
        review.Should().Contain("4.38");
    }

    [Fact]
    public void Readme_ReflectsCurrentAutomatedTestCount()
    {
        var readme = File.ReadAllText(GetPath("README.md"));
        readme.Should().MatchRegex(@"53[0-9]\+ automated tests");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
