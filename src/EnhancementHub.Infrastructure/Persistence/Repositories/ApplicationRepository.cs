using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Infrastructure.Persistence.Repositories;

public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly EnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public ApplicationRepository(EnhancementHubDbContext dbContext, IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<ApplicationEntity>> ListWithRepositoriesAsync(
        CancellationToken cancellationToken = default) =>
        await _accessService.ApplyVisibilityFilter(
                _dbContext.Applications.AsNoTracking().Include(a => a.Repositories))
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ApplicationProfile>> ListProfilesAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.ApplicationProfiles
            .AsNoTracking()
            .Where(p => p.ApplicationId == applicationId)
            .OrderByDescending(p => p.GeneratedAt)
            .ToListAsync(cancellationToken);
}
