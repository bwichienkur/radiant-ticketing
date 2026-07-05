using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class GetTeamDetailQueryHandler : IRequestHandler<GetTeamDetailQuery, TeamDetailDto?>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetTeamDetailQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<TeamDetailDto?> Handle(GetTeamDetailQuery request, CancellationToken cancellationToken)
    {
        var team = await _dbContext.Teams
            .AsNoTracking()
            .Where(t => t.Id == request.TeamId)
            .Select(t => new TeamDetailDto(
                t.Id,
                t.Name,
                t.Description,
                t.Members
                    .OrderBy(m => m.User.DisplayName)
                    .Select(m => new TeamMemberDto(
                        m.Id,
                        m.UserId,
                        m.User.Email,
                        m.User.DisplayName,
                        m.User.Role,
                        m.Role,
                        m.User.IsActive))
                    .ToList(),
                t.OwnedApplications
                    .OrderBy(a => a.Name)
                    .Select(a => new TeamApplicationSummaryDto(a.Id, a.Name))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return team;
    }
}
