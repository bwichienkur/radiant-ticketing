using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Applications.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Applications.Queries;

public sealed record ListApplicationsQuery : IRequest<IReadOnlyList<ApplicationDto>>;

public sealed class ListApplicationsQueryHandler
    : IRequestHandler<ListApplicationsQuery, IReadOnlyList<ApplicationDto>>
{
    private readonly IApplicationRepository _applications;
    private readonly IApplicationAccessService _accessService;

    public ListApplicationsQueryHandler(
        IApplicationRepository applications,
        IApplicationAccessService accessService)
    {
        _applications = applications;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<ApplicationDto>> Handle(
        ListApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await _applications.ListWithRepositoriesAsync(cancellationToken);
        return entities.Select(e => e.ToDto()).ToList();
    }
}

public sealed record GetApplicationProfileQuery(Guid ApplicationId)
    : IRequest<IReadOnlyList<ApplicationProfileDto>>;

public sealed class GetApplicationProfileQueryHandler
    : IRequestHandler<GetApplicationProfileQuery, IReadOnlyList<ApplicationProfileDto>>
{
    private readonly IApplicationRepository _applications;
    private readonly IApplicationAccessService _accessService;

    public GetApplicationProfileQueryHandler(
        IApplicationRepository applications,
        IApplicationAccessService accessService)
    {
        _applications = applications;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<ApplicationProfileDto>> Handle(
        GetApplicationProfileQuery request,
        CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleApplicationAsync(request.ApplicationId, cancellationToken);

        var profiles = await _applications.ListProfilesAsync(request.ApplicationId, cancellationToken);
        return profiles.Select(p => p.ToDto()).ToList();
    }
}
