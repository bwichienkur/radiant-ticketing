using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class GetTeamDetailQueryHandler : IRequestHandler<GetTeamDetailQuery, TeamDetailDto?>
{
    private readonly ITeamRepository _teams;

    public GetTeamDetailQueryHandler(ITeamRepository teams) => _teams = teams;

    public async Task<TeamDetailDto?> Handle(GetTeamDetailQuery request, CancellationToken cancellationToken)
    {
        var team = await _teams.GetDetailAsync(request.TeamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        return new TeamDetailDto(
            team.Id,
            team.Name,
            team.Description,
            team.Members
                .Select(m => new TeamMemberDto(
                    m.Id,
                    m.UserId,
                    m.Email,
                    m.DisplayName,
                    m.UserRole,
                    m.TeamRole,
                    m.IsActive))
                .ToList(),
            team.Applications
                .Select(a => new TeamApplicationSummaryDto(a.Id, a.Name))
                .ToList());
    }
}
