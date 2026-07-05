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
    private readonly ICurrentTenantService _currentTenant;

    public EnhancementRequestAccessService(
        IEnhancementHubDbContext dbContext,
        ICurrentUserService currentUser,
        ICurrentTenantService currentTenant)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    public IQueryable<EnhancementRequest> ApplyVisibilityFilter(IQueryable<EnhancementRequest> query)
    {
        query = ApplyTenantFilter(query);

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

    private IQueryable<EnhancementRequest> ApplyTenantFilter(IQueryable<EnhancementRequest> query)
    {
        if (!_currentTenant.TenantId.HasValue)
        {
            return query;
        }

        var tenantId = _currentTenant.TenantId.Value;

        return query.Where(r =>
            _dbContext.Users.Any(u => u.Id == r.SubmittedByUserId && u.TenantId == tenantId)
            || (r.TeamId.HasValue
                && _dbContext.Teams.Any(t => t.Id == r.TeamId && t.TenantId == tenantId))
            || (r.TargetApplicationId.HasValue
                && _dbContext.Applications.Any(a =>
                    a.Id == r.TargetApplicationId
                    && _dbContext.Teams.Any(t => t.Id == a.OwnerTeamId && t.TenantId == tenantId))));
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
