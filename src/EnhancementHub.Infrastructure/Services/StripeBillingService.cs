using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace EnhancementHub.Infrastructure.Services;

public sealed class StripeBillingService : IStripeBillingService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly StripeOptions _options;
    private readonly ITenantIsolationService _tenantIsolationService;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        IEnhancementHubDbContext dbContext,
        IOptions<StripeOptions> options,
        ITenantIsolationService tenantIsolationService,
        ILogger<StripeBillingService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _tenantIsolationService = tenantIsolationService;
        _logger = logger;
    }

    public async Task<StripeCheckoutResult> CreateCheckoutSessionAsync(
        Guid tenantId,
        TenantPlan plan,
        string? customerEmail,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return new StripeCheckoutResult(false, null, "Stripe billing is not configured.");
        }

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        var priceId = ResolvePriceId(plan);
        if (string.IsNullOrWhiteSpace(priceId))
        {
            return new StripeCheckoutResult(false, null, $"Stripe price is not configured for {plan}.");
        }

        StripeConfiguration.ApiKey = _options.SecretKey;

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "subscription",
            ClientReferenceId = tenantId.ToString(),
            SuccessUrl = _options.SuccessUrl,
            CancelUrl = _options.CancelUrl,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = tenantId.ToString(),
                ["plan"] = plan.ToString()
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId.ToString(),
                    ["plan"] = plan.ToString()
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            sessionOptions.Customer = tenant.StripeCustomerId;
        }
        else
        {
            sessionOptions.CustomerEmail = customerEmail ?? tenant.BillingEmail;
        }

        var service = new SessionService();
        var session = await service.CreateAsync(sessionOptions, cancellationToken: cancellationToken);
        return new StripeCheckoutResult(true, session.Url, null);
    }

    public async Task<StripePortalResult> CreatePortalSessionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return new StripePortalResult(false, null, "Stripe billing is not configured.");
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            return new StripePortalResult(false, null, "No Stripe customer exists for this tenant.");
        }

        StripeConfiguration.ApiKey = _options.SecretKey;

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = tenant.StripeCustomerId,
            ReturnUrl = _options.PortalReturnUrl
        }, cancellationToken: cancellationToken);

        return new StripePortalResult(true, session.Url, null);
    }

    public async Task<StripeWebhookResult> HandleWebhookAsync(
        string payload,
        string? signatureHeader,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new StripeWebhookResult(false, "Stripe webhooks are disabled.");
        }

        if (!VerifyWebhookSignature(payload, signatureHeader, _options.WebhookSecret))
        {
            return new StripeWebhookResult(false, "Invalid webhook signature.");
        }

        var envelope = JsonSerializer.Deserialize<StripeWebhookEnvelope>(payload);
        if (envelope?.Type is null)
        {
            return new StripeWebhookResult(false, "Unsupported webhook payload.");
        }

        switch (envelope.Type)
        {
            case "checkout.session.completed":
                await ApplyCheckoutCompletedAsync(envelope.Data.Object, cancellationToken);
                break;
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await ApplySubscriptionChangedAsync(envelope.Type, envelope.Data.Object, cancellationToken);
                break;
            default:
                _logger.LogDebug("Ignoring Stripe event type {EventType}", envelope.Type);
                break;
        }

        return new StripeWebhookResult(true, null);
    }

    internal static bool VerifyWebhookSignature(string payload, string? signatureHeader, string? webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var timestamp = ExtractSignaturePart(signatureHeader, "t");
        var signature = ExtractSignaturePart(signatureHeader, "v1");
        if (timestamp is null || signature is null)
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    internal async Task ApplyCheckoutCompletedAsync(
        JsonElement sessionElement,
        CancellationToken cancellationToken)
    {
        var session = sessionElement.Deserialize<StripeCheckoutSessionPayload>();
        if (session is null)
        {
            return;
        }

        var tenantId = ResolveTenantId(session.Metadata, session.ClientReferenceId);
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("Checkout session {SessionId} missing tenant metadata", session.Id);
            return;
        }

        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value, cancellationToken);
        if (tenant is null)
        {
            return;
        }

        tenant.StripeCustomerId ??= session.Customer;
        tenant.StripeSubscriptionId ??= session.Subscription;
        tenant.SubscriptionStatus = TenantSubscriptionStatus.Active;
        tenant.Plan = ResolvePlan(session.Metadata) ?? tenant.Plan;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (tenant.Plan == Domain.Enums.TenantPlan.Enterprise)
        {
            await _tenantIsolationService.TryAutoProvisionAsync(tenant.Id, cancellationToken);
        }
    }

    internal async Task ApplySubscriptionChangedAsync(
        string eventType,
        JsonElement subscriptionElement,
        CancellationToken cancellationToken)
    {
        var subscription = subscriptionElement.Deserialize<StripeSubscriptionPayload>();
        if (subscription is null)
        {
            return;
        }

        var tenantId = ResolveTenantId(subscription.Metadata, clientReferenceId: null);
        Tenant? tenant = null;

        if (tenantId.HasValue)
        {
            tenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId.Value, cancellationToken);
        }

        tenant ??= await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscription.Id, cancellationToken);

        if (tenant is null)
        {
            _logger.LogWarning("No tenant matched subscription {SubscriptionId}", subscription.Id);
            return;
        }

        tenant.StripeCustomerId ??= subscription.Customer;
        tenant.StripeSubscriptionId = subscription.Id;
        tenant.SubscriptionStatus = MapSubscriptionStatus(subscription.Status, eventType);
        tenant.SubscriptionPeriodEnd = subscription.CurrentPeriodEnd;
        tenant.Plan = ResolvePlan(subscription.Metadata) ?? MapPlanFromSubscriptionStatus(tenant, subscription.Status);
        tenant.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (tenant.Plan == Domain.Enums.TenantPlan.Enterprise)
        {
            await _tenantIsolationService.TryAutoProvisionAsync(tenant.Id, cancellationToken);
        }
    }

    internal static TenantSubscriptionStatus MapSubscriptionStatus(string? status, string eventType)
    {
        if (string.Equals(eventType, "customer.subscription.deleted", StringComparison.OrdinalIgnoreCase))
        {
            return TenantSubscriptionStatus.Canceled;
        }

        return status?.ToLowerInvariant() switch
        {
            "trialing" => TenantSubscriptionStatus.Trialing,
            "active" => TenantSubscriptionStatus.Active,
            "past_due" => TenantSubscriptionStatus.PastDue,
            "canceled" => TenantSubscriptionStatus.Canceled,
            "unpaid" => TenantSubscriptionStatus.Unpaid,
            _ => TenantSubscriptionStatus.None
        };
    }

    internal static TenantPlan? ResolvePlan(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue("plan", out var planValue))
        {
            return null;
        }

        return Enum.TryParse<TenantPlan>(planValue, ignoreCase: true, out var plan) ? plan : null;
    }

    internal static Guid? ResolveTenantId(
        IReadOnlyDictionary<string, string>? metadata,
        string? clientReferenceId)
    {
        if (metadata is not null
            && metadata.TryGetValue("tenant_id", out var tenantValue)
            && Guid.TryParse(tenantValue, out var tenantId))
        {
            return tenantId;
        }

        if (Guid.TryParse(clientReferenceId, out var referenceTenantId))
        {
            return referenceTenantId;
        }

        return null;
    }

    private static TenantPlan MapPlanFromSubscriptionStatus(Tenant tenant, string? status)
    {
        if (string.Equals(status, "canceled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "unpaid", StringComparison.OrdinalIgnoreCase))
        {
            return TenantPlan.Trial;
        }

        return tenant.Plan;
    }

    private static string? ExtractSignaturePart(string signatureHeader, string key)
    {
        foreach (var part in signatureHeader.Split(',', StringSplitOptions.TrimEntries))
        {
            var separatorIndex = part.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            if (string.Equals(part[..separatorIndex], key, StringComparison.OrdinalIgnoreCase))
            {
                return part[(separatorIndex + 1)..];
            }
        }

        return null;
    }

    private string? ResolvePriceId(TenantPlan plan) =>
        plan switch
        {
            TenantPlan.Team => _options.Prices.Team,
            TenantPlan.Enterprise => _options.Prices.Enterprise,
            _ => null
        };

    private bool IsConfigured() =>
        _options.Enabled && !string.IsNullOrWhiteSpace(_options.SecretKey);
}

internal sealed class StripeWebhookEnvelope
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("data")]
    public StripeWebhookData Data { get; set; } = new();
}

internal sealed class StripeWebhookData
{
    [JsonPropertyName("object")]
    public JsonElement Object { get; set; }
}

internal sealed class StripeCheckoutSessionPayload
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("customer")]
    public string? Customer { get; set; }

    [JsonPropertyName("subscription")]
    public string? Subscription { get; set; }

    [JsonPropertyName("client_reference_id")]
    public string? ClientReferenceId { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

internal sealed class StripeSubscriptionPayload
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("customer")]
    public string? Customer { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("current_period_end")]
    public DateTime? CurrentPeriodEnd { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
