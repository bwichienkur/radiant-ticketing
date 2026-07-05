using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Analysis.Queries;

public sealed record GetAnalysisComparisonQuery(
    Guid EnhancementRequestId,
    int VersionA,
    int? VersionB = null) : IRequest<AnalysisComparisonDto>;

public sealed class GetAnalysisComparisonQueryHandler
    : IRequestHandler<GetAnalysisComparisonQuery, AnalysisComparisonDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetAnalysisComparisonQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<AnalysisComparisonDto> Handle(
        GetAnalysisComparisonQuery request,
        CancellationToken cancellationToken)
    {
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

        var versionB = request.VersionB ?? analyses.Max(a => a.Version);
        var analysisA = analyses.FirstOrDefault(a => a.Version == request.VersionA)
            ?? throw new NotFoundException(nameof(Domain.Entities.EnhancementAnalysis), request.VersionA);
        var analysisB = analyses.FirstOrDefault(a => a.Version == versionB)
            ?? throw new NotFoundException(nameof(Domain.Entities.EnhancementAnalysis), versionB);

        var fieldChanges = CompareFields(analysisA, analysisB);
        var findingChanges = CompareFindings(analysisA.Findings, analysisB.Findings);

        var architectEdits = await _dbContext.ApprovalActions
            .AsNoTracking()
            .CountAsync(
                a => a.EnhancementRequestId == request.EnhancementRequestId
                     && a.ActionType == ApprovalActionType.EditRequirements
                     && a.EnhancementAnalysisId == analysisB.Id,
                cancellationToken);

        return new AnalysisComparisonDto(
            request.EnhancementRequestId,
            analysisA.Version,
            analysisB.Version,
            analysisA.RiskLevel,
            analysisB.RiskLevel,
            analysisA.ConfidenceScore,
            analysisB.ConfidenceScore,
            fieldChanges,
            findingChanges,
            analysisA.Findings.Count(f => f.IsAiSuggested),
            analysisB.Findings.Count(f => f.IsHumanApproved),
            architectEdits);
    }

    public static List<AnalysisFieldChangeDto> CompareFields(
        Domain.Entities.EnhancementAnalysis a,
        Domain.Entities.EnhancementAnalysis b)
    {
        var fields = new (string Name, string? A, string? B)[]
        {
            ("FeatureSummary", a.FeatureSummary, b.FeatureSummary),
            ("TechnicalRequirements", a.TechnicalRequirements, b.TechnicalRequirements),
            ("TestingPlan", a.TestingPlan, b.TestingPlan),
            ("RolloutPlan", a.RolloutPlan, b.RolloutPlan),
            ("RiskExplanation", a.RiskExplanation, b.RiskExplanation),
            ("FeatureCategory", a.FeatureCategory, b.FeatureCategory)
        };

        return fields
            .Select(f => new AnalysisFieldChangeDto(
                f.Name,
                f.A,
                f.B,
                !string.Equals(f.A, f.B, StringComparison.Ordinal)))
            .Where(f => f.Changed || !string.IsNullOrWhiteSpace(f.ValueA) || !string.IsNullOrWhiteSpace(f.ValueB))
            .ToList();
    }

    internal static List<FindingComparisonDto> CompareFindings(
        IEnumerable<Domain.Entities.AnalysisFinding> findingsA,
        IEnumerable<Domain.Entities.AnalysisFinding> findingsB)
    {
        var mapA = findingsA.ToDictionary(f => Key(f), StringComparer.OrdinalIgnoreCase);
        var mapB = findingsB.ToDictionary(f => Key(f), StringComparer.OrdinalIgnoreCase);
        var keys = mapA.Keys.Union(mapB.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(k => k);
        var results = new List<FindingComparisonDto>();

        foreach (var key in keys)
        {
            mapA.TryGetValue(key, out var fa);
            mapB.TryGetValue(key, out var fb);

            var changeType = fa is null
                ? ComparisonChangeType.Added
                : fb is null
                    ? ComparisonChangeType.Removed
                    : fa.Description != fb.Description
                        ? ComparisonChangeType.Modified
                        : ComparisonChangeType.Unchanged;

            results.Add(new FindingComparisonDto(
                fb?.Title ?? fa!.Title,
                fb?.Category ?? fa!.Category,
                changeType,
                fb?.IsAiSuggested ?? fa!.IsAiSuggested,
                fb?.IsHumanApproved ?? false));
        }

        return results;
    }

    private static string Key(Domain.Entities.AnalysisFinding f) => $"{f.Category}:{f.Title}";
}
