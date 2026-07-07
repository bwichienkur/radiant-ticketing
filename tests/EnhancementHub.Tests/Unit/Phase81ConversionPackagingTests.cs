using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase81ConversionPackagingTests
{
    [Fact]
    public void FeedbackWidget_IncludesPortfolioHealthSettingsAndAdminWorkflows()
    {
        var widget = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/FeedbackWidget.tsx"));
        widget.Should().Contain("'portfolio-health'");
        widget.Should().Contain("'settings'");
        widget.Should().Contain("'admin'");
        widget.Should().Contain("/Spa/PortfolioHealth");
        widget.Should().Contain("/Spa/Settings");
        widget.Should().Contain("/Spa/Admin");
    }

    [Fact]
    public void TrialFlowE2E_SpecExists()
    {
        var spec = File.ReadAllText(GetPath("tests/e2e/trial-flow.spec.ts"));
        spec.Should().Contain("/Account/Signup");
        spec.Should().Contain("/Spa/CreateRequest");
        spec.Should().Contain("Submit request");
    }

    [Fact]
    public void PublicPricingPage_IsAnonymousAndLinksToSignup()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Pricing/Pricing.cshtml"));
        page.Should().Contain("Start your free trial");
        page.Should().Contain("Team");
        page.Should().Contain("Enterprise");

        var pageModel = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Pricing/Pricing.cshtml.cs"));
        pageModel.Should().Contain("[AllowAnonymous]");

        var program = File.ReadAllText(GetPath("src/EnhancementHub.Web/Program.cs"));
        program.Should().Contain("AllowAnonymousToPage(\"/Pricing\")");
    }

    [Fact]
    public void PortfolioHealth_HasCsvExportEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaPortfolioController.cs"));
        controller.Should().Contain("health/export");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
