using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Reporting.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Reporting.Queries;

public sealed class GetAiUsageReportQueryHandler
    : IRequestHandler<GetAiUsageReportQuery, AiUsageReportDto>
{
    private readonly IReportingDbContext _dbContext;

    public GetAiUsageReportQueryHandler(IReportingDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<AiUsageReportDto> Handle(
        GetAiUsageReportQuery request,
        CancellationToken cancellationToken)
    {
        var periodStart = DateTime.UtcNow.Date;
        var periodEnd = periodStart.AddDays(1);

        var runs = await _dbContext.AiPromptRuns
            .AsNoTracking()
            .Where(r => r.StartedAt >= periodStart && r.StartedAt < periodEnd)
            .ToListAsync(cancellationToken);

        var byWorkflow = runs
            .GroupBy(r => string.IsNullOrWhiteSpace(r.WorkflowStep) ? "Unknown" : r.WorkflowStep)
            .Select(g => new AiUsageByWorkflowDto(
                g.Key,
                g.Count(),
                g.Sum(r => r.TotalTokens ?? 0),
                g.Sum(r => r.EstimatedCostUsd ?? 0m)))
            .OrderByDescending(x => x.TotalTokens)
            .ToList();

        var byModel = runs
            .GroupBy(r => string.IsNullOrWhiteSpace(r.ModelName) ? "Unknown" : r.ModelName)
            .Select(g => new AiUsageByModelDto(
                g.Key,
                g.Count(),
                g.Sum(r => r.TotalTokens ?? 0),
                g.Sum(r => r.EstimatedCostUsd ?? 0m)))
            .OrderByDescending(x => x.TotalTokens)
            .ToList();

        return new AiUsageReportDto(
            periodStart,
            periodEnd,
            runs.Count,
            runs.Sum(r => r.TotalTokens ?? 0),
            runs.Sum(r => r.EstimatedCostUsd ?? 0m),
            byWorkflow,
            byModel);
    }
}
