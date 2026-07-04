using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Services;

public sealed class EnhancementRequestAccessService : IEnhancementRequestAccessService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public EnhancementRequestAccessService(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public IQueryable<EnhancementRequest> ApplyVisibilityFilter(IQueryable<EnhancementRequest> query)
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

        return query.Where(r =>
            r.SubmittedByUserId == userId
            || (r.TeamId.HasValue && teamIds.Contains(r.TeamId.Value))
            || (r.TargetApplicationId.HasValue
                && _dbContext.Applications.Any(a =>
                    a.Id == r.TargetApplicationId
                    && teamIds.Contains(a.OwnerTeamId))));
    }

    public async Task<EnhancementRequest> GetAccessibleRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyVisibilityFilter(_dbContext.EnhancementRequests.AsQueryable());

        return await query.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EnhancementRequest), requestId);
    }

    public async Task EnsureCanModifyAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await GetAccessibleRequestAsync(requestId, cancellationToken);

        if (_currentUser.Role == UserRole.Admin)
        {
            return;
        }

        if (request.SubmittedByUserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You do not have permission to modify this enhancement request.");
        }
    }
}
