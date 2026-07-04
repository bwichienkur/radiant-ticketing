using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class ApiChangeRecommendation : BaseEntity
{
    public Guid EnhancementAnalysisId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public bool IsAiSuggested { get; set; }

    public EnhancementAnalysis EnhancementAnalysis { get; set; } = null!;
}
