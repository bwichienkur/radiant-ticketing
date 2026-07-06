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
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
