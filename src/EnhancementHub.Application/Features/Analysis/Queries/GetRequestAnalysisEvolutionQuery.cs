using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Application.Features.Analysis.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Analysis.Queries;

public sealed record GetRequestAnalysisEvolutionQuery(Guid EnhancementRequestId)
    : IRequest<AnalysisComparisonDto>;

public sealed class GetRequestAnalysisEvolutionQueryHandler
    : IRequestHandler<GetRequestAnalysisEvolutionQuery, AnalysisComparisonDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IEnhancementRequestAccessService _accessService;
    private readonly IMediator _mediator;

    public GetRequestAnalysisEvolutionQueryHandler(
        IEnhancementHubDbContext dbContext,
        IEnhancementRequestAccessService accessService,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _accessService = accessService;
        _mediator = mediator;
    }

    public async Task<AnalysisComparisonDto> Handle(
        GetRequestAnalysisEvolutionQuery request,
        CancellationToken cancellationToken)
    {
        await _accessService.GetAccessibleRequestAsync(request.EnhancementRequestId, cancellationToken);

        var analyses = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Include(a => a.Findings)
            .Where(a => a.EnhancementRequestId == request.EnhancementRequestId)
            .OrderBy(a => a.Version)
            .ToListAsync(cancellationToken);

        if (analyses.Count == 0)
        {
            throw new NotFoundException(nameof(Domain.Entities.EnhancementAnalysis), request.EnhancementRequestId);
        }

        var first = analyses[0];
        var latest = analyses[^1];

        if (analyses.Count > 1)
        {
            return await _mediator.Send(
                new GetAnalysisComparisonQuery(request.EnhancementRequestId, first.Version, latest.Version),
                cancellationToken);
        }

        var architectEdit = await _dbContext.ApprovalActions
            .AsNoTracking()
            .Where(a =>
                a.EnhancementRequestId == request.EnhancementRequestId
                && a.EnhancementAnalysisId == latest.Id
                && a.ActionType == ApprovalActionType.EditRequirements)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (architectEdit is null || string.IsNullOrWhiteSpace(architectEdit.PreviousValue))
        {
            return await _mediator.Send(
                new GetAnalysisComparisonQuery(request.EnhancementRequestId, first.Version, latest.Version),
                cancellationToken);
        }

        var previous = JsonSerializer.Deserialize<Dictionary<string, string?>>(architectEdit.PreviousValue)
            ?? new Dictionary<string, string?>();

        var syntheticBaseline = new Domain.Entities.EnhancementAnalysis
        {
            Version = latest.Version,
            RiskLevel = latest.RiskLevel,
            ConfidenceScore = latest.ConfidenceScore,
            FeatureSummary = previous.GetValueOrDefault("FeatureSummary"),
            TechnicalRequirements = previous.GetValueOrDefault("TechnicalRequirements"),
            TestingPlan = previous.GetValueOrDefault("TestingPlan"),
            RolloutPlan = previous.GetValueOrDefault("RolloutPlan"),
            RiskExplanation = latest.RiskExplanation,
            FeatureCategory = latest.FeatureCategory
        };

        var fieldChanges = GetAnalysisComparisonQueryHandler.CompareFields(syntheticBaseline, latest);
        var findingChanges = GetAnalysisComparisonQueryHandler.CompareFindings(
            Array.Empty<Domain.Entities.AnalysisFinding>(),
            latest.Findings);

        return new AnalysisComparisonDto(
            request.EnhancementRequestId,
            latest.Version,
            latest.Version,
            latest.RiskLevel,
            latest.RiskLevel,
            latest.ConfidenceScore,
            latest.ConfidenceScore,
            fieldChanges,
            findingChanges,
            latest.Findings.Count(f => f.IsAiSuggested),
            latest.Findings.Count(f => f.IsHumanApproved),
            1);
    }
}
