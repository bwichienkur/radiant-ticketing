using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class OpenAiAnalysisService : IAiAnalysisService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly PromptSanitizer _promptSanitizer;
    private readonly AiResponseValidator _validator;
    private readonly IRiskScoringService _riskScoring;
    private readonly ILogger<OpenAiAnalysisService> _logger;

    public OpenAiAnalysisService(
        IChatCompletionService chatCompletion,
        PromptSanitizer promptSanitizer,
        AiResponseValidator validator,
        IRiskScoringService riskScoring,
        ILogger<OpenAiAnalysisService> logger)
    {
        _chatCompletion = chatCompletion;
        _promptSanitizer = promptSanitizer;
        _validator = validator;
        _riskScoring = riskScoring;
        _logger = logger;
    }

    public async Task<AiAnalysisResult> AnalyzeEnhancementAsync(
        Guid enhancementRequestId,
        string title,
        string description,
        string? repositoryContext,
        string? applicationContext = null,
        CancellationToken cancellationToken = default)
    {
        if (!_chatCompletion.IsConfigured)
        {
            _logger.LogInformation(
                "AI provider not configured; using deterministic mock analysis for {RequestId}",
                enhancementRequestId);
            return CreateMockAnalysis(title, description);
        }

        var prompt = _promptSanitizer.BuildStructuredPrompt(
            title,
            description,
            repositoryContext,
            applicationContext);

        var completion = await _chatCompletion.CompleteAsync(
            new ChatCompletionRequest
            {
                WorkflowStep = AiWorkflowStep.EnhancementAnalysis,
                SystemPrompt = """
                    You are a software architecture analyst. Return JSON with:
                    summary (string), impactedAreas (string[]), recommendations (string[]),
                    risks (string[]), estimatedEffortHours (number), modelUsed (string).
                    When recommendations imply new cloud services, external APIs, or hosting changes,
                    call that out in risks or recommendations and suggest a lower-cost alternative when feasible.
                    Honor deployment and infrastructure constraints in the user prompt.
                    """,
                UserPrompt = prompt
            },
            cancellationToken);

        if (!_validator.TryValidate(completion.Content, out var result, out var error))
        {
            _logger.LogWarning("AI response validation failed: {Error}", error);
            return CreateMockAnalysis(title, description);
        }

        result!.ModelUsed = completion.ModelUsed;
        result.IsMock = false;
        result.PromptTokens = completion.PromptTokens;
        result.CompletionTokens = completion.CompletionTokens;
        result.TotalTokens = completion.TotalTokens;
        result.EstimatedCostUsd = completion.EstimatedCostUsd;
        return result;
    }

    private AiAnalysisResult CreateMockAnalysis(string title, string description)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{title}|{description}"));
        var seed = BitConverter.ToInt32(hash, 0);

        var impacted = new List<string> { "Application layer", "API endpoints" };
        if (description.Contains("database", StringComparison.OrdinalIgnoreCase))
        {
            impacted.Add("Database schema");
        }

        var risks = new List<string> { "Regression in existing workflows" };
        if (description.Length > 500)
        {
            risks.Add("Scope creep due to broad requirements");
        }

        var effort = 8 + Math.Abs(seed % 32);

        var result = new AiAnalysisResult
        {
            Summary = $"Mock analysis for '{title}': changes appear moderate with focused impact on {impacted[0]}.",
            ImpactedAreas = impacted,
            Recommendations =
            [
                "Add integration tests covering the primary user flow.",
                "Document API contract changes.",
                "Plan a phased rollout behind a feature flag."
            ],
            Risks = risks,
            EstimatedEffortHours = effort,
            ModelUsed = "deterministic-mock",
            IsMock = true
        };

        _ = _riskScoring.CalculateRiskScore(result, null);
        return result;
    }
}
