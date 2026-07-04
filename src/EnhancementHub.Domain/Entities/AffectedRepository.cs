using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class AffectedRepository : BaseEntity
{
    public Guid EnhancementAnalysisId { get; set; }
    public Guid RepositoryId { get; set; }
    public string ImpactDescription { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }

    public EnhancementAnalysis EnhancementAnalysis { get; set; } = null!;
    public Repository Repository { get; set; } = null!;
}
