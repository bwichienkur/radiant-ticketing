using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class AnalysisFinding : BaseEntity
{
    public Guid EnhancementAnalysisId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public bool IsAiSuggested { get; set; }
    public bool IsHumanApproved { get; set; }

    public EnhancementAnalysis EnhancementAnalysis { get; set; } = null!;
}
