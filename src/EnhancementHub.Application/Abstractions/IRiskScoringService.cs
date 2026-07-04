using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface IRiskScoringService
{
    double CalculateRiskScore(AiAnalysisResult analysis, RepositoryScanResult? repositoryContext);
    RiskLevel MapToRiskLevel(double score);
}
