using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Reporting.Queries;

public sealed record GetDashboardInsightsQuery : IRequest<DashboardInsightsDto>;

public sealed class GetDashboardInsightsQueryHandler
    : IRequestHandler<GetDashboardInsightsQuery, DashboardInsightsDto>
{
    private readonly IReportingDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardInsightsQueryHandler(
        IReportingDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<DashboardInsightsDto> Handle(
        GetDashboardInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Date.AddDays(-6);
        var requests = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Where(r => r.CreatedAt >= since)
            .Select(r => new { r.CreatedAt.Date })
            .ToListAsync(cancellationToken);

        var trend = Enumerable.Range(0, 7)
            .Select(i => DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-6 + i)))
            .Select(date => new DailyRequestCountDto(
                date,
                requests.Count(r => DateOnly.FromDateTime(r.Date) == date)))
            .ToList();

        var recentApprovals = await _dbContext.ApprovalActions
            .AsNoTracking()
            .Include(a => a.EnhancementRequest)
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new DashboardActivityItemDto(
                "Approval",
                $"{a.ActionType}: {a.EnhancementRequest.Title}",
                a.User.DisplayName,
                a.CreatedAt,
                a.EnhancementRequestId,
                $"/EnhancementRequests/Details/{a.EnhancementRequestId}"))
            .ToListAsync(cancellationToken);

        var recentAnalyses = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Include(a => a.EnhancementRequest)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new DashboardActivityItemDto(
                "Analysis",
                $"AI analysis: {a.EnhancementRequest.Title}",
                $"{a.RiskLevel} risk",
                a.CreatedAt,
                a.EnhancementRequestId,
                $"/EnhancementRequests/Details/{a.EnhancementRequestId}"))
            .ToListAsync(cancellationToken);

        var activity = recentApprovals
            .Concat(recentAnalyses)
            .OrderByDescending(a => a.OccurredAt)
            .Take(8)
            .ToList();

        var myPendingApprovals = 0;
        var myAwaitingAnalysis = 0;
        if (_currentUser.Role is UserRole.Admin or UserRole.Approver)
        {
            myPendingApprovals = await _dbContext.EnhancementRequests
                .AsNoTracking()
                .CountAsync(r => r.Status == EnhancementRequestStatus.PendingApproval, cancellationToken);
        }

        if (_currentUser.Role is UserRole.Admin or UserRole.Developer)
        {
            myAwaitingAnalysis = await _dbContext.EnhancementRequests
                .AsNoTracking()
                .CountAsync(r =>
                    r.Status == EnhancementRequestStatus.Submitted
                    || r.Status == EnhancementRequestStatus.AiAnalyzing,
                    cancellationToken);
        }

        return new DashboardInsightsDto(activity, trend, myPendingApprovals, myAwaitingAnalysis);
    }
}
