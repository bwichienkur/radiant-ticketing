using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class AffectedApplication : BaseEntity
{
    public Guid EnhancementAnalysisId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ImpactDescription { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }

    public EnhancementAnalysis EnhancementAnalysis { get; set; } = null!;
    public Application Application { get; set; } = null!;
}
