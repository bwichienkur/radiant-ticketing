using EnhancementHub.Application.Abstractions;
using MediatR;

namespace EnhancementHub.Application.Features.Tenants.Queries;

public sealed record GetCurrentTenantIsolationQuery : IRequest<TenantIsolationStatus>;

public sealed class GetCurrentTenantIsolationQueryHandler
    : IRequestHandler<GetCurrentTenantIsolationQuery, TenantIsolationStatus>
{
    private readonly ICurrentTenantService _currentTenant;
    private readonly ITenantIsolationService _isolationService;

    public GetCurrentTenantIsolationQueryHandler(
        ICurrentTenantService currentTenant,
        ITenantIsolationService isolationService)
    {
        _currentTenant = currentTenant;
        _isolationService = isolationService;
    }

    public Task<TenantIsolationStatus> Handle(
        GetCurrentTenantIsolationQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        return _isolationService.GetIsolationStatusAsync(
            _currentTenant.TenantId.Value,
            cancellationToken);
    }
}
