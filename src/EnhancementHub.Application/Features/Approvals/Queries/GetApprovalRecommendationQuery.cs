using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Approvals.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Approvals.Queries;

public sealed record GetApprovalRecommendationQuery(Guid EnhancementRequestId)
    : IRequest<ApprovalRecommendationDto>;

public sealed class GetApprovalRecommendationQueryHandler
    : IRequestHandler<GetApprovalRecommendationQuery, ApprovalRecommendationDto>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IEnhancementRequestAccessService _accessService;

    public GetApprovalRecommendationQueryHandler(
        IEnhancementHubDbContext dbContext,
        IEnhancementRequestAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<ApprovalRecommendationDto> Handle(
        GetApprovalRecommendationQuery request,
        CancellationToken cancellationToken)
    {
        await _accessService.GetAccessibleRequestAsync(request.EnhancementRequestId, cancellationToken);

        var enhancementRequest = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.EnhancementRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.EnhancementRequest), request.EnhancementRequestId);

        var latestAnalysis = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Where(a => a.EnhancementRequestId == request.EnhancementRequestId)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var recommendation = BuildRecommendation(enhancementRequest.Priority, latestAnalysis);
        var summary = BuildSummary(enhancementRequest, latestAnalysis, recommendation);

        return new ApprovalRecommendationDto(
            request.EnhancementRequestId,
            recommendation,
            summary,
            latestAnalysis?.RiskLevel,
            latestAnalysis?.ConfidenceScore,
            latestAnalysis?.NeedsClarification ?? false);
    }

    private static string BuildRecommendation(string priority, Domain.Entities.EnhancementAnalysis? analysis)
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
        Domain.Entities.EnhancementRequest request,
        Domain.Entities.EnhancementAnalysis? analysis,
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
}
