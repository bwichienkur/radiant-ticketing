using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Infrastructure.Services;

public sealed class RiskScoringService : IRiskScoringService
{
    public double CalculateRiskScore(AiAnalysisResult analysis, RepositoryScanResult? repositoryContext)
    {
        var score = 0.2;

        score += Math.Min(analysis.Risks.Count * 0.1, 0.4);
        score += Math.Min(analysis.ImpactedAreas.Count * 0.05, 0.2);

        if (analysis.EstimatedEffortHours > 40)
        {
            score += 0.15;
        }
        else if (analysis.EstimatedEffortHours > 16)
        {
            score += 0.08;
        }

        if (repositoryContext is not null)
        {
            if (repositoryContext.DbContextTypes.Count > 0
                && analysis.ImpactedAreas.Any(a => a.Contains("database", StringComparison.OrdinalIgnoreCase)
                    || a.Contains("migration", StringComparison.OrdinalIgnoreCase)))
            {
                score += 0.15;
            }

            if (repositoryContext.Controllers.Count > 0
                && analysis.ImpactedAreas.Any(a => a.Contains("api", StringComparison.OrdinalIgnoreCase)))
            {
                score += 0.05;
            }
        }

        return Math.Round(Math.Clamp(score, 0, 1), 2);
    }

    public RiskLevel MapToRiskLevel(double score) => score switch
    {
        >= 0.75 => RiskLevel.Critical,
        >= 0.5 => RiskLevel.High,
        >= 0.25 => RiskLevel.Medium,
        _ => RiskLevel.Low
    };
}
