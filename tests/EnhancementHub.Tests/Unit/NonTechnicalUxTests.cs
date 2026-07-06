using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class NonTechnicalUxTests
{
    [Fact]
    public void CreateRequestApp_UsesCopilotFirstHeadline()
    {
        var content = ReadClientFile("apps/CreateRequestApp.tsx");
        content.Should().Contain("Tell us what you need changed");
        content.Should().Contain("Fill in the form manually");
    }

    [Fact]
    public void IntakeCopilotPanel_UsesPlainLanguageLabels()
    {
        var content = ReadClientFile("components/IntakeCopilotPanel.tsx");
        content.Should().Contain("Describe your need");
        content.Should().Contain("data-tour=\"intake-copilot\"");
        content.Should().Contain("Privacy note");
    }

    [Fact]
    public void RequestLabels_ProvidesPlainStatusCopy()
    {
        var content = ReadClientFile("utils/requestLabels.ts");
        content.Should().Contain("Being analyzed");
        content.Should().Contain("getStatusNextStep");
    }

    [Fact]
    public void RequestDetailApp_ShowsStatusNextStep()
    {
        var content = ReadClientFile("apps/RequestDetailApp.tsx");
        content.Should().Contain("getStatusNextStep");
        content.Should().Contain("Your original request");
    }

    [Fact]
    public void AnalysisDetailSections_CollapsesTechnicalContent()
    {
        var content = ReadClientFile("components/AnalysisDetailSections.tsx");
        content.Should().Contain("Technical details for your IT team");
        content.Should().Contain("Show details");
    }

    [Fact]
    public void MissionControl_UsesImpactAtAGlance()
    {
        var content = ReadClientFile("components/MissionControl.tsx");
        content.Should().Contain("Impact at a glance");
    }

    [Fact]
    public void DashboardApp_HidesApproverStatsForNonApprovers()
    {
        var content = ReadClientFile("apps/DashboardApp.tsx");
        content.Should().Contain("Track your change requests");
        content.Should().Contain("isApprover ?");
    }

    [Fact]
    public void OnboardingCodeStep_NotesItOwnership()
    {
        var content = ReadClientFile("components/OnboardingAdvancedSteps.tsx");
        content.Should().Contain("Usually done by IT");
    }

    [Fact]
    public void ProductTour_IncludesIntakeCopilotStep()
    {
        var content = File.ReadAllText(Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/wwwroot/js/site.js"));
        content.Should().Contain("data-tour=\"intake-copilot\"");
        content.Should().Contain("Describe your need");
    }

    private static string ReadClientFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/ClientApp/src",
            relativePath));
    }

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
