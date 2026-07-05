using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase34AccessibilityTests
{
    [Fact]
    public void SystemMapGraph_SupportsKeyboardNavigation()
    {
        var component = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SystemMapGraph.tsx"));
        component.Should().Contain("tabIndex={0}");
        component.Should().Contain("onKeyDown");
        component.Should().Contain("ArrowRight");
    }

    [Fact]
    public void OnboardingWizard_HasAriaCurrentOnSteps()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/OnboardingWizardApp.tsx"));
        app.Should().Contain("aria-current={isCurrent ? 'step' : undefined}");
    }

    [Fact]
    public void ClassicOnboardingProgress_HasAriaCurrent()
    {
        var partial = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Onboarding/_OnboardingProgress.cshtml"));
        partial.Should().Contain("aria-current");
    }

    [Fact]
    public void AdminJobsTable_UsesColumnScope()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Jobs.cshtml"));
        page.Should().Contain("scope=\"col\"");
    }

    [Fact]
    public void AccessibilityWorkflow_Exists()
    {
        File.Exists(GetPath(".github/workflows/accessibility.yml")).Should().BeTrue();
        File.Exists(GetPath("tests/a11y/static-check.mjs")).Should().BeTrue();
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
