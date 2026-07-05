using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface IStripeBillingService
{
    Task<StripeCheckoutResult> CreateCheckoutSessionAsync(
        Guid tenantId,
        TenantPlan plan,
        string? customerEmail,
        CancellationToken cancellationToken = default);

    Task<StripePortalResult> CreatePortalSessionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<StripeWebhookResult> HandleWebhookAsync(
        string payload,
        string? signatureHeader,
        CancellationToken cancellationToken = default);
}

public sealed record StripeCheckoutResult(bool Accepted, string? CheckoutUrl, string? Error);

public sealed record StripePortalResult(bool Accepted, string? PortalUrl, string? Error);

public sealed record StripeWebhookResult(bool Accepted, string? Message);
