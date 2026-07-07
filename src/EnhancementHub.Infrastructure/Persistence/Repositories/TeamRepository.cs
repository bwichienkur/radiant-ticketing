using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Persistence.Repositories;

public sealed class TeamRepository : ITeamRepository
{
    private readonly EnhancementHubDbContext _dbContext;

    public TeamRepository(EnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<Team>> ListWithCountsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Teams
            .AsNoTracking()
            .Include(t => t.Members)
            .Include(t => t.OwnedApplications)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task<TeamDetailProjection?> GetDetailAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams
            .AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new TeamDetailProjection(
                t.Id,
                t.Name,
                t.Description,
                t.Members
                    .OrderBy(m => m.User.DisplayName)
                    .Select(m => new TeamMemberProjection(
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
                    .Select(a => new TeamApplicationProjection(a.Id, a.Name))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return team;
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        _dbContext.Teams.AnyAsync(t => t.Name == name, cancellationToken);

    public Task<Team?> GetByIdWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

    public void Add(Team team) => _dbContext.Teams.Add(team);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
