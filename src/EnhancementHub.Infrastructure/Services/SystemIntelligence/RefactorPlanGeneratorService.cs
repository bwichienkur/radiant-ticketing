using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class RefactorPlanGeneratorService : IRefactorPlanGenerator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefactorPlanGeneratorService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RefactorPlanGeneratorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RefactorPlanGeneratorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RefactorPlanResult> GenerateAsync(
        string targetDescription,
        Guid? enhancementRequestId,
        Guid? databaseConnectionId,
        Guid? repositoryId,
        RefactorBlastRadiusResult? blastRadius,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("OpenAI API key not configured; using deterministic refactor plan.");
            return CreateMockPlan(targetDescription, blastRadius);
        }

        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var blastRadiusSummary = blastRadius is null
            ? "No blast radius analysis provided."
            : JsonSerializer.Serialize(blastRadius, JsonOptions);

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
                        You are a database migration architect. Return JSON with:
                        title (string), targetDescription (string), riskLevel (Low|Medium|High|Critical),
                        confidenceScore (number 0-1), migrationSteps (array of { order, description, sqlScript, rollbackScript }).
                        """
                },
                new
                {
                    role = "user",
                    content = $"""
                        Target change: {targetDescription}
                        EnhancementRequestId: {enhancementRequestId}
                        DatabaseConnectionId: {databaseConnectionId}
                        RepositoryId: {repositoryId}
                        Blast radius: {blastRadiusSummary}
                        """
                }
            }
        };

        var client = _httpClientFactory.CreateClient(InfrastructureServiceExtensions.OpenAiHttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            return CreateMockPlan(targetDescription, blastRadius);
        }

        var parsed = JsonSerializer.Deserialize<RefactorPlanAiResponse>(content, JsonOptions);
        if (parsed is null)
        {
            return CreateMockPlan(targetDescription, blastRadius);
        }

        return new RefactorPlanResult
        {
            Title = parsed.Title ?? $"Refactor: {targetDescription}",
            TargetDescription = parsed.TargetDescription ?? targetDescription,
            MigrationSteps = parsed.MigrationSteps ?? [],
            RiskLevel = Enum.TryParse<RiskLevel>(parsed.RiskLevel, true, out var risk) ? risk : RiskLevel.Medium,
            ConfidenceScore = parsed.ConfidenceScore,
            GeneratedByAi = true
        };
    }

    private static RefactorPlanResult CreateMockPlan(string targetDescription, RefactorBlastRadiusResult? blastRadius) =>
        new()
        {
            Title = $"Refactor plan: {targetDescription}",
            TargetDescription = targetDescription,
            RiskLevel = blastRadius?.TotalAffectedComponents > 5 ? RiskLevel.High : RiskLevel.Medium,
            ConfidenceScore = 0.6,
            GeneratedByAi = false,
            MigrationSteps =
            [
                new MigrationStepDto
                {
                    Order = 1,
                    Description = "Review blast radius and create backup.",
                    SqlScript = "-- backup script placeholder"
                },
                new MigrationStepDto
                {
                    Order = 2,
                    Description = $"Apply schema change for: {targetDescription}",
                    SqlScript = "-- migration script placeholder",
                    RollbackScript = "-- rollback script placeholder"
                }
            ]
        };

    private sealed class RefactorPlanAiResponse
    {
        public string? Title { get; set; }
        public string? TargetDescription { get; set; }
        public string? RiskLevel { get; set; }
        public double ConfidenceScore { get; set; }
        public List<MigrationStepDto>? MigrationSteps { get; set; }
    }
}
