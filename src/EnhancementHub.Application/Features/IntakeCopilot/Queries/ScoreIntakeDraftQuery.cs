using EnhancementHub.Application.Features.IntakeCopilot.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.IntakeCopilot.Queries;

public sealed record ScoreIntakeDraftQuery(ScoreIntakeDraftRequest Draft)
    : IRequest<IntakeQualityScoreDto>;

public sealed class ScoreIntakeDraftQueryHandler : IRequestHandler<ScoreIntakeDraftQuery, IntakeQualityScoreDto>
{
    public Task<IntakeQualityScoreDto> Handle(ScoreIntakeDraftQuery request, CancellationToken cancellationToken)
    {
        var draft = request.Draft;
        var missing = new List<string>();
        var suggestions = new List<string>();
        var score = 100;

        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            missing.Add("Title");
            suggestions.Add("Add a short title that names the business change.");
            score -= 25;
        }
        else if (draft.Title.Trim().Length < 8)
        {
            suggestions.Add("Expand the title so approvers can recognize the request in the queue.");
            score -= 5;
        }

        if (string.IsNullOrWhiteSpace(draft.BusinessDescription))
        {
            missing.Add("Business problem");
            suggestions.Add("Describe the business problem and why it matters today.");
            score -= 30;
        }
        else if (draft.BusinessDescription.Trim().Length < 40)
        {
            suggestions.Add("Add more context to the business problem (who is affected, current pain).");
            score -= 15;
        }

        if (string.IsNullOrWhiteSpace(draft.DesiredOutcome))
        {
            missing.Add("Desired outcome");
            suggestions.Add("State what success looks like in concrete, testable terms.");
            score -= 25;
        }
        else if (draft.DesiredOutcome.Trim().Length < 20)
        {
            suggestions.Add("Clarify the desired outcome with an example or measurable result.");
            score -= 10;
        }

        if (!draft.TargetApplicationId.HasValue)
        {
            missing.Add("Affected system");
            suggestions.Add("Select which application or system is impacted.");
            score -= 10;
        }

        if (string.IsNullOrWhiteSpace(draft.Department))
        {
            suggestions.Add("Add a department so routing and reporting stay accurate.");
            score -= 5;
        }

        if (string.IsNullOrWhiteSpace(draft.Priority))
        {
            missing.Add("Priority");
            score -= 5;
        }

        score = Math.Clamp(score, 0, 100);
        var ready = missing.Count == 0 && score >= 70;

        return Task.FromResult(new IntakeQualityScoreDto(
            score,
            ready,
            missing,
            suggestions));
    }
}
