using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class ListTeamsQueryHandler : IRequestHandler<ListTeamsQuery, IReadOnlyList<TeamSummaryDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListTeamsQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<TeamSummaryDto>> Handle(
        ListTeamsQuery request,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Teams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TeamSummaryDto(
                t.Id,
                t.Name,
                t.Description,
                t.Members.Count,
                t.OwnedApplications.Count))
            .ToListAsync(cancellationToken);
    }
}
