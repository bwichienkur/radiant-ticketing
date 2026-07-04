using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class AiPromptRun : BaseEntity
{
    public Guid EnhancementRequestId { get; set; }
    public Guid? EnhancementAnalysisId { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string? RawResponse { get; set; }
    public string? StructuredResponse { get; set; }
    public AiRunStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public EnhancementRequest EnhancementRequest { get; set; } = null!;
    public EnhancementAnalysis? EnhancementAnalysis { get; set; }
    public ICollection<RetrievedContextItem> RetrievedContextItems { get; set; } = new List<RetrievedContextItem>();
}
