using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services;

public sealed class TenantMeteringService : ITenantMeteringService
{
    private readonly IEnhancementHubDbContext _dbContext;

    public TenantMeteringService(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task RecordAnalysisAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetOrCreateCurrentSnapshotAsync(tenantId, cancellationToken);
        snapshot.AnalysisCount++;
        snapshot.CapturedAt = DateTime.UtcNow;
        snapshot.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshUsageSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetOrCreateCurrentSnapshotAsync(tenantId, cancellationToken);
        var teamIds = _dbContext.Teams
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .Select(t => t.Id);

        snapshot.ApplicationCount = await _dbContext.Applications
            .AsNoTracking()
            .CountAsync(a => teamIds.Contains(a.OwnerTeamId), cancellationToken);

        snapshot.StorageBytes = await (
            from a in _dbContext.EnhancementAttachments.AsNoTracking()
            join r in _dbContext.EnhancementRequests on a.EnhancementRequestId equals r.Id
            join u in _dbContext.Users on r.SubmittedByUserId equals u.Id
            where u.TenantId == tenantId
            select a.Id).CountAsync(cancellationToken) * 1024L * 1024L;

        snapshot.CapturedAt = DateTime.UtcNow;
        snapshot.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantUsageSummary> GetUsageSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await RefreshUsageSnapshotAsync(tenantId, cancellationToken);
        var snapshot = await GetOrCreateCurrentSnapshotAsync(tenantId, cancellationToken);

        return new TenantUsageSummary(
            tenantId,
            snapshot.ApplicationCount,
            snapshot.AnalysisCount,
            snapshot.StorageBytes,
            snapshot.PeriodStart);
    }

    private async Task<TenantUsageSnapshot> GetOrCreateCurrentSnapshotAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var periodStart = GetCurrentPeriodStart(DateTime.UtcNow);
        var snapshot = await _dbContext.TenantUsageSnapshots
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.PeriodStart == periodStart, cancellationToken);

        if (snapshot is not null)
        {
            return snapshot;
        }

        var now = DateTime.UtcNow;
        snapshot = new TenantUsageSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PeriodStart = periodStart,
            ApplicationCount = 0,
            AnalysisCount = 0,
            StorageBytes = 0,
            CapturedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.TenantUsageSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return snapshot;
    }

    internal static DateTime GetCurrentPeriodStart(DateTime utcNow) =>
        new(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
}

public sealed class TenantBillingService : ITenantBillingService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly ITenantMeteringService _meteringService;
    private readonly CommercialOptions _commercialOptions;
    private readonly StripeOptions _stripeOptions;

    public TenantBillingService(
        IEnhancementHubDbContext dbContext,
        ITenantMeteringService meteringService,
        Microsoft.Extensions.Options.IOptions<CommercialOptions> commercialOptions,
        Microsoft.Extensions.Options.IOptions<StripeOptions> stripeOptions)
    {
        _dbContext = dbContext;
        _meteringService = meteringService;
        _commercialOptions = commercialOptions.Value;
        _stripeOptions = stripeOptions.Value;
    }

    public async Task<TenantBillingStatus> GetBillingStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new Application.Common.Exceptions.NotFoundException(nameof(Tenant), tenantId);

        var usage = await _meteringService.GetUsageSummaryAsync(tenantId, cancellationToken);
        var limits = ResolveLimits(tenant.Plan);
        var isOver = usage.ApplicationCount > limits.MaxApplications
                     || usage.AnalysisCountThisMonth > limits.MaxAnalysesPerMonth
                     || usage.StorageBytes > limits.MaxStorageMegabytes * 1024L * 1024L;

        var isTrialActive = tenant.Plan == Domain.Enums.TenantPlan.Trial
                            && tenant.TrialEndsAt > DateTime.UtcNow;
        var hasActiveSubscription = HasActiveSubscription(tenant);
        var isTrialExpired = tenant.Plan == Domain.Enums.TenantPlan.Trial
                             && !isTrialActive
                             && !hasActiveSubscription;

        return new TenantBillingStatus(
            tenant.Id,
            tenant.Name,
            tenant.Plan.ToString(),
            tenant.Region.ToString(),
            isTrialActive,
            isTrialExpired,
            tenant.TrialEndsAt,
            tenant.SubscriptionStatus.ToString(),
            tenant.SubscriptionPeriodEnd,
            hasActiveSubscription,
            _stripeOptions.Enabled,
            new TenantPlanLimitsStatus(
                limits.MaxApplications,
                limits.MaxAnalysesPerMonth,
                limits.MaxStorageMegabytes,
                isOver),
            usage);
    }

    public async Task EnsureWithinLimitsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var status = await GetBillingStatusAsync(tenantId, cancellationToken);
        if (status.IsTrialExpired)
        {
            throw new ForbiddenException(
                "Trial has expired. Upgrade your plan to continue using EnhancementHub.");
        }

        if (status.Limits.IsOverLimit)
        {
            throw new ForbiddenException(
                "Tenant has exceeded plan limits. Upgrade your plan or reduce usage.");
        }
    }

    internal static bool HasActiveSubscription(Tenant tenant) =>
        tenant.Plan != Domain.Enums.TenantPlan.Trial
        || tenant.SubscriptionStatus is Domain.Enums.TenantSubscriptionStatus.Active
            or Domain.Enums.TenantSubscriptionStatus.Trialing;

    internal TenantPlanLimits ResolveLimits(Domain.Enums.TenantPlan plan) =>
        plan switch
        {
            Domain.Enums.TenantPlan.Team => _commercialOptions.TeamLimits,
            Domain.Enums.TenantPlan.Enterprise => _commercialOptions.EnterpriseLimits,
            _ => _commercialOptions.TrialLimits
        };
}
