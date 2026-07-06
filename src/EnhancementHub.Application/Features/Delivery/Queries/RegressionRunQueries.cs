using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Delivery.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Delivery.Queries;

public sealed record GetApplicationRegressionRunsQuery(Guid ApplicationId, int Limit = 10)
    : IRequest<IReadOnlyList<ApplicationRegressionRunDto>>;

public sealed class GetApplicationRegressionRunsQueryHandler
    : IRequestHandler<GetApplicationRegressionRunsQuery, IReadOnlyList<ApplicationRegressionRunDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetApplicationRegressionRunsQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ApplicationRegressionRunDto>> Handle(
        GetApplicationRegressionRunsQuery request,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ApplicationRegressionRuns
            .AsNoTracking()
            .Where(r => r.ApplicationId == request.ApplicationId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(request.Limit)
            .Select(r => new ApplicationRegressionRunDto(
                r.Id,
                r.ApplicationId,
                r.TestUrl,
                r.Passed,
                r.QaRunner.ToString(),
                r.IsSimulation,
                r.CaseCount,
                r.PassedCaseCount,
                r.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
