using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase37ReactOnboardingAdvancedTests
{
    [Fact]
    public void SpaBff_ExposesAdvancedOnboardingBff()
    {
        var sources = SpaBffTestHelper.ReadAllSpaBffSources();
        sources.Should().Contain("upload-zip");
        sources.Should().Contain("clone-github-app");
        sources.Should().Contain("clone-git");
        sources.Should().Contain("build-connection-string");
        sources.Should().Contain("on-prem-agent");
        sources.Should().Contain("export-docs");
        sources.Should().Contain("GetGitHubAppStatusQuery");
    }

    [Fact]
    public void OnboardingAdvancedSteps_CoversAllCodeModes()
    {
        var component = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/OnboardingAdvancedSteps.tsx"));
        component.Should().Contain("uploadOnboardingZip");
        component.Should().Contain("cloneGitHubAppRepository");
        component.Should().Contain("cloneGitRepository");
        component.Should().Contain("setupOnPremAgent");
        component.Should().Contain("buildDatabaseConnectionString");
    }

    [Fact]
    public void OnboardingWizardApp_UsesAdvancedSteps()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/OnboardingWizardApp.tsx"));
        app.Should().Contain("OnboardingCodeStep");
        app.Should().Contain("OnboardingDatabaseStep");
        app.Should().Contain("getOnboardingExportDocsUrl");
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
