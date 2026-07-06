using EnhancementHub.Application.Features.IntakeCopilot.Dtos;
using EnhancementHub.Application.Features.IntakeCopilot.Queries;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class IntakeQualityScoringTests
{
    [Fact]
    public async Task ScoreIntakeDraft_MarksIncompleteDraftNotReady()
    {
        var handler = new ScoreIntakeDraftQueryHandler();
        var result = await handler.Handle(
            new ScoreIntakeDraftQuery(new ScoreIntakeDraftRequest(
                Title: "",
                BusinessDescription: "Too short",
                DesiredOutcome: "",
                Priority: "Medium",
                TargetApplicationId: null,
                Department: null,
                SupportingNotes: null)),
            CancellationToken.None);

        result.ReadyToSubmit.Should().BeFalse();
        result.MissingFields.Should().Contain("Title");
        result.MissingFields.Should().Contain("Desired outcome");
        result.Score.Should().BeLessThan(70);
    }

    [Fact]
    public async Task ScoreIntakeDraft_MarksCompleteDraftReady()
    {
        var handler = new ScoreIntakeDraftQueryHandler();
        var result = await handler.Handle(
            new ScoreIntakeDraftQuery(new ScoreIntakeDraftRequest(
                Title: "Track cancellation reasons",
                BusinessDescription: "Managers cannot explain why orders are cancelled during monthly reviews.",
                DesiredOutcome: "Managers can run a monthly report on cancellation reasons by channel.",
                Priority: "High",
                TargetApplicationId: Guid.NewGuid(),
                Department: "Operations",
                SupportingNotes: "Needed for compliance reporting.")),
            CancellationToken.None);

        result.ReadyToSubmit.Should().BeTrue();
        result.MissingFields.Should().BeEmpty();
        result.Score.Should().BeGreaterThanOrEqualTo(70);
    }
}
