using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase31ReactSpaExtendedTests
{
    [Fact]
    public void ClientApp_IncludesApprovalAndOnboardingEntries()
    {
        var viteConfig = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/vite.config.ts"));
        viteConfig.Should().Contain("spa-shell");
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("ApprovalQueueApp");
        shell.Should().Contain("OnboardingWizardApp");
    }

    [Fact]
    public void ReactBundles_IncludeApprovalAndOnboarding()
    {
        File.Exists(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/spa-shell.js")).Should().BeTrue();
        Directory.GetFiles(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/chunks"), "ApprovalQueueApp-*.js")
            .Should().NotBeEmpty();
        Directory.GetFiles(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/chunks"), "OnboardingWizardApp-*.js")
            .Should().NotBeEmpty();
    }

    [Fact]
    public void SpaBff_ExposesApprovalAndOnboardingBff()
    {
        var sources = SpaBffTestHelper.ReadAllSpaBffSources();
        sources.Should().Contain("web-api/spa/approvals");
        sources.Should().Contain("[HttpGet(\"pending\")]");
        sources.Should().Contain("SubmitApprovalActionCommand");
        sources.Should().Contain("web-api/spa/onboarding");
        sources.Should().Contain("[HttpPost(\"start\")]");
        sources.Should().Contain("advance-review");
        sources.Should().Contain("QueueApplicationDiscoveryCommand");
    }

    [Fact]
    public void ApprovalQueueSpaPage_MountsReactBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/ApprovalQueue.cshtml"));
        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa/react/spa-shell.js");
    }

    [Fact]
    public void OnboardingWizardSpaPage_MountsReactBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/OnboardingWizard.cshtml"));
        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa/react/spa-shell.js");
    }

    [Fact]
    public void ClassicPages_RedirectToReactRoutes()
    {
        var approve = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/EnhancementRequests/Approve.cshtml.cs"));
        var wizard = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Onboarding/Wizard.cshtml.cs"));
        approve.Should().Contain("RedirectToPage(\"/Spa/ApprovalQueue\"");
        wizard.Should().Contain("RedirectToPage(\"/Spa/OnboardingWizard\"");
    }

    [Fact]
    public void ApprovalQueueReactComponent_HasKeyboardNavigation()
    {
        var component = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/ApprovalQueueApp.tsx"));
        component.Should().Contain("event.key === 'j'");
        component.Should().Contain("RequestClarification");
    }

    [Fact]
    public void OnboardingWizardReactComponent_CoversCoreSteps()
    {
        var component = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/OnboardingWizardApp.tsx"));
        component.Should().Contain("ApplicationBasics");
        component.Should().Contain("ConnectCode");
        component.Should().Contain("RunDiscovery");
        component.Should().Contain("ReviewExport");
    }

    private static string GetPath(string relativePath) =>
        Path.Combine(GetRepoRoot(), relativePath);

    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "EnhancementHub.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new InvalidOperationException("Repo root not found");
    }
}
