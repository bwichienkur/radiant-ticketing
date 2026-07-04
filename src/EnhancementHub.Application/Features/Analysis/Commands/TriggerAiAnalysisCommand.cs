using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Analysis.Commands;

public sealed record TriggerAiAnalysisCommand(Guid EnhancementRequestId) : IRequest<EnhancementAnalysisDto>;

public sealed class TriggerAiAnalysisCommandHandler
    : IRequestHandler<TriggerAiAnalysisCommand, EnhancementAnalysisDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IAiAnalysisService _aiAnalysisService;
    private readonly IRiskScoringService _riskScoringService;
    private readonly IAuditService _auditService;

    public TriggerAiAnalysisCommandHandler(
        IEnhancementHubDbContext dbContext,
        IAiAnalysisService aiAnalysisService,
        IRiskScoringService riskScoringService,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _aiAnalysisService = aiAnalysisService;
        _riskScoringService = riskScoringService;
        _auditService = auditService;
    }

    public async Task<EnhancementAnalysisDto> Handle(
        TriggerAiAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        var enhancementRequest = await _dbContext.EnhancementRequests
            .Include(r => r.TargetApplication)
            .ThenInclude(a => a!.Profiles)
            .FirstOrDefaultAsync(r => r.Id == request.EnhancementRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), request.EnhancementRequestId);

        enhancementRequest.Status = EnhancementRequestStatus.AiAnalyzing;
        enhancementRequest.UpdatedAt = DateTime.UtcNow;

        var latestVersion = await _dbContext.EnhancementAnalyses
            .Where(a => a.EnhancementRequestId == enhancementRequest.Id)
            .Select(a => (int?)a.Version)
            .MaxAsync(cancellationToken) ?? 0;

        var analysis = new EnhancementAnalysis
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = enhancementRequest.Id,
            Version = latestVersion + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.EnhancementAnalyses.Add(analysis);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var repositoryContext = enhancementRequest.TargetApplication?.Profiles.FirstOrDefault()?.KeyComponents;
            var description = $"{enhancementRequest.BusinessDescription}\n\nDesired outcome: {enhancementRequest.DesiredOutcome}";
            var result = await _aiAnalysisService.AnalyzeEnhancementAsync(
                enhancementRequest.Id,
                enhancementRequest.Title,
                description,
                repositoryContext,
                cancellationToken);

            await PopulateAnalysisFromResultAsync(
                analysis,
                enhancementRequest,
                result,
                cancellationToken);

            enhancementRequest.Status = EnhancementRequestStatus.PendingApproval;
            enhancementRequest.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                "EnhancementAnalyzed",
                nameof(EnhancementRequest),
                enhancementRequest.Id,
                result.Summary,
                cancellationToken);
        }
        catch (Exception ex)
        {
            analysis.AmbiguityNotes = ex.Message;
            enhancementRequest.Status = EnhancementRequestStatus.Submitted;
            enhancementRequest.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }

        return await LoadAnalysisDtoAsync(analysis.Id, cancellationToken);
    }

    private Task PopulateAnalysisFromResultAsync(
        EnhancementAnalysis analysis,
        EnhancementRequest request,
        Abstractions.Models.AiAnalysisResult result,
        CancellationToken cancellationToken)
    {
        var riskScore = _riskScoringService.CalculateRiskScore(result, null);
        analysis.FeatureSummary = result.Summary;
        analysis.BusinessRequirement = request.BusinessDescription;
        analysis.TechnicalRequirements = string.Join(Environment.NewLine, result.Recommendations);
        analysis.RiskLevel = _riskScoringService.MapToRiskLevel(riskScore);
        analysis.RiskExplanation = string.Join(Environment.NewLine, result.Risks);
        analysis.TestingPlan = "Validate impacted areas with automated regression and integration tests.";
        analysis.ConfidenceScore = result.IsMock ? 0.6 : 0.85;
        analysis.NeedsClarification = result.Risks.Any(r =>
            r.Contains("clarification", StringComparison.OrdinalIgnoreCase));
        analysis.UpdatedAt = DateTime.UtcNow;

        foreach (var area in result.ImpactedAreas)
        {
            _dbContext.AnalysisFindings.Add(new AnalysisFinding
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

        foreach (var risk in result.Risks)
        {
            _dbContext.AnalysisFindings.Add(new AnalysisFinding
            {
                Id = Guid.NewGuid(),
                EnhancementAnalysisId = analysis.Id,
                Category = "Risk",
                Title = risk,
                Description = risk,
                ConfidenceScore = analysis.ConfidenceScore,
                IsAiSuggested = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _dbContext.RiskAssessments.Add(new RiskAssessment
        {
            Id = Guid.NewGuid(),
            EnhancementAnalysisId = analysis.Id,
            RiskLevel = analysis.RiskLevel,
            Explanation = analysis.RiskExplanation,
            ConfidenceScore = analysis.ConfidenceScore,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        if (request.TargetApplicationId.HasValue)
        {
            _dbContext.AffectedApplications.Add(new AffectedApplication
            {
                Id = Guid.NewGuid(),
                EnhancementAnalysisId = analysis.Id,
                ApplicationId = request.TargetApplicationId.Value,
                ImpactDescription = result.Summary,
                ConfidenceScore = analysis.ConfidenceScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _dbContext.AiPromptRuns.Add(new AiPromptRun
        {
            Id = Guid.NewGuid(),
            EnhancementRequestId = request.Id,
            EnhancementAnalysisId = analysis.Id,
            PromptVersion = "v1",
            ModelName = result.ModelUsed,
            SystemPrompt = "Enhancement analysis",
            UserPrompt = request.Title,
            StructuredResponse = JsonSerializer.Serialize(result),
            Status = AiRunStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    private async Task<EnhancementAnalysisDto> LoadAnalysisDtoAsync(
        Guid analysisId,
        CancellationToken cancellationToken)
    {
        var analysis = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Include(a => a.Findings)
            .Include(a => a.AffectedApplications).ThenInclude(a => a.Application)
            .Include(a => a.AffectedRepositories).ThenInclude(r => r.Repository)
            .Include(a => a.AffectedComponents)
            .Include(a => a.DatabaseChangeRecommendations)
            .Include(a => a.ApiChangeRecommendations)
            .Include(a => a.RiskAssessments)
            .FirstAsync(a => a.Id == analysisId, cancellationToken);

        return analysis.ToDto();
    }
}
