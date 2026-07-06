using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Delivery.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Delivery.Queries;

public sealed record GetApplicationTestSuiteQuery(Guid ApplicationId) : IRequest<ApplicationTestSuiteDto?>;

public sealed class GetApplicationTestSuiteQueryHandler
    : IRequestHandler<GetApplicationTestSuiteQuery, ApplicationTestSuiteDto?>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetApplicationTestSuiteQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<ApplicationTestSuiteDto?> Handle(
        GetApplicationTestSuiteQuery request,
        CancellationToken cancellationToken)
    {
        var suite = await _dbContext.ApplicationTestSuites
            .AsNoTracking()
            .Include(s => s.TestCases)
            .Where(s => s.ApplicationId == request.ApplicationId && s.IsDefaultRegression)
            .FirstOrDefaultAsync(cancellationToken);

        if (suite is null)
        {
            return null;
        }

        var cases = suite.TestCases
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Title)
            .Select(c => new TestCaseSummaryDto(
                c.Id,
                c.Title,
                c.Status.ToString(),
                c.Origin.ToString(),
                c.SourceEnhancementRequestId,
                c.CurrentVersion))
            .ToList();

        return new ApplicationTestSuiteDto(suite.Id, suite.ApplicationId, suite.Name, cases);
    }
}
