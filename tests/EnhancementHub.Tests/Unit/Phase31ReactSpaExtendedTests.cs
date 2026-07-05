using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase31ReactSpaExtendedTests
{
    [Fact]
    public void ClientApp_IncludesApprovalAndOnboardingEntries()
    {
        var viteConfig = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/vite.config.ts"));
        viteConfig.Should().Contain("approval-queue");
        viteConfig.Should().Contain("onboarding-wizard");
    }

    [Fact]
    public void ReactBundles_IncludeApprovalAndOnboarding()
    {
        File.Exists(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/approval-queue.js")).Should().BeTrue();
        File.Exists(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/onboarding-wizard.js")).Should().BeTrue();
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
        page.Should().Contain("spa-approval-queue-root");
        page.Should().Contain("spa/react/approval-queue.js");
    }

    [Fact]
    public void OnboardingWizardSpaPage_MountsReactBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/OnboardingWizard.cshtml"));
        page.Should().Contain("spa-onboarding-wizard-root");
        page.Should().Contain("spa/react/onboarding-wizard.js");
    }

    [Fact]
    public void ClassicPages_LinkToReactAlternatives()
    {
        var approve = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/EnhancementRequests/Approve.cshtml"));
        var wizard = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Onboarding/Wizard.cshtml"));
        approve.Should().Contain("/Spa/ApprovalQueue");
        wizard.Should().Contain("/Spa/OnboardingWizard");
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
