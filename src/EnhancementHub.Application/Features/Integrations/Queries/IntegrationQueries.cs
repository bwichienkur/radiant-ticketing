using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Integrations.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Integrations.Queries;

public sealed record ListOpenApiRegistrationsQuery(Guid ApplicationId)
    : IRequest<IReadOnlyList<OpenApiRegistrationDto>>;

public sealed class ListOpenApiRegistrationsQueryHandler
    : IRequestHandler<ListOpenApiRegistrationsQuery, IReadOnlyList<OpenApiRegistrationDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IApplicationAccessService _accessService;

    public ListOpenApiRegistrationsQueryHandler(
        IEnhancementHubDbContext dbContext,
        IApplicationAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<OpenApiRegistrationDto>> Handle(
        ListOpenApiRegistrationsQuery request,
        CancellationToken cancellationToken)
    {
        await _accessService.EnsureAccessibleApplicationAsync(request.ApplicationId, cancellationToken);

        var items = await _dbContext.OpenApiRegistrations
            .AsNoTracking()
            .Where(r => r.ApplicationId == request.ApplicationId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return items.Select(r => new OpenApiRegistrationDto(
            r.Id,
            r.ApplicationId,
            r.Name,
            r.EndpointCount,
            r.BaseUrl,
            r.LastIngestedAt)).ToList();
    }
}

public sealed record ListOpenApiEndpointsQuery(Guid RegistrationId)
    : IRequest<IReadOnlyList<OpenApiEndpointDto>>;

public sealed class ListOpenApiEndpointsQueryHandler
    : IRequestHandler<ListOpenApiEndpointsQuery, IReadOnlyList<OpenApiEndpointDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListOpenApiEndpointsQueryHandler(IEnhancementHubDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<IReadOnlyList<OpenApiEndpointDto>> Handle(
        ListOpenApiEndpointsQuery request,
        CancellationToken cancellationToken)
    {
        var registration = await _dbContext.OpenApiRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.OpenApiRegistration), request.RegistrationId);

        var endpoints = await _dbContext.OpenApiEndpoints
            .AsNoTracking()
            .Where(e => e.OpenApiRegistrationId == request.RegistrationId)
            .OrderBy(e => e.Path)
            .ThenBy(e => e.HttpMethod)
            .ToListAsync(cancellationToken);

        return endpoints.Select(e => new OpenApiEndpointDto(
            e.Id,
            e.Path,
            e.HttpMethod,
            e.OperationId,
            e.Summary,
            e.Tags)).ToList();
    }
}
