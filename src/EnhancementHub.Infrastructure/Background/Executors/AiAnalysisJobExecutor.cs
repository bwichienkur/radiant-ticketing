using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class AiAnalysisJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiAnalysisJobExecutor> _logger;

    public AiAnalysisJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<AiAnalysisJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiAnalysisService>();
        var riskScoring = scope.ServiceProvider.GetRequiredService<IRiskScoringService>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var pending = await dbContext.EnhancementRequests
            .Include(r => r.TargetApplication)
            .ThenInclude(a => a!.Profiles)
            .Where(r => r.Status == EnhancementRequestStatus.Submitted
                || r.Status == EnhancementRequestStatus.AiAnalyzing)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var request in pending)
        {
            await ProcessRequestAsync(dbContext, aiService, riskScoring, audit, notificationService, request, cancellationToken);
        }
    }

    private async Task ProcessRequestAsync(
        IEnhancementHubDbContext dbContext,
        IAiAnalysisService aiService,
        IRiskScoringService riskScoring,
        IAuditService audit,
        INotificationService notificationService,
        EnhancementRequest request,
        CancellationToken cancellationToken)
    {
        request.Status = EnhancementRequestStatus.AiAnalyzing;
        request.UpdatedAt = DateTime.UtcNow;

        var latestVersion = await dbContext.EnhancementAnalyses
            .Where(a => a.EnhancementRequestId == request.Id)
            .Select(a => (int?)a.Version)
            .MaxAsync(cancellationToken) ?? 0;

        var analysis = new EnhancementAnalysis
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.Id,
            Version = latestVersion + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.EnhancementAnalyses.Add(analysis);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var profile = request.TargetApplication?.Profiles.FirstOrDefault();
            var applicationContext = ApplicationAnalysisContextFormatter.Format(
                profile,
                request.TargetApplication?.DeploymentNotes);
            var description = $"{request.BusinessDescription}\n\nDesired outcome: {request.DesiredOutcome}";
            var result = await aiService.AnalyzeEnhancementAsync(
                request.Id,
                request.Title,
                description,
                repositoryContext: null,
                applicationContext,
                cancellationToken);

            var riskScore = riskScoring.CalculateRiskScore(result, null);
            analysis.FeatureSummary = result.Summary;
            analysis.BusinessRequirement = request.BusinessDescription;
            analysis.TechnicalRequirements = string.Join(Environment.NewLine, result.Recommendations);
            analysis.RiskLevel = riskScoring.MapToRiskLevel(riskScore);
            analysis.RiskExplanation = string.Join(Environment.NewLine, result.Risks);
            analysis.TestingPlan = "Validate impacted areas with automated regression and integration tests.";
            analysis.ConfidenceScore = result.IsMock ? 0.6 : 0.85;
            analysis.NeedsClarification = result.Risks.Any(r =>
                r.Contains("clarification", StringComparison.OrdinalIgnoreCase));
            analysis.UpdatedAt = DateTime.UtcNow;

            foreach (var area in result.ImpactedAreas)
            {
                dbContext.AnalysisFindings.Add(new AnalysisFinding
                {
                    Id = Guid.NewGuid(),
                    EnhancementAnalysisId = analysis.Id,
                    Category = "Impact",
                    Title = area,
                    Description = area,
                    ConfidenceScore = analysis.ConfidenceScore,
                    IsAiSuggested = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            dbContext.RiskAssessments.Add(new RiskAssessment
            {
                Id = Guid.NewGuid(),
                EnhancementAnalysisId = analysis.Id,
                RiskLevel = analysis.RiskLevel,
                Explanation = analysis.RiskExplanation,
                ConfidenceScore = analysis.ConfidenceScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            dbContext.AiPromptRuns.Add(new AiPromptRun
            {
                Id = Guid.NewGuid(),
                EnhancementRequestId = request.Id,
                EnhancementAnalysisId = analysis.Id,
                WorkflowStep = AiWorkflowStep.EnhancementAnalysis.ToString(),
                PromptVersion = "v1",
                ModelName = result.ModelUsed,
                SystemPrompt = "Enhancement analysis",
                UserPrompt = request.Title,
                StructuredResponse = JsonSerializer.Serialize(result),
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                TotalTokens = result.TotalTokens,
                EstimatedCostUsd = result.EstimatedCostUsd,
                Status = AiRunStatus.Completed,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            request.Status = EnhancementRequestStatus.PendingApproval;
            request.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            await audit.LogAsync(
                "EnhancementAnalyzed",
                nameof(EnhancementRequest),
                request.Id,
                result.Summary,
                cancellationToken);

            var submitterTenantId = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == request.SubmittedByUserId)
                .Select(u => u.TenantId)
                .FirstOrDefaultAsync(cancellationToken);

            await notificationService.NotifyApproversOfPendingApprovalAsync(
                request.Id,
                request.Title,
                submitterTenantId,
                cancellationToken);

            await notificationService.NotifySubmitterOfAnalysisCompleteAsync(
                request.SubmittedByUserId,
                request.Id,
                request.Title,
                submitterTenantId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            analysis.AmbiguityNotes = ex.Message;
            request.Status = EnhancementRequestStatus.Submitted;
            request.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Failed to analyze enhancement request {RequestId}", request.Id);
        }
    }
}
