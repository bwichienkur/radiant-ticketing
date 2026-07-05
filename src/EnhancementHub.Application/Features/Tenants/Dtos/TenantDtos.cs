namespace EnhancementHub.Application.Features.Tenants.Dtos;

public sealed record RegisterTenantResultDto(
    Guid TenantId,
    string Slug,
    string Plan,
    string Region,
    DateTime? TrialEndsAt,
    string Token,
    Guid AdminUserId,
    string AdminEmail);

public sealed record TenantBillingDto(
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
    int MaxApplications,
    int MaxAnalysesPerMonth,
    long MaxStorageMegabytes,
    int ApplicationCount,
    int AnalysisCountThisMonth,
    long StorageBytes,
    bool IsOverLimit);

public sealed record TenantSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string Plan,
    string Region,
    bool IsActive,
    DateTime? TrialEndsAt);
