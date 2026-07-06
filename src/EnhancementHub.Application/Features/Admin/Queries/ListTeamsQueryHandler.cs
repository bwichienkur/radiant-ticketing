using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed class ListTeamsQueryHandler : IRequestHandler<ListTeamsQuery, IReadOnlyList<TeamSummaryDto>>
{
    private readonly ITeamRepository _teams;

    public ListTeamsQueryHandler(ITeamRepository teams) => _teams = teams;

    public async Task<IReadOnlyList<TeamSummaryDto>> Handle(
        ListTeamsQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await _teams.ListWithCountsAsync(cancellationToken);
        return entities
            .Select(t => new TeamSummaryDto(
                t.Id,
                t.Name,
                t.Description,
                t.Members.Count,
                t.OwnedApplications.Count))
            .ToList();
    }
}
