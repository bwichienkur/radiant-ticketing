using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Analysis.Queries;

public sealed record GetEnhancementAnalysisQuery(
    Guid EnhancementRequestId,
    int? Version = null) : IRequest<EnhancementAnalysisDto>;

public sealed class GetEnhancementAnalysisQueryHandler
    : IRequestHandler<GetEnhancementAnalysisQuery, EnhancementAnalysisDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetEnhancementAnalysisQueryHandler(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EnhancementAnalysisDto> Handle(
        GetEnhancementAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Include(a => a.Findings)
            .Include(a => a.AffectedApplications).ThenInclude(a => a.Application)
            .Include(a => a.AffectedRepositories).ThenInclude(r => r.Repository)
            .Include(a => a.AffectedComponents)
            .Include(a => a.DatabaseChangeRecommendations)
            .Include(a => a.ApiChangeRecommendations)
            .Include(a => a.RiskAssessments)
            .Where(a => a.EnhancementRequestId == request.EnhancementRequestId);

        EnhancementAnalysis? analysis;
        if (request.Version.HasValue)
        {
            analysis = await query.FirstOrDefaultAsync(
                a => a.Version == request.Version.Value,
                cancellationToken);
        }
        else
        {
            analysis = await query
                .OrderByDescending(a => a.Version)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (analysis is null)
        {
            throw new NotFoundException(
                nameof(EnhancementAnalysis),
                $"request:{request.EnhancementRequestId}, version:{request.Version?.ToString() ?? "latest"}");
        }

        return analysis.ToDto();
    }
}
