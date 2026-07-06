using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Common;

public static class DriftRiskScoringHelper
{
    public static async Task<RiskLevel> ResolveRiskLevelAsync(
        IEnhancementHubDbContext dbContext,
        IRiskScoringService riskScoring,
        EnhancementRequest request,
        AiAnalysisResult result,
        CancellationToken cancellationToken)
    {
        var riskScore = riskScoring.CalculateRiskScore(result, null);
        var baseLevel = riskScoring.MapToRiskLevel(riskScore);
        var findingId = DriftRequestProvenance.TryParseFindingId(request.SupportingNotes);
        if (findingId is null)
        {
            return baseLevel;
        }

        var severity = await dbContext.SchemaDriftFindings
            .AsNoTracking()
            .Where(f => f.Id == findingId.Value)
            .Select(f => (DriftSeverity?)f.Severity)
            .FirstOrDefaultAsync(cancellationToken);

        return riskScoring.ApplyDriftSeverityBoost(baseLevel, severity);
    }
}
