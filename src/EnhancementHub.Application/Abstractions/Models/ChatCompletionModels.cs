using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions.Models;

public sealed class ChatCompletionRequest
{
    public AiWorkflowStep WorkflowStep { get; init; }
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public bool JsonResponse { get; init; } = true;
}

public sealed class ChatCompletionResponse
{
    public string Content { get; init; } = string.Empty;
    public string ModelUsed { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
    public decimal EstimatedCostUsd { get; init; }
    public bool IsMock { get; init; }
}
