using EnhancementHub.Domain.Entities;

namespace EnhancementHub.Application.Abstractions.Persistence;

public interface ITeamRepository
{
    Task<IReadOnlyList<Team>> ListWithCountsAsync(CancellationToken cancellationToken = default);
    Task<TeamDetailProjection?> GetDetailAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<Team?> GetByIdWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
    void Add(Team team);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record TeamDetailProjection(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<TeamMemberProjection> Members,
    IReadOnlyList<TeamApplicationProjection> Applications);

public sealed record TeamMemberProjection(
    Guid Id,
    Guid UserId,
    string Email,
    string DisplayName,
    Domain.Enums.UserRole UserRole,
    string TeamRole,
    bool IsActive);

public sealed record TeamApplicationProjection(Guid Id, string Name);
