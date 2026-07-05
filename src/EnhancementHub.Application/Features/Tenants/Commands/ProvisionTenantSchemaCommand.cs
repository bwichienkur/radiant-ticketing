using EnhancementHub.Application.Abstractions;
using MediatR;

namespace EnhancementHub.Application.Features.Tenants.Commands;

public sealed record ProvisionTenantSchemaCommand : IRequest<TenantIsolationStatus>;

public sealed class ProvisionTenantSchemaCommandHandler
    : IRequestHandler<ProvisionTenantSchemaCommand, TenantIsolationStatus>
{
    private readonly ICurrentTenantService _currentTenant;
    private readonly ITenantIsolationService _isolationService;

    public ProvisionTenantSchemaCommandHandler(
        ICurrentTenantService currentTenant,
        ITenantIsolationService isolationService)
    {
        _currentTenant = currentTenant;
        _isolationService = isolationService;
    }

    public Task<TenantIsolationStatus> Handle(
        ProvisionTenantSchemaCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        return _isolationService.ProvisionDedicatedSchemaAsync(
            _currentTenant.TenantId.Value,
            cancellationToken);
    }
}
