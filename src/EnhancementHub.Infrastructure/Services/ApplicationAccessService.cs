using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Infrastructure.Services;

public sealed class ApplicationAccessService : IApplicationAccessService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public ApplicationAccessService(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public IQueryable<ApplicationEntity> ApplyVisibilityFilter(IQueryable<ApplicationEntity> query)
    {
        if (_currentUser.Role == UserRole.Admin)
        {
            return query;
        }

        if (!_currentUser.UserId.HasValue)
        {
            return query.Where(_ => false);
        }

        var userId = _currentUser.UserId.Value;
        var teamIds = _dbContext.TeamMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.TeamId);

        return query.Where(a => teamIds.Contains(a.OwnerTeamId));
    }

    public async Task<ApplicationEntity> GetAccessibleApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyVisibilityFilter(_dbContext.Applications.AsQueryable());

        return await query.FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Application), applicationId);
    }

    public Task EnsureAccessibleApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        GetAccessibleApplicationAsync(applicationId, cancellationToken);

    public async Task EnsureAccessibleConnectionAsync(
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.DatabaseConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken)
            ?? throw new NotFoundException(nameof(DatabaseConnection), connectionId);

        await EnsureAccessibleApplicationAsync(connection.ApplicationId, cancellationToken);
    }
}
