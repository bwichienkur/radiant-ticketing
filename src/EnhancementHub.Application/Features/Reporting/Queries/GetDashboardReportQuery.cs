using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Reporting.Queries;

public sealed record GetDashboardReportQuery : IRequest<DashboardReportDto>;

public sealed class GetDashboardReportQueryHandler
    : IRequestHandler<GetDashboardReportQuery, DashboardReportDto>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetDashboardReportQueryHandler(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardReportDto> Handle(
        GetDashboardReportQuery request,
        CancellationToken cancellationToken)
    {
        var requests = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Select(r => new { r.Id, r.Status, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var requestsByStatus = Enum.GetValues<EnhancementRequestStatus>()
            .ToDictionary(
                status => status,
                status => requests.Count(r => r.Status == status));

        var approvalActions = await _dbContext.ApprovalActions
            .AsNoTracking()
            .Where(a => a.ActionType == ApprovalActionType.Approve)
            .Select(a => new { a.EnhancementRequestId, a.CreatedAt })
            .ToListAsync(cancellationToken);

        double? averageApprovalTimeHours = null;
        if (approvalActions.Count > 0)
        {
            var approvalDurations = new List<double>();
            foreach (var action in approvalActions)
            {
                var requestCreatedAt = requests
                    .FirstOrDefault(r => r.Id == action.EnhancementRequestId)
                    ?.CreatedAt;

                if (requestCreatedAt.HasValue)
                {
                    approvalDurations.Add((action.CreatedAt - requestCreatedAt.Value).TotalHours);
                }
            }

            if (approvalDurations.Count > 0)
            {
                averageApprovalTimeHours = approvalDurations.Average();
            }
        }

        var latestAnalyses = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .GroupBy(a => a.EnhancementRequestId)
            .Select(g => new { RiskLevel = g.OrderByDescending(a => a.Version).First().RiskLevel })
            .ToListAsync(cancellationToken);

        var highRiskCount = latestAnalyses.Count(a => a.RiskLevel == RiskLevel.High);
        var criticalRiskCount = latestAnalyses.Count(a => a.RiskLevel == RiskLevel.Critical);

        return new DashboardReportDto(
            requestsByStatus,
            requests.Count,
            requests.Count(r => r.Status == EnhancementRequestStatus.PendingApproval),
            highRiskCount,
            criticalRiskCount,
            requests.Count(r => r.Status == EnhancementRequestStatus.ReadyForDevelopment),
            averageApprovalTimeHours);
    }
}
