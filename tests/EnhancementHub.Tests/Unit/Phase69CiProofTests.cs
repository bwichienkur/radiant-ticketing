using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase69CiProofTests
{
    [Fact]
    public void PostgresK6GateScript_ExistsWith500VuProfile()
    {
        var script = File.ReadAllText(GetPath("tests/load/k6-postgres-gate.js"));
        script.Should().Contain("target: 500");
        script.Should().Contain("p(95)<2000");
    }

    [Fact]
    public void CiWorkflow_IncludesPostgresLoadStorybookAndAllowlist()
    {
        var ci = File.ReadAllText(GetPath(".github/workflows/ci.yml"));
        ci.Should().Contain("run-load-test-postgres.mjs");
        ci.Should().Contain("build-storybook");
        ci.Should().Contain("check-ef-handler-allowlist.mjs");
    }

    [Fact]
    public void AccessibilityWorkflow_RunsAxeE2eSuite()
    {
        var workflow = File.ReadAllText(GetPath(".github/workflows/accessibility.yml"));
        workflow.Should().Contain("run-e2e-accessibility.mjs");
    }

    [Fact]
    public void UIKitStories_ExistForVisualBaseline()
    {
        var stories = File.ReadAllText(GetPath(
            "src/EnhancementHub.Web/ClientApp/src/components/ui/UIKit.stories.tsx"));
        stories.Should().Contain("UI Kit/Components");
        stories.Should().Contain("PageHeaderDefault");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
