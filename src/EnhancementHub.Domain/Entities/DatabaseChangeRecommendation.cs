using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class DatabaseChangeRecommendation : BaseEntity
{
    public Guid EnhancementAnalysisId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool MigrationRequired { get; set; }
    public double ConfidenceScore { get; set; }
    public bool IsAiSuggested { get; set; }

    public EnhancementAnalysis EnhancementAnalysis { get; set; } = null!;
}
