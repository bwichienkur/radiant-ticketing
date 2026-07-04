using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Applications.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = EnhancementHub.Domain.Entities.Application;

namespace EnhancementHub.Application.Features.Applications.Queries;

public sealed record ListApplicationsQuery : IRequest<IReadOnlyList<ApplicationDto>>;

public sealed class ListApplicationsQueryHandler
    : IRequestHandler<ListApplicationsQuery, IReadOnlyList<ApplicationDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListApplicationsQueryHandler(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ApplicationDto>> Handle(
        ListApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Applications
            .AsNoTracking()
            .Include(a => a.Repositories)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDto()).ToList();
    }
}

public sealed record GetApplicationProfileQuery(Guid ApplicationId)
    : IRequest<IReadOnlyList<ApplicationProfileDto>>;

public sealed class GetApplicationProfileQueryHandler
    : IRequestHandler<GetApplicationProfileQuery, IReadOnlyList<ApplicationProfileDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetApplicationProfileQueryHandler(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ApplicationProfileDto>> Handle(
        GetApplicationProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profiles = await _dbContext.ApplicationProfiles
            .AsNoTracking()
            .Where(p => p.ApplicationId == request.ApplicationId)
            .OrderByDescending(p => p.GeneratedAt)
            .ToListAsync(cancellationToken);

        return profiles.Select(p => p.ToDto()).ToList();
    }
}
