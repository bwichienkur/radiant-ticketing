using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase58PostDiligenceTests
{
    [Fact]
    public void ApiVersioningDoc_ExistsWithV1Policy()
    {
        var doc = File.ReadAllText(GetPath("docs/API_VERSIONING.md"));
        doc.Should().Contain("/api/v1");
        doc.Should().Contain("Deprecation policy");
    }

    [Fact]
    public void ProductScorecard_ReflectsPhase57Completion()
    {
        var scorecard = File.ReadAllText(GetPath("docs/PRODUCT_SCORECARD.md"));
        scorecard.Should().Contain("Phase 57");
        scorecard.Should().Contain("8.5");
        scorecard.Should().Contain("SCIM");
    }

    [Fact]
    public void DueDiligenceRoadmap_QuickWinsComplete()
    {
        var roadmap = File.ReadAllText(GetPath("docs/DUE_DILIGENCE_ROADMAP.md"));
        roadmap.Should().Contain("Dashboard widgets linking to drift + pending approvals");
        roadmap.Should().NotContain("- [ ] Recalibrate");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}

public sealed class Phase59AccessibilityTests
{
    [Fact]
    public void AccessibilitySpec_UsesAxeOnFiveFlows()
    {
        var spec = File.ReadAllText(GetPath("tests/e2e/accessibility.spec.ts"));
        spec.Should().Contain("@axe-core/playwright");
        spec.Should().Contain("dashboard");
        spec.Should().Contain("approval queue");
        spec.Should().Contain("request detail");
        spec.Should().Contain("create request");
        spec.Should().Contain("system map");
    }

    [Fact]
    public void AdminJobsAndTeams_UsePageHeaderPartial()
    {
        var jobs = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Jobs.cshtml"));
        var teams = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Teams.cshtml"));
        jobs.Should().Contain("_PageHeader");
        teams.Should().Contain("_PageHeader");
    }

    [Fact]
    public void SiteCss_IncludesNarrowMobileApprovalQueueRules()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/site.css"));
        css.Should().Contain("575.98px");
        css.Should().Contain("approval-quick-actions");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}

public sealed class Phase60FeatureFlagsTests
{
    [Fact]
    public void FeatureService_InterfaceAndImplementationExist()
    {
        File.Exists(GetPath("src/EnhancementHub.Application/Abstractions/IFeatureService.cs")).Should().BeTrue();
        var impl = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Services/ConfigurationFeatureService.cs"));
        impl.Should().Contain("IFeatureService");
        impl.Should().Contain("Features");
    }

    [Fact]
    public void PlatformRuntimeStatus_IncludesFeatureFlags()
    {
        var model = File.ReadAllText(GetPath("src/EnhancementHub.Application/Abstractions/Models/PlatformRuntimeStatus.cs"));
        model.Should().Contain("FeatureFlags");
        var service = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Services/PlatformRuntimeStatusService.cs"));
        service.Should().Contain("FeatureFlags.IntakeCopilot");
    }

    [Fact]
    public void AppSettings_DefinesFeatureFlags()
    {
        var settings = File.ReadAllText(GetPath("src/EnhancementHub.Web/appsettings.json"));
        settings.Should().Contain("\"Features\"");
        settings.Should().Contain("FeedbackWidget");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
