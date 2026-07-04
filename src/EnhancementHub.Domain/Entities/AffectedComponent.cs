using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class AffectedComponent : BaseEntity
{
    public Guid EnhancementAnalysisId { get; set; }
    public Guid? IndexedFileId { get; set; }
    public string ComponentPath { get; set; } = string.Empty;
    public ComponentType ComponentType { get; set; }
    public string ImpactDescription { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }

    public EnhancementAnalysis EnhancementAnalysis { get; set; } = null!;
    public IndexedFile? IndexedFile { get; set; }
}
