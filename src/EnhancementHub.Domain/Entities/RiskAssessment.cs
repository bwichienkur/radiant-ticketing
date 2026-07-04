using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class RiskAssessment : BaseEntity
{
    public Guid EnhancementAnalysisId { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string? SecurityConcerns { get; set; }
    public string? PerformanceConcerns { get; set; }
    public string? Explanation { get; set; }
    public double ConfidenceScore { get; set; }

    public EnhancementAnalysis EnhancementAnalysis { get; set; } = null!;
}
