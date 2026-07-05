using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Tenants.Queries;

public sealed record GetCurrentTenantBillingQuery : IRequest<TenantBillingDto>;

public sealed class GetCurrentTenantBillingQueryHandler
    : IRequestHandler<GetCurrentTenantBillingQuery, TenantBillingDto>
{
    private readonly ICurrentTenantService _currentTenant;
    private readonly ITenantBillingService _billingService;

    public GetCurrentTenantBillingQueryHandler(
        ICurrentTenantService currentTenant,
        ITenantBillingService billingService)
    {
        _currentTenant = currentTenant;
        _billingService = billingService;
    }

    public async Task<TenantBillingDto> Handle(
        GetCurrentTenantBillingQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Tenant context is required.");
        }

        var status = await _billingService.GetBillingStatusAsync(_currentTenant.TenantId.Value, cancellationToken);
        return new TenantBillingDto(
            status.TenantId,
            status.TenantName,
            status.Plan,
            status.Region,
            status.IsTrialActive,
            status.IsTrialExpired,
            status.TrialEndsAt,
            status.SubscriptionStatus,
            status.SubscriptionPeriodEnd,
            status.HasActiveSubscription,
            status.StripeEnabled,
            status.Limits.MaxApplications,
            status.Limits.MaxAnalysesPerMonth,
            status.Limits.MaxStorageMegabytes,
            status.Usage.ApplicationCount,
            status.Usage.AnalysisCountThisMonth,
            status.Usage.StorageBytes,
            status.Limits.IsOverLimit);
    }
}

public sealed record ListTenantsQuery : IRequest<IReadOnlyList<TenantSummaryDto>>;

public sealed class ListTenantsQueryHandler : IRequestHandler<ListTenantsQuery, IReadOnlyList<TenantSummaryDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListTenantsQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<TenantSummaryDto>> Handle(
        ListTenantsQuery request,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TenantSummaryDto(
                t.Id,
                t.Name,
                t.Slug,
                t.Plan.ToString(),
                t.Region.ToString(),
                t.IsActive,
                t.TrialEndsAt))
            .ToListAsync(cancellationToken);
    }
}
