using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class RefactorPlanGeneratorService : IRefactorPlanGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatCompletionService _chatCompletion;
    private readonly IPiiRedactionService _piiRedaction;
    private readonly ILogger<RefactorPlanGeneratorService> _logger;

    public RefactorPlanGeneratorService(
        IChatCompletionService chatCompletion,
        IPiiRedactionService piiRedaction,
        ILogger<RefactorPlanGeneratorService> logger)
    {
        _chatCompletion = chatCompletion;
        _piiRedaction = piiRedaction;
        _logger = logger;
    }

    public async Task<RefactorPlanResult> GenerateAsync(
        string targetDescription,
        Guid? enhancementRequestId,
        Guid? databaseConnectionId,
        Guid? repositoryId,
        RefactorBlastRadiusResult? blastRadius,
        string? infrastructureContext = null,
        CancellationToken cancellationToken = default)
    {
        if (!_chatCompletion.IsConfigured)
        {
            _logger.LogInformation("AI provider not configured; using deterministic refactor plan.");
            return CreateMockPlan(targetDescription, blastRadius);
        }

        var blastRadiusSummary = blastRadius is null
            ? "No blast radius analysis provided."
            : JsonSerializer.Serialize(blastRadius, JsonOptions);

        var infrastructureSection = string.IsNullOrWhiteSpace(infrastructureContext)
            ? string.Empty
            : $"\nInfrastructure context:\n{infrastructureContext}\n";

        var userPrompt = _piiRedaction.Redact($"""
            Target change: {targetDescription}
            EnhancementRequestId: {enhancementRequestId}
            DatabaseConnectionId: {databaseConnectionId}
            RepositoryId: {repositoryId}
            Blast radius: {blastRadiusSummary}{infrastructureSection}
            """);

        var completion = await _chatCompletion.CompleteAsync(
            new ChatCompletionRequest
            {
                WorkflowStep = AiWorkflowStep.RefactorPlan,
                SystemPrompt = """
                    You are a database migration architect. Return JSON with:
                    title (string), targetDescription (string), riskLevel (Low|Medium|High|Critical),
                    confidenceScore (number 0-1), migrationSteps (array of { order, description, sqlScript, rollbackScript }).
                    When migration options imply new cloud database tiers or services, note tradeoffs in step descriptions
                    and prefer approaches compatible with the infrastructure context. Honor deployment constraints.
                    """,
                UserPrompt = userPrompt
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(completion.Content))
        {
            return CreateMockPlan(targetDescription, blastRadius);
        }

        var parsed = JsonSerializer.Deserialize<RefactorPlanAiResponse>(completion.Content, JsonOptions);
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
            GeneratedByAi = true,
            ModelUsed = completion.ModelUsed,
            PromptTokens = completion.PromptTokens,
            CompletionTokens = completion.CompletionTokens,
            TotalTokens = completion.TotalTokens,
            EstimatedCostUsd = completion.EstimatedCostUsd
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
            ModelUsed = "deterministic-mock",
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
