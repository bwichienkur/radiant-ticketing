namespace EnhancementHub.Application.Abstractions;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    bool HasTenantContext { get; }
}

public interface ITenantMeteringService
{
    Task RecordAnalysisAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task RefreshUsageSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantUsageSummary> GetUsageSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record TenantUsageSummary(
    Guid TenantId,
    int ApplicationCount,
    int AnalysisCountThisMonth,
    long StorageBytes,
    DateTime PeriodStart);

public interface ITenantBillingService
{
    Task<TenantBillingStatus> GetBillingStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task EnsureWithinLimitsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record TenantBillingStatus(
    Guid TenantId,
    string TenantName,
    string Plan,
    string Region,
    bool IsTrialActive,
    bool IsTrialExpired,
    DateTime? TrialEndsAt,
    string SubscriptionStatus,
    DateTime? SubscriptionPeriodEnd,
    bool HasActiveSubscription,
    bool StripeEnabled,
    TenantPlanLimitsStatus Limits,
    TenantUsageSummary Usage);

public sealed record TenantPlanLimitsStatus(
    int MaxApplications,
    int MaxAnalysesPerMonth,
    long MaxStorageMegabytes,
    bool IsOverLimit);
