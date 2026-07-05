using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Domain.Enums;
using MediatR;

namespace EnhancementHub.Application.Features.Billing.Commands;

public sealed record CreateBillingCheckoutCommand(TenantPlan Plan) : IRequest<BillingSessionDto>;

public sealed record CreateBillingPortalCommand : IRequest<BillingSessionDto>;

public sealed record BillingSessionDto(string Url);

public sealed class CreateBillingCheckoutCommandHandler
    : IRequestHandler<CreateBillingCheckoutCommand, BillingSessionDto>
{
    private readonly ICurrentTenantService _currentTenant;
    private readonly IStripeBillingService _stripeBillingService;

    public CreateBillingCheckoutCommandHandler(
        ICurrentTenantService currentTenant,
        IStripeBillingService stripeBillingService)
    {
        _currentTenant = currentTenant;
        _stripeBillingService = stripeBillingService;
    }

    public async Task<BillingSessionDto> Handle(
        CreateBillingCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        if (request.Plan is not (TenantPlan.Team or TenantPlan.Enterprise))
        {
            throw new ForbiddenException("Only Team and Enterprise plans can be purchased.");
        }

        var result = await _stripeBillingService.CreateCheckoutSessionAsync(
            _currentTenant.TenantId.Value,
            request.Plan,
            customerEmail: null,
            cancellationToken);

        if (!result.Accepted || string.IsNullOrWhiteSpace(result.CheckoutUrl))
        {
            throw new ForbiddenException(result.Error ?? "Unable to create checkout session.");
        }

        return new BillingSessionDto(result.CheckoutUrl);
    }
}

public sealed class CreateBillingPortalCommandHandler
    : IRequestHandler<CreateBillingPortalCommand, BillingSessionDto>
{
    private readonly ICurrentTenantService _currentTenant;
    private readonly IStripeBillingService _stripeBillingService;

    public CreateBillingPortalCommandHandler(
        ICurrentTenantService currentTenant,
        IStripeBillingService stripeBillingService)
    {
        _currentTenant = currentTenant;
        _stripeBillingService = stripeBillingService;
    }

    public async Task<BillingSessionDto> Handle(
        CreateBillingPortalCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        var result = await _stripeBillingService.CreatePortalSessionAsync(
            _currentTenant.TenantId.Value,
            cancellationToken);

        if (!result.Accepted || string.IsNullOrWhiteSpace(result.PortalUrl))
        {
            throw new ForbiddenException(result.Error ?? "Unable to open billing portal.");
        }

        return new BillingSessionDto(result.PortalUrl);
    }
}
