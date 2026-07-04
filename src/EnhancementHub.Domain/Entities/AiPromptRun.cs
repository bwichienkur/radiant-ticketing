using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class AiPromptRun : BaseEntity
{
    public Guid? EnhancementRequestId { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid? EnhancementAnalysisId { get; set; }
    public string WorkflowStep { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string? RawResponse { get; set; }
    public string? StructuredResponse { get; set; }
    public AiRunStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public EnhancementRequest? EnhancementRequest { get; set; }
    public EnhancementAnalysis? EnhancementAnalysis { get; set; }
    public ICollection<RetrievedContextItem> RetrievedContextItems { get; set; } = new List<RetrievedContextItem>();
}
