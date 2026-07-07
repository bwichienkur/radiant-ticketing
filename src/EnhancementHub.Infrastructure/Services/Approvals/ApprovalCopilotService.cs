using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Approvals.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Approvals;

public sealed class ApprovalCopilotService : IApprovalCopilotService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IEnhancementRequestAccessService _accessService;
    private readonly IChatCompletionService _chatCompletion;
    private readonly IFeatureService _featureService;
    private readonly ILogger<ApprovalCopilotService> _logger;

    public ApprovalCopilotService(
        IEnhancementHubDbContext dbContext,
        IEnhancementRequestAccessService accessService,
        IChatCompletionService chatCompletion,
        IFeatureService featureService,
        ILogger<ApprovalCopilotService> logger)
    {
        _dbContext = dbContext;
        _accessService = accessService;
        _chatCompletion = chatCompletion;
        _featureService = featureService;
        _logger = logger;
    }

    public async Task<ApprovalRecommendationDto> GetRecommendationAsync(
        Guid enhancementRequestId,
        CancellationToken cancellationToken = default)
    {
        await _accessService.GetAccessibleRequestAsync(enhancementRequestId, cancellationToken);

        var enhancementRequest = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == enhancementRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), enhancementRequestId);

        var latestAnalysis = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Where(a => a.EnhancementRequestId == enhancementRequestId)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (!_featureService.IsEnabled(FeatureFlags.ApprovalCopilot) || !_chatCompletion.IsConfigured)
        {
            return BuildHeuristic(enhancementRequestId, enhancementRequest, latestAnalysis, "Heuristic");
        }

        try
        {
            var llmResult = await TryLlmRecommendationAsync(
                enhancementRequestId,
                enhancementRequest,
                latestAnalysis,
                cancellationToken);

            if (llmResult is not null)
            {
                return llmResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Approval copilot LLM failed for request {RequestId}", enhancementRequestId);
        }

        return BuildHeuristic(enhancementRequestId, enhancementRequest, latestAnalysis, "HeuristicFallback");
    }

    private async Task<ApprovalRecommendationDto?> TryLlmRecommendationAsync(
        Guid enhancementRequestId,
        EnhancementRequest enhancementRequest,
        EnhancementAnalysis? latestAnalysis,
        CancellationToken cancellationToken)
    {
        var userPrompt = $"""
            Enhancement request:
            Title: {enhancementRequest.Title}
            Priority: {enhancementRequest.Priority}
            Business description: {enhancementRequest.BusinessDescription}
            Desired outcome: {enhancementRequest.DesiredOutcome}
            Supporting notes: {enhancementRequest.SupportingNotes ?? "None"}

            Latest AI analysis:
            Available: {(latestAnalysis is not null ? "Yes" : "No")}
            Risk level: {latestAnalysis?.RiskLevel.ToString() ?? "Unknown"}
            Confidence: {latestAnalysis?.ConfidenceScore.ToString("P0") ?? "N/A"}
            Needs clarification: {latestAnalysis?.NeedsClarification.ToString() ?? "Unknown"}
            Feature summary: {latestAnalysis?.FeatureSummary ?? "None"}
            Risk explanation: {latestAnalysis?.RiskExplanation ?? "None"}
            """;

        var response = await _chatCompletion.CompleteAsync(
            new ChatCompletionRequest
            {
                WorkflowStep = AiWorkflowStep.ApprovalCopilot,
                SystemPrompt = """
                    You are EnhancementHub Approval Copilot. Recommend one approval action for an enterprise architect.
                    Return ONLY valid JSON with:
                    {
                      "recommendation": "Approve|ApproveWithCare|Caution|RequestClarification|Reject",
                      "summary": "2-4 sentences explaining why, referencing risk and confidence"
                    }
                    Be conservative on critical risk or low confidence. Prefer RequestClarification when analysis is missing.
                    """,
                UserPrompt = userPrompt,
                JsonResponse = true
            },
            cancellationToken);

        if (response.IsMock)
        {
            return null;
        }

        var parsed = JsonSerializer.Deserialize<LlmApprovalResponse>(response.Content, JsonOptions);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Recommendation) || string.IsNullOrWhiteSpace(parsed.Summary))
        {
            return null;
        }

        var recommendation = NormalizeRecommendation(parsed.Recommendation);

        _dbContext.AiPromptRuns.Add(new AiPromptRun
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = enhancementRequestId,
            EnhancementAnalysisId = latestAnalysis?.Id,
            WorkflowStep = AiWorkflowStep.ApprovalCopilot.ToString(),
            PromptVersion = "v1",
            ModelName = response.ModelUsed,
            SystemPrompt = "Approval copilot recommendation",
            UserPrompt = userPrompt,
            StructuredResponse = response.Content,
            PromptTokens = response.PromptTokens,
            CompletionTokens = response.CompletionTokens,
            TotalTokens = response.TotalTokens,
            EstimatedCostUsd = response.EstimatedCostUsd,
            Status = AiRunStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ApprovalRecommendationDto(
            enhancementRequestId,
            recommendation,
            parsed.Summary.Trim(),
            latestAnalysis?.RiskLevel,
            latestAnalysis?.ConfidenceScore,
            latestAnalysis?.NeedsClarification ?? false,
            "Llm");
    }

    private static string NormalizeRecommendation(string raw)
    {
        return raw.Trim() switch
        {
            "Approve" => "Approve",
            "ApproveWithCare" => "ApproveWithCare",
            "Caution" => "Caution",
            "RequestClarification" => "RequestClarification",
            "Reject" => "Reject",
            _ => "Caution"
        };
    }

    private static ApprovalRecommendationDto BuildHeuristic(
        Guid enhancementRequestId,
        EnhancementRequest enhancementRequest,
        EnhancementAnalysis? latestAnalysis,
        string source)
    {
        var recommendation = BuildRecommendation(enhancementRequest.Priority, latestAnalysis);
        var summary = BuildSummary(enhancementRequest, latestAnalysis, recommendation);

        return new ApprovalRecommendationDto(
            enhancementRequestId,
            recommendation,
            summary,
            latestAnalysis?.RiskLevel,
            latestAnalysis?.ConfidenceScore,
            latestAnalysis?.NeedsClarification ?? false,
            source);
    }

    private static string BuildRecommendation(string priority, EnhancementAnalysis? analysis)
    {
        if (analysis is null)
        {
            return "RequestClarification";
        }

        if (analysis.NeedsClarification || !string.IsNullOrWhiteSpace(analysis.AmbiguityNotes))
        {
            return "RequestClarification";
        }

        return analysis.RiskLevel switch
        {
            RiskLevel.Critical when analysis.ConfidenceScore < 0.75 => "Reject",
            RiskLevel.Critical => "Caution",
            RiskLevel.High when analysis.ConfidenceScore < 0.65 => "Caution",
            RiskLevel.High => "ApproveWithCare",
            _ when string.Equals(priority, "Critical", StringComparison.OrdinalIgnoreCase)
                && analysis.RiskLevel <= RiskLevel.Medium => "ApproveWithCare",
            _ => "Approve"
        };
    }

    private static string BuildSummary(
        EnhancementRequest request,
        EnhancementAnalysis? analysis,
        string recommendation)
    {
        if (analysis is null)
        {
            return "AI analysis is not available yet. Wait for analysis to finish or ask the requester for more detail before deciding.";
        }

        var confidence = $"{analysis.ConfidenceScore:P0}";
        var risk = analysis.RiskLevel.ToString();
        var outcomeSnippet = Truncate(request.DesiredOutcome, 120);
        var analysisSnippet = Truncate(analysis.FeatureSummary ?? request.BusinessDescription, 160);

        return recommendation switch
        {
            "Approve" =>
                $"This request is ready to approve. AI assessed {risk} risk at {confidence} confidence. {analysisSnippet} Success looks like: {outcomeSnippet}. No clarification flags were raised.",
            "ApproveWithCare" =>
                $"Lean toward approval with extra scrutiny. AI assessed {risk} risk at {confidence} confidence. {analysisSnippet} Verify downstream impact and rollout plan before approving.",
            "Caution" =>
                $"Proceed carefully. Critical/high-impact change at {confidence} confidence ({risk} risk). {analysisSnippet} Confirm blast radius, testing, and rollback coverage with the architect team.",
            "RequestClarification" =>
                $"Ask for more information before approving. AI flagged ambiguity at {confidence} confidence ({risk} risk). {analysisSnippet} Request clearer scope, owners, or acceptance criteria from the submitter.",
            "Reject" =>
                $"Consider declining unless scope is narrowed. Critical risk with only {confidence} confidence suggests the request is under-specified or too broad. {analysisSnippet} Reject or send back for a smaller, testable change.",
            _ =>
                $"Review manually. AI assessed {risk} risk at {confidence} confidence. {analysisSnippet}"
        };
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "No detail provided.";
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : $"{trimmed[..(maxLength - 1)]}…";
    }

    private sealed class LlmApprovalResponse
    {
        public string Recommendation { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
