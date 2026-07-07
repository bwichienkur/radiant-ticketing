using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase72GateVerificationTests
{
    [Fact]
    public void Gate85VerificationDoc_ListsAllExitGates()
    {
        var doc = File.ReadAllText(GetPath("docs/GATE_85_VERIFICATION.md"));
        doc.Should().Contain("Overall product quality");
        doc.Should().Contain("SPA navigation without full reload");
        doc.Should().Contain("Design partners with measured ROI");
        doc.Should().Contain("Published case study");
        doc.Should().Contain("axe serious violations");
        doc.Should().Contain("Postgres k6 p95");
        doc.Should().Contain("DriftAutopilot");
        doc.Should().Contain("Tenant branding");
    }

    [Fact]
    public void UxHeuristicReviewDoc_IncludesFiveReviewerFindings()
    {
        var doc = File.ReadAllText(GetPath("docs/UX_HEURISTIC_REVIEW.md"));
        doc.Should().Contain("Reviewer 1");
        doc.Should().Contain("Reviewer 5");
        doc.Should().Contain("Nielsen");
    }

    [Fact]
    public void ProductScorecard_ReflectsWave4Completion()
    {
        var scorecard = File.ReadAllText(GetPath("docs/PRODUCT_SCORECARD.md"));
        scorecard.Should().Contain("Wave 4");
        scorecard.Should().Contain("8.5");
    }

    [Fact]
    public void PhasesDoc_MarksWave4Complete()
    {
        var phases = File.ReadAllText(GetPath("docs/PHASES.md"));
        phases.Should().Contain("Phase 70");
        phases.Should().Contain("Phase 71");
        phases.Should().Contain("Phase 72");
        phases.Should().Contain("Complete");
    }

    [Fact]
    public void Roadmap85_MarksWave4Complete()
    {
        var roadmap = File.ReadAllText(GetPath("docs/ROADMAP_85.md"));
        roadmap.Should().Contain("Wave 4");
        roadmap.Should().Contain("complete");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
