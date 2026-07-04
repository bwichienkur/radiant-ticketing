using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions.Models;

public sealed class DocumentationBundle
{
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string MarkdownDocumentation { get; set; } = string.Empty;
    public string MermaidErd { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RefactorPlanResult
{
    public string Title { get; set; } = string.Empty;
    public string TargetDescription { get; set; } = string.Empty;
    public IReadOnlyList<MigrationStepDto> MigrationSteps { get; set; } = Array.Empty<MigrationStepDto>();
    public RiskLevel RiskLevel { get; set; }
    public double ConfidenceScore { get; set; }
    public bool GeneratedByAi { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
}

public sealed class MigrationStepDto
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SqlScript { get; set; }
    public string? RollbackScript { get; set; }
}

public sealed class OnPremAgentRegistration
{
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ApplicationId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? ApiKey { get; set; }
}
