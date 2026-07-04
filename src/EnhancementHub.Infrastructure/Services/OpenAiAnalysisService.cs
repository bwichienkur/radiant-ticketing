using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class OpenAiAnalysisService : IAiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly PromptSanitizer _promptSanitizer;
    private readonly AiResponseValidator _validator;
    private readonly IRiskScoringService _riskScoring;
    private readonly ILogger<OpenAiAnalysisService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OpenAiAnalysisService(
        HttpClient httpClient,
        IConfiguration configuration,
        PromptSanitizer promptSanitizer,
        AiResponseValidator validator,
        IRiskScoringService riskScoring,
        ILogger<OpenAiAnalysisService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
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
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("OpenAI API key not configured; using deterministic mock analysis for {RequestId}", enhancementRequestId);
            return CreateMockAnalysis(title, description);
        }

        var prompt = _promptSanitizer.BuildStructuredPrompt(title, description, repositoryContext);
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        var requestBody = new
        {
            model,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = """
                        You are a software architecture analyst. Return JSON with:
                        summary (string), impactedAreas (string[]), recommendations (string[]),
                        risks (string[]), estimatedEffortHours (number), modelUsed (string).
                        """
                },
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI request failed ({Status}): {Body}", response.StatusCode, responseJson);
            return CreateMockAnalysis(title, description);
        }

        using var doc = JsonDocument.Parse(responseJson);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (!_validator.TryValidate(content ?? string.Empty, out var result, out var error))
        {
            _logger.LogWarning("AI response validation failed: {Error}", error);
            return CreateMockAnalysis(title, description);
        }

        result!.ModelUsed = model;
        result.IsMock = false;
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
