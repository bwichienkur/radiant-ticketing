namespace EnhancementHub.Application.Abstractions.Models;

public sealed class AiAnalysisResult
{
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<string> ImpactedAreas { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Risks { get; set; } = Array.Empty<string>();
    public double EstimatedEffortHours { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public bool IsMock { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
}
